using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kỹ năng Dậm đất từ trên không (Ground Slam / Dive Attack) phong cách Dead Cells.
/// Nhấn [S + Space] hoặc [Xuống + Nhảy] khi đang ở trên không.
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
        if (IsSlamming) return;

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

    /// <summary>
    /// Kiểm tra xem nhân vật đã chạm sàn hay chưa (kết hợp cả Player.IsGround và Raycast đa lớp).
    /// </summary>
    private bool IsHitGround()
    {
        if (player != null && player.IsGround) return true;

        // Quét raycast xuống dưới 1.1m để phát hiện sàn
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

            // Chờ cho đến khi chạm sàn (có timeout an toàn 1.2s tránh kẹt vĩnh viễn)
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

            // Độ trễ khựng nhẹ hồi phục sau cú nện đất (0.08s)
            yield return new WaitForSeconds(0.08f);
        }
        finally
        {
            // Failsafe tuyệt đối: luôn khôi phục trọng lực và mở khóa di chuyển
            if (rb != null) rb.gravityScale = originalGravity;
            IsSlamming = false;
        }
    }

    private void PerformImpact()
    {
        Vector2 impactPos = transform.position;

        // Rung màn hình và ngưng đọng thời gian (Hit-stop)
        CameraShake.Shake(0.45f);
        HitStop.Do(0.08f);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SFX_Hit();
        }

        // Tìm tất cả kẻ địch trong bán kính nổ
        Collider2D[] hits = Physics2D.OverlapCircleAll(impactPos, impactRadius);
        var hitRoots = new HashSet<GameObject>();

        float baseDmg = player != null ? player.GetBaseDamage() : 5f;
        float slamDamage = baseDmg * damageMultiplier * MetaUpgrades.DamageMultiplier;

        foreach (var h in hits)
        {
            if (h.isTrigger) continue;
            GameObject root = h.attachedRigidbody != null ? h.attachedRigidbody.gameObject : h.gameObject;
            if (root == gameObject || !hitRoots.Add(root)) continue;

            // Kiểm tra và áp dụng sát thương + Stun 1s lên kẻ địch
            var telegraph = root.GetComponent<EnemyTelegraph>();
            if (telegraph != null)
            {
                telegraph.Stun(stunDuration);
            }

            var e1 = root.GetComponent<PatrolEnemy1>();
            if (e1 != null)
            {
                e1.Stun(stunDuration);
                e1.takeDamageE(slamDamage, true);
                continue;
            }

            var boss = root.GetComponent<PatrolBoss>();
            if (boss != null)
            {
                boss.takeDamageE(slamDamage);
                DamagePopup.Spawn(root.transform.position, slamDamage, Color.yellow, isCritical: true);
                continue;
            }

            var dmgable = root.GetComponent<IDamageable>();
            if (dmgable != null)
            {
                dmgable.TakeDamage(slamDamage, root.transform.position);
                DamagePopup.Spawn(root.transform.position, slamDamage, Color.yellow, isCritical: true);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, impactRadius);
    }
}
