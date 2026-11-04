using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Component máu dùng chung cho Player / Enemy / Boss.
/// Thay cho việc mỗi script tự viết currentHp, maxHealth, UpdateHpBar...
/// </summary>
public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 5f;
    [Tooltip("Thời gian bất tử sau mỗi lần trúng đòn (0 = tắt)")]
    [SerializeField] private float invulnerableTime = 0f;

    public UnityEvent<float, float> onHealthChanged; // (hiện tại, tối đa)
    public UnityEvent onDamaged;
    public UnityEvent onDied;

    public float Current { get; private set; }
    public float Max => maxHealth;
    public bool IsDead { get; private set; }

    private float lastHitTime = -999f;

    private void Awake()
    {
        Current = maxHealth;
    }

    private void Start()
    {
        onHealthChanged?.Invoke(Current, maxHealth);
    }

    public void TakeDamage(float amount, Vector2 hitPoint)
    {
        if (IsDead) return;
        if (invulnerableTime > 0f && Time.time < lastHitTime + invulnerableTime) return;
        lastHitTime = Time.time;

        Current = Mathf.Max(0f, Current - amount);

        GameFeel.OnHit(hitPoint, amount, gameObject);
        onDamaged?.Invoke();
        onHealthChanged?.Invoke(Current, maxHealth);

        if (Current <= 0f)
        {
            IsDead = true;
            onDied?.Invoke();
        }
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        Current = Mathf.Min(maxHealth, Current + amount);
        onHealthChanged?.Invoke(Current, maxHealth);
    }

    public void ResetHealth()
    {
        IsDead = false;
        Current = maxHealth;
        onHealthChanged?.Invoke(Current, maxHealth);
    }
}
