using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.Mathematics;

/// <summary>
/// Boss đa giai đoạn (Multi-Phase Boss) kiểu Dead Cells:
/// - Phase 1: Tuần tra, truy đuổi và đánh cận chiến có cảnh báo `!`.
/// - Phase 2 (<= 50% HP): Nổi giận (ENRAGED), tăng 40% tốc độ, mở khóa chiêu Lao Húc Sóng Xung Kích + Bắn Cầu Ma Thuật (có thể Parry!).
/// - Tự động hiện thanh máu Boss lớn ở đáy màn hình khi giao chiến + có cơ chế kháng Stun liên tục (Poise).
/// </summary>
public class PatrolBoss : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private Transform checkPoint;
    [SerializeField] private float distance = 1f;
    [SerializeField] LayerMask layerMask;
    private bool facingRight = false;
    [SerializeField] private Transform player;
    [SerializeField] private float attackRange = 10f;
    [SerializeField] private float retrieveDistance = 2.7f;
    [SerializeField] private float chaseSpeed = 4f;
    private Animator animator;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRadius = 1f;
    [SerializeField] LayerMask LayerPlayer;
    [SerializeField] float damageE = 1f;
    [SerializeField] private float maxHealth = 5f;
    private float currentHp;
    [SerializeField] private Image hpBar;
    public GameObject coinPrefab;
    private bool die = true;

    [Header("Multi-Phase Boss Settings")]
    [SerializeField] private string bossTitle = "THE CONCIERGE WARDEN";
    [SerializeField] private float phase2SpecialCooldown = 4.5f;
    [SerializeField] private float lungeSpeed = 13f;

    public string BossTitle => bossTitle;
    public float CurrentHp => currentHp;
    public float MaxHealth => maxHealth;
    public bool IsPhase2 { get; private set; }
    public bool IsAlive => die && currentHp > 0f;

    private EnemyTelegraph telegraph;
    private StatusEffectController status;
    private SpriteRenderer[] sprites;
    private bool isPerformingSpecial;
    private float nextSpecialTime;
    private float stunImmunityUntil;
    private bool bossEngaged;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        sprites = GetComponentsInChildren<SpriteRenderer>();
        telegraph = GetComponent<EnemyTelegraph>() ?? gameObject.AddComponent<EnemyTelegraph>();
        status = GetComponent<StatusEffectController>();
    }

    void Start()
    {
        currentHp = maxHealth;
        if (player == null)
        {
            var p = FindAnyObjectByType<Player>();
            if (p != null) player = p.transform;
        }
    }

    void Update()
    {
        if (!die || player == null) return;

        UpdateHpBar();

        if (telegraph != null && telegraph.IsStunned)
        {
            animator.SetBool("Attack1", false);
            return;
        }

        if (isPerformingSpecial) return;

        float dist = Vector2.Distance(transform.position, player.position);

        // Kích hoạt thanh máu Boss ở đáy màn hình khi vào tầm giao chiến
        if (dist <= attackRange * 1.25f)
        {
            if (!bossEngaged)
            {
                bossEngaged = true;
                ProCombatHUD.RegisterActiveBoss(this);
            }
        }

        if (dist <= attackRange)
        {
            // Trong Phase 2, định kỳ tung chiêu đặc biệt (Lao húc + Sóng xung kích + Đạn ma thuật)
            if (IsPhase2 && Time.time >= nextSpecialTime && dist > 2.2f)
            {
                StartCoroutine(Phase2SpecialRoutine());
                return;
            }
            Chanse();
        }
        else
        {
            if (telegraph != null) telegraph.CancelWarning();
            Patrol();
        }
    }

    private float GetSpeedScale()
    {
        if (status == null) status = GetComponent<StatusEffectController>();
        float frostMult = status != null ? status.SpeedMultiplier : 1f;
        float phaseMult = IsPhase2 ? 1.4f : 1f;
        return frostMult * phaseMult;
    }

    private void Chanse()
    {
        FacePlayer();

        if (Vector2.Distance(transform.position, player.position) > retrieveDistance)
        {
            if (telegraph != null) telegraph.CancelWarning();
            RaycastHit2D hit = Physics2D.Raycast(checkPoint.position, Vector2.down, distance, layerMask);

            if (hit == false)
            {
                transform.position = Vector2.MoveTowards(transform.position, player.position, 0f * Time.deltaTime);
            }
            else
            {
                transform.position = Vector2.MoveTowards(transform.position, player.position, chaseSpeed * GetSpeedScale() * Time.deltaTime);
            }
            animator.SetBool("Attack1", false);
        }
        else
        {
            if (telegraph != null && !telegraph.IsPreparingAttack)
            {
                telegraph.ShowWarning(IsPhase2 ? 0.28f : 0.38f, null);
            }
            animator.SetBool("Attack1", true);
        }
    }

    private void FacePlayer()
    {
        if (player == null) return;
        if (transform.position.x > player.position.x && facingRight == true)
        {
            transform.eulerAngles = new Vector3(0f, 0f, 0f);
            facingRight = false;
        }
        else if (transform.position.x < player.position.x && facingRight == false)
        {
            transform.eulerAngles = new Vector3(0f, -180f, 0f);
            facingRight = true;
        }
    }

    private void Patrol()
    {
        transform.Translate(Vector2.left * moveSpeed * GetSpeedScale() * Time.deltaTime);
        RaycastHit2D hit = Physics2D.Raycast(checkPoint.position, Vector2.down, distance, layerMask);
        Collider2D hit1 = Physics2D.OverlapCircle(attackPoint.position, 0.2f, layerMask);
        if ((hit == false || hit1 == true) && facingRight == true)
        {
            transform.eulerAngles = new Vector3(0, 0, 0);
            facingRight = false;
        }
        else if ((hit == false || hit1 == true) && facingRight == false)
        {
            transform.eulerAngles = new Vector3(0, -180, 0);
            facingRight = true;
        }
    }

    private IEnumerator Phase2SpecialRoutine()
    {
        isPerformingSpecial = true;
        nextSpecialTime = Time.time + phase2SpecialCooldown;
        animator.SetBool("Attack1", false);
        FacePlayer();

        // Cảnh báo rõ ràng để Player kịp Parry hoặc Dash né!
        if (telegraph != null) telegraph.ShowWarning(0.45f, null);
        DamagePopup.SpawnText(transform.position + Vector3.up * 1.4f, "RAGE CHARGE!", new Color(1f, 0.25f, 0.35f), 6.2f);
        CombatVFX.SpawnShockwave(transform.position, new Color(0.95f, 0.2f, 0.45f, 0.9f), 0.3f, 2.2f, 0.35f);

        float windup = 0.45f;
        while (windup > 0f)
        {
            if (!die || (telegraph != null && telegraph.IsStunned))
            {
                isPerformingSpecial = false;
                yield break;
            }
            windup -= Time.deltaTime;
            yield return null;
        }

        // Lao húc về phía Player (kiểm tra sàn phía trước để không rơi khỏi map)
        float dir = player != null && player.position.x >= transform.position.x ? 1f : -1f;
        float lungeTime = 0.42f;
        bool hitPlayer = false;

        while (lungeTime > 0f && die)
        {
            if (telegraph != null && telegraph.IsStunned) break;

            if (checkPoint == null || Physics2D.Raycast(checkPoint.position, Vector2.down, distance * 1.5f, layerMask))
            {
                transform.position += new Vector3(dir * lungeSpeed * Time.deltaTime, 0f, 0f);
            }

            if (UnityEngine.Random.value < 0.5f)
            {
                CombatVFX.SpawnBurst(transform.position, new Color(0.9f, 0.2f, 0.45f, 0.85f), 2, 1f, 3f, 0.18f, 0.25f, 2f);
            }

            if (!hitPlayer && player != null && Vector2.Distance(transform.position, player.position) <= attackRadius * 1.3f)
            {
                hitPlayer = true;
                var p = player.GetComponent<Player>();
                if (p != null) p.TakeDamageP(damageE * 1.25f, transform.position, gameObject);
            }

            lungeTime -= Time.deltaTime;
            yield return null;
        }

        // Kết thúc cú lao: phóng ra 2 cầu ma thuật 2 bên (có thể bị Player Parry phản ngược!)
        if (die && (telegraph == null || !telegraph.IsStunned))
        {
            SpawnBossDarkOrb(new Vector2(dir, 0.05f));
            SpawnBossDarkOrb(new Vector2(-dir, 0.05f));
            CameraShake.Shake(0.32f);
            CombatVFX.SpawnShockwave(transform.position, new Color(0.8f, 0.2f, 1f, 0.9f), 0.35f, 3.0f, 0.3f);
        }

        yield return new WaitForSeconds(0.25f);
        isPerformingSpecial = false;
    }

    private void SpawnBossDarkOrb(Vector2 dir)
    {
        var go = new GameObject("BossDarkOrb");
        go.transform.position = transform.position + Vector3.up * 0.4f;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CombatVFX.GetCircleSprite();
        sr.color = new Color(0.95f, 0.2f, 0.65f, 1f);
        CombatVFX.ApplyVfxLayer(sr, 55);
        go.transform.localScale = Vector3.one * 0.55f;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.28f;
        col.isTrigger = true;

        var proj = go.AddComponent<Projectile>();
        proj.Init(dir, damageE, 9.5f, gameObject);
    }

    private void EnterPhase2()
    {
        if (IsPhase2) return;
        IsPhase2 = true;
        nextSpecialTime = Time.time + 1.2f;

        HitStop.Do(0.12f);
        CameraShake.Shake(0.55f);
        DamagePopup.SpawnText(transform.position + Vector3.up * 1.6f, "PHASE II — ENRAGED!", new Color(1f, 0.2f, 0.3f), 8.2f);
        CombatVFX.SpawnShockwave(transform.position, new Color(1f, 0.18f, 0.35f, 0.95f), 0.4f, 4.2f, 0.45f);
        CombatVFX.SpawnBurst(transform.position, new Color(1f, 0.2f, 0.4f, 1f), 22, 3f, 9f, 0.26f, 0.55f, 4f);
        if (AudioManager.Instance != null) AudioManager.Instance.SFX_Hit();

        // Nhuộm hào quang đỏ sẫm lên Boss
        if (sprites != null)
        {
            foreach (var s in sprites)
            {
                if (s != null) s.color = new Color(1f, 0.65f, 0.68f, s.color.a);
            }
        }
    }

    private void Attack()
    {
        if (!die) return;
        Collider2D collInfo = Physics2D.OverlapCircle(attackPoint.position, attackRadius, LayerPlayer);
        if (collInfo)
        {
            var p = collInfo.gameObject.GetComponent<Player>();
            if (p != null)
            {
                p.TakeDamageP(damageE * (IsPhase2 ? 1.25f : 1f), transform.position, gameObject);
            }
        }
    }

    public void Stun(float duration = 0.85f)
    {
        if (!die) return;
        // Poise: Boss miễn nhiễm stun trong 3.5s sau khi vừa bị choáng để tránh stun-lock vô hạn
        if (Time.time < stunImmunityUntil) return;

        stunImmunityUntil = Time.time + duration + 3.5f;
        if (telegraph != null) telegraph.Stun(duration);
        animator.SetBool("Attack1", false);
        animator.SetTrigger("Hurt");
    }

    public void takeDamageE(float damage)
    {
        takeDamageE(damage, false);
    }

    public void takeDamageE(float damage, bool isCritical)
    {
        if (die == false) return;

        if (!bossEngaged)
        {
            bossEngaged = true;
            ProCombatHUD.RegisterActiveBoss(this);
        }

        GameFeel.OnHit(transform.position, damage, gameObject, isCritical);
        animator.SetTrigger("Hurt");
        currentHp -= damage;
        currentHp = Mathf.Max(currentHp, 0);

        // Chuyển Phase 2 khi máu tụt xuống <= 50%
        if (!IsPhase2 && currentHp > 0f && currentHp <= maxHealth * 0.5f)
        {
            EnterPhase2();
        }

        if (currentHp <= 0)
        {
            Die();
            die = false;
        }
    }

    public void TakeDotDamage(float damage, Color popupColor)
    {
        if (die == false) return;
        DamagePopup.Spawn(transform.position, damage, popupColor, false);
        currentHp -= damage;
        currentHp = Mathf.Max(currentHp, 0);

        if (!IsPhase2 && currentHp > 0f && currentHp <= maxHealth * 0.5f)
        {
            EnterPhase2();
        }

        if (currentHp <= 0)
        {
            Die();
            die = false;
        }
    }

    private void Die()
    {
        if (telegraph != null) telegraph.CancelWarning();
        ProCombatHUD.UnregisterBoss(this);

        RunManager.Get().AddCells(15);
        RunManager.Get().AddGold(35);
        CurseSystem.OnEnemyKilled();
        MutationSystem.OnEnemyKilled(true, transform.position);
        KillStreakManager.Get().RegisterKill(transform.position);
        CombatVFX.SpawnShockwave(transform.position, new Color(1f, 0.85f, 0.25f, 0.95f), 0.4f, 4.5f, 0.45f);

        if (coinPrefab != null)
        {
            Vector3 spawnPos = new Vector3(transform.position.x, transform.position.y + 1, 0);
            Instantiate(coinPrefab, spawnPos, Quaternion.identity);
        }
        animator.SetTrigger("Die");
    }

    private void OnDieAnimationEnd()
    {
        Destroy(gameObject);
    }

    private void UpdateHpBar()
    {
        if (hpBar != null)
        {
            hpBar.fillAmount = maxHealth > 0f ? currentHp / maxHealth : 0f;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (checkPoint == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(checkPoint.position, Vector2.down * distance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
    }
}
