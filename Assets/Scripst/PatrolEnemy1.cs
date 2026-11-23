using UnityEngine;
using UnityEngine.UI;
using Unity.Mathematics;


public class PatrolEnemy1 : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private Transform checkPoint;
    [SerializeField] private float distance = 1f;
    [SerializeField] LayerMask layerMask;
    private bool facingRight = true;
    [SerializeField] private Transform player;
    [SerializeField] private float attackRange = 10f;
    [SerializeField] private float retrieveDistance = 2.7f;
    [SerializeField] private float chaseSpeed = 4f;
    private Animator animator;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRadius = 1f;
    [SerializeField] LayerMask LayerPlayer;
    [SerializeField] float damageE = 2f; // Sát thương mặc định 2 damage (ảnh 2 và ảnh 3)
    [SerializeField] private float maxHealth = 5f;
    private float currentHp;
    [SerializeField] private Image hpBar;
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private GameObject filePrefab;
    [SerializeField] float dropChance = 1f; // 100%

    [Header("Pro Combat (Knockback & Shield)")]
    [SerializeField] private float knockbackForce = 4.5f;
    [SerializeField] private bool hasFrontShield = false;

    private EnemyTelegraph telegraph;
    private ShieldedEnemy shield;
    private StatusEffectController status;
    private Rigidbody2D rb;
    private bool isDead = false;
    private bool lastHitWasCrit = false;
    private bool isAttacking = false;
    private float nextManualAttackTime = 0f;

    public bool IsFacingRight => facingRight;
    public bool IsDead => isDead;
    public bool IsAttacking => isAttacking;
    public float HpRatio => maxHealth > 0f ? currentHp / maxHealth : 0f;
    public LayerMask GroundMask => layerMask;
    /// <summary>Hệ số tốc độ cộng thêm (VD: quái mini nổi điên).</summary>
    public float SpeedBonus { get; set; } = 1f;
    /// <summary>Khi true, AI tuần tra/truy đuổi tạm dừng để script khác điều khiển (VD: chiêu lao giáo).</summary>
    public bool ExternalControl { get; set; }

    public void FaceDirection(float dir)
    {
        if (dir > 0f && !facingRight) { transform.eulerAngles = Vector3.zero; facingRight = true; }
        else if (dir < 0f && facingRight) { transform.eulerAngles = new Vector3(0f, -180f, 0f); facingRight = false; }
    }

    public void ScaleMaxHealth(float multiplier)
    {
        if (multiplier <= 0f) return;
        maxHealth *= multiplier;
        currentHp = maxHealth;
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        telegraph = GetComponent<EnemyTelegraph>() ?? gameObject.AddComponent<EnemyTelegraph>();
        shield = GetComponent<ShieldedEnemy>();
        if (hasFrontShield && shield == null) shield = gameObject.AddComponent<ShieldedEnemy>();
        status = GetComponent<StatusEffectController>();
        if (layerMask.value == 0)
        {
            layerMask = LayerMask.GetMask("Ground");
            if (layerMask.value == 0) layerMask = 128; // Layer 7 (Ground)
        }
        if (LayerPlayer.value == 0)
        {
            LayerPlayer = LayerMask.GetMask("Player");
            if (LayerPlayer.value == 0) LayerPlayer = ~0;
        }
        EnsurePoints();
    }

    public void EnsurePoints()
    {
        if (checkPoint == null)
        {
            var cp = transform.Find("checkPoint");
            if (cp != null) checkPoint = cp;
            else
            {
                var go = new GameObject("checkPoint");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(0.5f, -0.5f, 0f);
                checkPoint = go.transform;
            }
        }
        if (attackPoint == null)
        {
            var ap = transform.Find("attackPoint");
            if (ap != null) attackPoint = ap;
            else
            {
                var go = new GameObject("attackPoint");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(0.8f, 0f, 0f);
                attackPoint = go.transform;
            }
        }
    }

    public void SetDamage(float dmg)
    {
        damageE = dmg;
    }

    void Start()
    {
        currentHp = maxHealth;
        // Đảm bảo quái đánh 2 damage theo yêu cầu cân bằng (kể cả quái ở ảnh 3)
        if (damageE < 2f) damageE = 2f;
        if (player == null)
        {
            var p = FindAnyObjectByType<Player>();
            if (p != null) player = p.transform;
        }

        EnsurePoints();
    }

    void Update()
    {
        if (isDead || player == null) return;

        UpdateHpBar();

        if (ExternalControl)
        {
            isAttacking = false;
            if (animator != null) animator.SetBool("Attack1", false);
            return;
        }

        if (telegraph != null && telegraph.IsStunned)
        {
            isAttacking = false;
            if (animator != null) animator.SetBool("Attack1", false);
            if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        // Trong phạm vi tấn công
        if (Vector2.Distance(transform.position, player.position) <= attackRange)
        {
            Chanse();
        }
        else
        {
            isAttacking = false;
            if (animator != null) animator.SetBool("Attack1", false);
            if (telegraph != null) telegraph.CancelWarning();
            Patrol();
        }
    }

    private float GetSpeedScale()
    {
        if (status == null) status = GetComponent<StatusEffectController>();
        return (status != null ? status.SpeedMultiplier : 1f) * SpeedBonus;
    }

    private void Chanse()
    {
        if (checkPoint == null || attackPoint == null) EnsurePoints();
        if (checkPoint == null) return;

        if (transform.position.x > player.position.x && facingRight == true)
        {
            transform.eulerAngles = new Vector3(0f, -180f, 0f);
            facingRight = false;
        }
        else if (transform.position.x < player.position.x && facingRight == false)
        {
            transform.eulerAngles = new Vector3(0f, 0f, 0f);
            facingRight = true;
        }
        if (Vector2.Distance(transform.position, player.position) > retrieveDistance)
        {
            isAttacking = false;
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
            if (animator != null) animator.SetBool("Attack1", false);
        }
        else
        {
            if (animator != null)
            {
                isAttacking = true;
                if (telegraph != null && !telegraph.IsPreparingAttack)
                {
                    telegraph.ShowWarning(0.35f, null);
                }
                animator.SetBool("Attack1", true);
            }
            else
            {
                // Quái không dùng Animator (VD: Infernal_Elite_Demon) -> tự động cảnh báo & chém định kỳ!
                if (Time.time >= nextManualAttackTime && (telegraph == null || !telegraph.IsPreparingAttack))
                {
                    isAttacking = true;
                    nextManualAttackTime = Time.time + 1.35f;
                    if (telegraph != null)
                    {
                        telegraph.ShowWarning(0.38f, () =>
                        {
                            if (!isDead) Attack();
                        });
                    }
                    else
                    {
                        Attack();
                    }
                }
                else if (Time.time >= nextManualAttackTime - 0.55f)
                {
                    isAttacking = false;
                }
            }
        }
    }

    private void Patrol()
    {
        if (checkPoint == null || attackPoint == null) EnsurePoints();
        if (checkPoint == null || attackPoint == null) return;

        transform.Translate(Vector2.right * moveSpeed * GetSpeedScale() * Time.deltaTime);
        RaycastHit2D hit = Physics2D.Raycast(checkPoint.position, Vector2.down, distance, layerMask);
        Collider2D hit1 = Physics2D.OverlapCircle(attackPoint.position, 0.2f, layerMask);
        if ((hit == false || hit1 == true) && facingRight)
        {
            transform.eulerAngles = new Vector3(0, -180, 0);
            facingRight = false;
        }
        else if ((hit == false || hit1 == true) && facingRight == false)
        {
            transform.eulerAngles = new Vector3(0, 0, 0);
            facingRight = true;
        }
    }

    private void Attack()
    {
        if (isDead) return;
        if (attackPoint == null) EnsurePoints();
        Collider2D collInfo = Physics2D.OverlapCircle(attackPoint.position, attackRadius, LayerPlayer);
        if (collInfo)
        {
            var p = collInfo.gameObject.GetComponent<Player>() ?? collInfo.gameObject.GetComponentInParent<Player>();
            if (p != null)
            {
                p.TakeDamageP(damageE, transform.position, gameObject);
                return;
            }
        }
        // Fallback theo khoảng cách để đòn chém Quái Tinh Anh luôn trúng chuẩn xác
        if (player != null && Vector2.Distance(attackPoint.position, player.position) <= attackRadius + 1.15f)
        {
            var p = player.GetComponent<Player>();
            if (p != null) p.TakeDamageP(damageE, transform.position, gameObject);
        }
    }

    public void Stun(float duration = 1.0f)
    {
        if (isDead) return;
        isAttacking = false;
        if (telegraph != null) telegraph.Stun(duration);
        if (animator != null)
        {
            animator.SetBool("Attack1", false);
            animator.SetTrigger("Hurt");
        }
        if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    public void takeDamageE(float damage)
    {
        takeDamageE(damage, false, transform.position, true);
    }

    public void takeDamageE(float damage, bool isCritical)
    {
        takeDamageE(damage, isCritical, transform.position, true);
    }

    public bool takeDamageE(float damage, bool isCritical, Vector3 attackerPos, bool bypassShield)
    {
        if (isDead) return false;

        // Kiểm tra Khiên chắn mặt trước nếu có (trừ khi đánh sau lưng / Slam / Parry / Skill)
        if (!bypassShield && shield != null && shield.TryBlockFrontalAttack(attackerPos, facingRight))
        {
            return false;
        }

        bool wasInterrupted = false;
        if (telegraph != null)
        {
            // Tỉ lệ STUN và ngắt đòn hạn chế ở mức 10%
            wasInterrupted = telegraph.TryInterrupt(0.10f);
            if (wasInterrupted)
            {
                isCritical = true; // Thưởng crit khi ngắt đòn đối thủ!
                isAttacking = false;
                if (animator != null) animator.SetBool("Attack1", false);
            }
        }

        GameFeel.OnHit(transform.position, damage, gameObject, isCritical);

        // Knockback đẩy lùi nhẹ tạo lực tay đã hơn
        if (rb != null && attackerPos != transform.position)
        {
            float pushDir = Mathf.Sign(transform.position.x - attackerPos.x);
            if (Mathf.Approximately(pushDir, 0f)) pushDir = facingRight ? -1f : 1f;
            float force = knockbackForce * (isCritical ? 1.45f : 1f);
            rb.linearVelocity = new Vector2(pushDir * force, Mathf.Max(rb.linearVelocity.y, 1.5f));
        }

        // Hạn chế giật khựng Hurt animation theo tỉ lệ stun 10%
        if (wasInterrupted || (UnityEngine.Random.value < 0.10f))
        {
            if (animator != null) animator.SetTrigger("Hurt");
        }
        lastHitWasCrit = isCritical;
        currentHp -= damage;
        currentHp = Mathf.Max(currentHp, 0);
        if (currentHp <= 0) Die();
        return true;
    }

    public void TakeDotDamage(float damage, Color popupColor)
    {
        if (isDead) return;
        DamagePopup.Spawn(transform.position, damage, popupColor, false);
        currentHp -= damage;
        currentHp = Mathf.Max(currentHp, 0);
        if (currentHp <= 0) Die();
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        if (telegraph != null) telegraph.CancelWarning();
        RunManager.Get().AddGold(5);
        CurseSystem.OnEnemyKilled();
        bool afflicted = status != null && status.HasAnyAffliction;
        MutationSystem.OnEnemyKilled(lastHitWasCrit || afflicted, transform.position);

        var elite = GetComponent<EliteEnemy>();
        if (elite != null) elite.OnEliteKilled();

        KillStreakManager.Get().RegisterKill(transform.position);

        if (coinPrefab != null)
        {
            Vector3 spawnPosCoin = new Vector3(transform.position.x, transform.position.y + 2f, 0);
            Instantiate(coinPrefab, spawnPosCoin, Quaternion.identity);
        }

        if (filePrefab != null && UnityEngine.Random.value < dropChance)
        {
            Vector3 spawnPosFile = new Vector3(transform.position.x + 0.5f, transform.position.y + 2f, 0);
            Instantiate(filePrefab, spawnPosFile, Quaternion.identity);
        }

        if (animator != null)
            animator.SetTrigger("Die");
        else
            Destroy(gameObject, 0.15f);
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
