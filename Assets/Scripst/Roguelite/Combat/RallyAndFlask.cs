using UnityEngine;

/// <summary>
/// Cơ chế Máu Cam (Rally Health Recovery) & Bình Máu (Health Flask) đặc trưng của Dead Cells:
/// - Khi bị mất máu, phần máu vừa mất giữ lại dưới dạng Máu Cam trong 2.8 giây.
/// - Nếu chém trúng quái ngay trong thời gian đó, hồi lại một phần Máu Cam thành máu thật!
/// - Phím [H]: Uống Bình Máu hồi 60% Max HP (có giới hạn số bình mỗi màn/lượt).
/// </summary>
public class RallyAndFlask : MonoBehaviour
{
    [Header("Rally (Máu Cam)")]
    [SerializeField] private float rallyHoldDuration = 2.8f;
    [SerializeField] private float rallyDecayRate = 1.6f; // máu/giây tụt dần sau khi hết thời gian giữ
    [SerializeField] private float rallyRecoverRatio = 0.45f; // hồi 45% sát thương gây ra

    [Header("Health Flask (Bình Máu - Phím H)")]
    [SerializeField] private int baseMaxFlasks = 2;
    [SerializeField] private float flaskHealFraction = 0.6f;
    [SerializeField] private float flaskCooldown = 1.2f;

    public float RallyPool { get; private set; }
    public int FlaskCharges { get; private set; }
    public int MaxFlaskCharges { get; private set; }
    public bool CanUseFlask => FlaskCharges > 0 && Time.time >= nextFlaskTime && player != null && player.CurrentHp < player.MaxHealth;

    private Player player;
    private float rallyHoldTimer;
    private float nextFlaskTime;

    private void Awake()
    {
        player = GetComponent<Player>();
        MaxFlaskCharges = baseMaxFlasks + Mathf.Min(2, SaveSystem.Data.upgradeHealth / 2);
        FlaskCharges = MaxFlaskCharges;
    }

    private void Update()
    {
        if (Time.timeScale <= 0f) return;

        // Cập nhật suy giảm Máu Cam
        if (RallyPool > 0f)
        {
            if (rallyHoldTimer > 0f)
            {
                rallyHoldTimer -= Time.deltaTime;
            }
            else
            {
                RallyPool = Mathf.Max(0f, RallyPool - rallyDecayRate * Time.deltaTime);
            }
        }

        // Phím H uống bình máu
        if (Input.GetKeyDown(KeyCode.H))
        {
            TryUseFlask();
        }
    }

    /// <summary>Gọi khi Player nhận sát thương: tích phần máu mất vào bể Máu Cam.</summary>
    public void OnPlayerDamaged(float damageTaken)
    {
        if (damageTaken <= 0f || player == null) return;
        RallyPool = Mathf.Min(player.MaxHealth - player.CurrentHp, RallyPool + damageTaken);
        rallyHoldTimer = rallyHoldDuration;
    }

    /// <summary>Gọi khi Player đánh trúng quái: hồi lại Máu Cam thành máu thật.</summary>
    public void OnPlayerDealtDamage(float damageDealt)
    {
        if (RallyPool <= 0.01f || player == null || damageDealt <= 0f) return;
        if (player.CurrentHp <= 0f) return;

        float recover = Mathf.Min(RallyPool, damageDealt * rallyRecoverRatio);
        if (recover > 0.05f)
        {
            RallyPool = Mathf.Max(0f, RallyPool - recover);
            player.Heal(recover);
            DamagePopup.SpawnText(transform.position + Vector3.up * 0.5f, $"+{recover:0.#}", new Color(1f, 0.65f, 0.15f), 5.5f);
            CombatVFX.SpawnBurst(transform.position, new Color(1f, 0.65f, 0.15f, 0.9f), 5, 1.5f, 3.5f, 0.16f, 0.3f, 2f);
        }
    }

    public bool TryUseFlask()
    {
        if (!CanUseFlask) return false;

        FlaskCharges--;
        nextFlaskTime = Time.time + flaskCooldown;
        RallyPool = 0f;

        float healAmount = Mathf.Max(1f, player.MaxHealth * flaskHealFraction);
        player.Heal(healAmount);

        DamagePopup.SpawnText(transform.position + Vector3.up * 0.7f, $"FLASK +{healAmount:0.#} HP", new Color(0.25f, 1f, 0.45f), 6.2f);
        CombatVFX.SpawnShockwave(transform.position, new Color(0.25f, 1f, 0.5f, 0.9f), 0.3f, 2.0f, 0.35f);
        CombatVFX.SpawnBurst(transform.position, new Color(0.3f, 1f, 0.55f, 0.95f), 14, 2f, 5.5f, 0.22f, 0.45f, -2f);
        if (AudioManager.Instance != null) AudioManager.Instance.SFX_TakeCoin();
        return true;
    }

    public void AddFlaskCharge(int amount = 1, bool increaseMax = false)
    {
        if (increaseMax) MaxFlaskCharges += amount;
        FlaskCharges = Mathf.Min(MaxFlaskCharges, FlaskCharges + amount);
    }

    public void RefillAllFlasks()
    {
        FlaskCharges = MaxFlaskCharges;
    }
}
