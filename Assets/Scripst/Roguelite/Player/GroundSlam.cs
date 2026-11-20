using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kỹ năng Dậm đất từ trên không (Ground Slam / Dive Attack) phong cách Dead Cells.
/// Nhấn [S + Space] hoặc [Xuống + Nhảy] khi đang ở trên không.
/// - Phá khiên của ShieldedEnemy, gây Stun đỏ 1.0s, tạo sóng xung kích & kích hoạt Rally hồi máu.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class GroundSlam : MonoBehaviour
{
    [Header("Slam Settings")]
    [SerializeField] private float slamSpeed = 32f;
    [SerializeField] private float impactRadius = 3.5f;
    [SerializeField] private float damageMultiplier = 2.0f;
    [SerializeField] private float stunDuration = 1.0f; // 1 giây theo yêu cầu

    public bool IsSlamming { get; private set; }

    private Rigidbody2D rb;
    private Player player;
    private float originalGravity = 1f;
    private float lastSlamTime;
    private const float Cooldown = 0.8f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        player = GetComponent<Player>();
        if (rb != null)
        {
            originalGravity = rb.gravityScale > 0f ? rb.gravityScale : 1f;
        }
    }

    private void Update()
    {
        if (IsSlamming || Time.timeScale <= 0f) return;

        // Chỉ kích hoạt khi đang ở trên không
        bool isAirborne = player != null ? !player.IsGround : true;
        if (!isAirborne) return;

        // Kiểm tra phím: S + Space hoặc Mũi tên xuống + Space
        bool downInput = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) || Input.GetAxisRaw("Vertical") < -0.5f;
        bool jumpInput = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space);

        if (downInput && jumpInput && Time.time >= lastSlamTime + Cooldown)
        {
            StartCoroutine(GroundSlamRoutine());
        }
    }

    private bool IsHitGround()
    {
        if (player != null && player.IsGround) return true;

        RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, Vector2.down, 1.1f);
        foreach (var h in hits)
        {
            if (h.collider != null && !h.collider.isTrigger && h.collider.gameObject != gameObject)
            {
                return true;
            }
        }
        return false;
    }

    private IEnumerator GroundSlamRoutine()
    {
        IsSlamming = true;
        lastSlamTime = Time.time;

        if (rb == null) rb = GetComponent<Rigidbody2D>();
        originalGravity = rb.gravityScale > 0f ? rb.gravityScale : 1f;

        try
        {
            // 1. Wind-up trên không (tụ lực khựng 0.05s)
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f;
            yield return new WaitForSeconds(0.05f);

            // 2. Lao thẳng xuống với vận tốc cao
            rb.linearVelocity = new Vector2(0f, -slamSpeed);

            float elapsed = 0f;
            const float MaxDiveDuration = 1.2f;

            while (!IsHitGround() && elapsed < MaxDiveDuration)
            {
                rb.linearVelocity = new Vector2(0f, -slamSpeed);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // 3. Chạm đất - Tác động chấn động nổ diện rộng (Impact)
            rb.gravityScale = originalGravity;
            rb.linearVelocity = Vector2.zero;

            PerformImpact();

            yield return new WaitForSeconds(0.08f);
        }
        finally
        {
            if (rb != null) rb.gravityScale = originalGravity;
            IsSlamming = false;
        }
    }

    private void PerformImpact()
    {
        Vector2 impactPos = transform.position;

        CameraShake.Shake(0.45f);
        HitStop.Do(0.08f);
        CombatVFX.SpawnShockwave(impactPos, new Color(1f, 0.72f, 0.2f, 0.95f), 0.35f, impactRadius * 1.35f, 0.28f);
        CombatVFX.SpawnBurst(impactPos, new Color(1f, 0.78f, 0.3f, 0.95f), 16, 3f, 8.5f, 0.22f, 0.4f, 9f);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SFX_Hit();
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(impactPos, impactRadius);
        var hitRoots = new HashSet<GameObject>();

        float baseDmg = player != null ? player.GetBaseDamage() : 5f;
        float slamDamage = baseDmg * damageMultiplier * MetaUpgrades.DamageMultiplier * ScrollUpgradeUI.BrutalityDamageMultiplier;

        foreach (var h in hits)
        {
            if (h.isTrigger) continue;
            GameObject root = h.attachedRigidbody != null ? h.attachedRigidbody.gameObject : h.gameObject;
            if (root == gameObject || !hitRoots.Add(root)) continue;

            // Ground Slam phá khiên của kẻ địch!
            var shield = root.GetComponent<ShieldedEnemy>();
            if (shield != null) shield.BreakGuard(stunDuration);

            var telegraph = root.GetComponent<EnemyTelegraph>();
            if (telegraph != null) telegraph.Stun(stunDuration);

            var e1 = root.GetComponent<PatrolEnemy1>();
            if (e1 != null)
            {
                e1.Stun(stunDuration);
                e1.takeDamageE(slamDamage, true, impactPos, bypassShield: true);
                if (player != null) player.NotifyDamageDealt(slamDamage);
                continue;
            }

            var boss = root.GetComponent<PatrolBoss>();
            if (boss != null)
            {
                boss.Stun(stunDuration * 0.75f);
                boss.takeDamageE(slamDamage, true);
                if (player != null) player.NotifyDamageDealt(slamDamage);
                continue;
            }

            var dmgable = root.GetComponent<IDamageable>();
            if (dmgable != null)
            {
                dmgable.TakeDamage(slamDamage, root.transform.position);
                DamagePopup.Spawn(root.transform.position, slamDamage, Color.yellow, isCritical: true);
                if (player != null) player.NotifyDamageDealt(slamDamage);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, impactRadius);
    }
}
