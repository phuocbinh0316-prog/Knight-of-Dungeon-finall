using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lướt/lăn né kiểu Dead Cells:
/// - Lao nhanh theo hướng đang nhìn với khung bất tử (i-frame).
/// - Xuyên qua va chạm vật lý với Quái (Phase-through Enemies) để lăn ra sau lưng đánh Backstab Crit!
/// - Để lại vệt bóng mờ (Afterimage Trail) cực mượt.
/// Phím: K hoặc Left Shift.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class DashRoll : MonoBehaviour
{
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.22f;
    [SerializeField] private float invulnerableDuration = 0.28f;
    [SerializeField] private float cooldown = 0.6f;
    [Range(0.2f, 1f)] [SerializeField] private float ghostAlpha = 0.5f;
    [SerializeField] private float afterimageInterval = 0.035f;
    [SerializeField] private Color afterimageColor = new Color(0.45f, 0.85f, 1f, 0.58f);

    public bool IsDashing { get; private set; }
    public bool IsInvulnerable => Time.time < invulnerableUntil;
    public float CooldownRemaining => Mathf.Max(0f, nextDashTime - Time.time);
    public float CooldownNormalized
    {
        get
        {
            float effCd = cooldown * ScrollUpgradeUI.TacticsCooldownMultiplier;
            return effCd > 0f ? Mathf.Clamp01(1f - CooldownRemaining / effCd) : 1f;
        }
    }

    private Rigidbody2D rb;
    private SpriteRenderer[] sprites;
    private SpriteRenderer primarySprite;
    private Collider2D[] playerColliders;
    private readonly List<Collider2D> ignoredEnemyColliders = new List<Collider2D>();

    private float nextDashTime;
    private float invulnerableUntil;
    private float originalGravity = 1f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sprites = GetComponentsInChildren<SpriteRenderer>();
        primarySprite = GetComponent<SpriteRenderer>() ?? (sprites.Length > 0 ? sprites[0] : null);
        playerColliders = GetComponentsInChildren<Collider2D>();
        if (rb != null) originalGravity = rb.gravityScale > 0f ? rb.gravityScale : 1f;
    }

    public bool TryTriggerDash()
    {
        if (IsDashing || Time.time < nextDashTime || Time.timeScale <= 0f) return false;
        StartCoroutine(DashRoutine());
        return true;
    }

    private void Update()
    {
        bool pressed = Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.LeftShift);
        if (pressed)
            TryTriggerDash();

        SetAlpha(IsInvulnerable ? ghostAlpha : 1f);
    }

    private IEnumerator DashRoutine()
    {
        IsDashing = true;
        float effCd = cooldown * ScrollUpgradeUI.TacticsCooldownMultiplier;
        nextDashTime = Time.time + effCd;
        invulnerableUntil = Time.time + invulnerableDuration;

        float dir = transform.right.x >= 0f ? 1f : -1f;
        originalGravity = rb.gravityScale > 0f ? rb.gravityScale : 1f;
        rb.gravityScale = 0f;

        // Tắt va chạm vật lý với kẻ địch để lướt xuyên ra sau lưng đánh Backstab!
        EnablePhaseThroughEnemies();

        // Bụi xuất phát khi lướt
        CombatVFX.SpawnFootDust(transform.position + Vector3.down * 0.45f, 6, false);
        if (AudioManager.Instance != null) AudioManager.Instance.SFX_Jump();

        try
        {
            float t = 0f;
            float ghostTimer = 0f;
            while (t < dashDuration)
            {
                rb.linearVelocity = new Vector2(dir * dashSpeed, 0f);
                t += Time.deltaTime;
                ghostTimer += Time.deltaTime;

                if (ghostTimer >= afterimageInterval)
                {
                    ghostTimer = 0f;
                    CombatVFX.SpawnAfterimage(primarySprite, afterimageColor, 0.22f);
                }

                // Kiểm tra lướt xuyên qua kẻ địch để kích hoạt Mutation Frost Dash!
                if (MutationSystem.Has(MutationType.FrostDash))
                {
                    Collider2D[] passed = Physics2D.OverlapCircleAll(transform.position, 1.35f);
                    foreach (var p in passed)
                    {
                        if (p == null || p.isTrigger) continue;
                        GameObject r = p.attachedRigidbody != null ? p.attachedRigidbody.gameObject : p.gameObject;
                        if (r == gameObject) continue;
                        if (r.GetComponent<PatrolEnemy1>() != null || r.GetComponent<PatrolBoss>() != null || r.GetComponent<RangedEnemy>() != null)
                        {
                            MutationSystem.OnDashThroughEnemy(r);
                        }
                    }
                }
                yield return null;
            }
        }
        finally
        {
            RestoreEnemyCollisions();
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                rb.gravityScale = originalGravity;
            }
            IsDashing = false;
        }
    }

    private void EnablePhaseThroughEnemies()
    {
        ignoredEnemyColliders.Clear();
        if (playerColliders == null || playerColliders.Length == 0) return;

        // Tìm các kẻ địch quanh khu vực lướt
        Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, 14f);
        foreach (var col in nearby)
        {
            if (col == null || col.isTrigger) continue;
            GameObject root = col.attachedRigidbody != null ? col.attachedRigidbody.gameObject : col.gameObject;
            if (root == gameObject) continue;

            bool isEnemy = root.GetComponent<PatrolEnemy1>() != null
                        || root.GetComponent<PatrolBoss>() != null
                        || root.GetComponent<RangedEnemy>() != null
                        || root.CompareTag("Enemy");

            if (!isEnemy) continue;

            foreach (var pCol in playerColliders)
            {
                if (pCol != null && !pCol.isTrigger)
                {
                    Physics2D.IgnoreCollision(pCol, col, true);
                }
            }
            ignoredEnemyColliders.Add(col);
        }
    }

    private void RestoreEnemyCollisions()
    {
        if (playerColliders == null) return;
        foreach (var col in ignoredEnemyColliders)
        {
            if (col == null) continue;
            foreach (var pCol in playerColliders)
            {
                if (pCol != null) Physics2D.IgnoreCollision(pCol, col, false);
            }
        }
        ignoredEnemyColliders.Clear();
    }

    private void SetAlpha(float a)
    {
        if (sprites == null) return;
        foreach (var s in sprites)
        {
            if (s == null) continue;
            var c = s.color;
            if (Mathf.Approximately(c.a, a)) continue;
            c.a = a;
            s.color = c;
        }
    }

    private void OnDisable()
    {
        RestoreEnemyCollisions();
        if (IsDashing && rb != null) rb.gravityScale = originalGravity;
        IsDashing = false;
        SetAlpha(1f);
    }
}
