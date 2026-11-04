using System.Collections;
using UnityEngine;

/// <summary>
/// Lướt/lăn né kiểu Dead Cells: lao nhanh theo hướng đang nhìn, có khung bất tử (i-frame).
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

    public bool IsDashing { get; private set; }
    public bool IsInvulnerable => Time.time < invulnerableUntil;

    private Rigidbody2D rb;
    private SpriteRenderer[] sprites;
    private float nextDashTime;
    private float invulnerableUntil;
    private float originalGravity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sprites = GetComponentsInChildren<SpriteRenderer>();
    }

    private void Update()
    {
        bool pressed = Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.LeftShift);
        if (pressed && !IsDashing && Time.time >= nextDashTime && Time.timeScale > 0f)
            StartCoroutine(DashRoutine());

        // Mờ đi khi đang bất tử để người chơi thấy rõ i-frame
        SetAlpha(IsInvulnerable ? ghostAlpha : 1f);
    }

    private IEnumerator DashRoutine()
    {
        IsDashing = true;
        nextDashTime = Time.time + cooldown;
        invulnerableUntil = Time.time + invulnerableDuration;

        float dir = transform.right.x >= 0f ? 1f : -1f; // Player lật bằng eulerAngles.y
        originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;

        float t = 0f;
        while (t < dashDuration)
        {
            rb.linearVelocity = new Vector2(dir * dashSpeed, 0f);
            t += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        rb.gravityScale = originalGravity;
        IsDashing = false;
    }

    private void SetAlpha(float a)
    {
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
        if (IsDashing && rb != null) rb.gravityScale = originalGravity;
        IsDashing = false;
    }
}
