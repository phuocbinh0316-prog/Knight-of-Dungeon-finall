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
    [SerializeField] private float stunDuration = 1.8f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheck;

    public bool IsSlamming { get; private set; }

    private Rigidbody2D rb;
    private Player player;
    private float originalGravity;
    private float lastSlamTime;
    private const float Cooldown = 1.0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        player = GetComponent<Player>();
        originalGravity = rb.gravityScale;
    }

    private void Update()
    {
        if (IsSlamming) return;

        // Kiểm tra điều kiện kích hoạt: đang trên không + nhấn Xuống + Nhảy (hoặc phím S khi đang rơi)
        bool downInput = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) || Input.GetAxisRaw("Vertical") < -0.5f;
        bool jumpInput = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.S);

        if (downInput && jumpInput && !IsOnGround() && Time.time >= lastSlamTime + Cooldown)
        {
            StartCoroutine(GroundSlamRoutine());
        }
    }

    private bool IsOnGround()
    {
        Vector2 checkPos = groundCheck != null ? (Vector2)groundCheck.position : (Vector2)transform.position + Vector2.down * 0.8f;
        return Physics2D.OverlapCircle(checkPos, 0.3f, groundLayer);
    }

    private IEnumerator GroundSlamRoutine()
    {
        IsSlamming = true;
        lastSlamTime = Time.time;

        // 1. Wind-up trên không (tụ lực tích gió)
        rb.velocity = Vector2.zero;
        rb.gravityScale = 0f;
        yield return new WaitForSeconds(0.06f);

        // 2. Lao thẳng xuống với vận tốc cao
        rb.velocity = new Vector2(0f, -slamSpeed);

        // Chờ cho đến khi chạm đất
        while (!IsOnGround())
        {
            rb.velocity = new Vector2(0f, -slamSpeed);
            yield return null;
        }

        // 3. Chạm đất - Tác động chấn động nổ diện rộng (Impact)
        rb.gravityScale = originalGravity;
        rb.velocity = Vector2.zero;

        PerformImpact();

        // Độ khựng hồi phục sau cú nện đất (0.12s) để tạo trọng lực đòn đánh
        yield return new WaitForSeconds(0.12f);

        IsSlamming = false;
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

            // Kiểm tra và áp dụng sát thương + Stun lên kẻ địch
            var telegraph = root.GetComponent<EnemyTelegraph>();
            if (telegraph != null)
            {
                telegraph.Stun(stunDuration);
            }

            var e1 = root.GetComponent<PatrolEnemy1>();
            if (e1 != null)
            {
                e1.Stun(stunDuration);
                e1.takeDamageE(slamDamage);
                DamagePopup.Spawn(root.transform.position, slamDamage, Color.yellow, isCritical: true);
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
