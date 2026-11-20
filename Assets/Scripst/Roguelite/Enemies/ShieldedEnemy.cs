using UnityEngine;

/// <summary>
/// Cơ chế Quái Cầm Khiên (Shieldbearer) kiểu Dead Cells:
/// - Chặn 100% đòn cận chiến đánh từ chính diện (hiện tia lửa kim loại + chữ BLOCKED!).
/// - Buộc người chơi phải Dash xuyên ra sau lưng đánh Backstab Crit, hoặc dùng Ground Slam / Parry / Skill để phá khiên!
/// </summary>
public class ShieldedEnemy : MonoBehaviour
{
    [SerializeField] private bool shieldActive = true;
    [SerializeField] private float guardBreakDuration = 2.0f;

    private float guardBrokenUntil;
    private GameObject shieldIconObj;
    private SpriteRenderer shieldIconSr;

    public bool IsGuardUp => shieldActive && Time.time >= guardBrokenUntil;

    private void Awake()
    {
        CreateShieldVisual();
    }

    private void CreateShieldVisual()
    {
        shieldIconObj = new GameObject("FrontShieldVisual");
        shieldIconObj.transform.SetParent(transform);
        shieldIconObj.transform.localPosition = new Vector3(0.55f, 0.05f, 0f);
        shieldIconObj.transform.localScale = new Vector3(0.28f, 0.95f, 1f);

        shieldIconSr = shieldIconObj.AddComponent<SpriteRenderer>();
        shieldIconSr.sprite = CombatVFX.GetPixelSprite();
        shieldIconSr.color = new Color(0.75f, 0.82f, 0.95f, 0.9f);
        shieldIconSr.sortingOrder = 35;
    }

    private void Update()
    {
        if (shieldIconObj != null)
        {
            shieldIconObj.SetActive(IsGuardUp);
        }
    }

    /// <summary>Kiểm tra xem đòn đánh từ attackerPos có đánh vào mặt trước đang giơ khiên hay không.</summary>
    public bool TryBlockFrontalAttack(Vector3 attackerPos, bool enemyFacingRight)
    {
        if (!IsGuardUp) return false;

        // Nếu quái đang bị Stun thì không đỡ được
        var tel = GetComponent<EnemyTelegraph>();
        if (tel != null && tel.IsStunned) return false;

        float dx = attackerPos.x - transform.position.x;
        bool attackerInFront = enemyFacingRight ? (dx >= 0f) : (dx <= 0f);

        if (attackerInFront)
        {
            DamagePopup.SpawnText(transform.position + Vector3.up * 0.8f, "BLOCKED!", new Color(0.78f, 0.86f, 1f), 6.2f);
            CombatVFX.SpawnBurst(transform.position + new Vector3(enemyFacingRight ? 0.5f : -0.5f, 0.1f, 0f), new Color(1f, 0.95f, 0.7f, 1f), 8, 2.5f, 6.5f, 0.16f, 0.25f, 5f);
            CameraShake.Shake(0.15f);
            if (AudioManager.Instance != null) AudioManager.Instance.SFX_Hit();
            return true;
        }
        return false;
    }

    /// <summary>Phá vỡ khiên khi ăn Ground Slam, Parry hoặc Lựu đạn nổ.</summary>
    public void BreakGuard(float extraDuration = 0f)
    {
        if (!shieldActive) return;
        guardBrokenUntil = Mathf.Max(guardBrokenUntil, Time.time + guardBreakDuration + extraDuration);
        DamagePopup.SpawnText(transform.position + Vector3.up * 1.1f, "GUARD BREAK!", new Color(1f, 0.55f, 0.15f), 6.8f);
        CombatVFX.SpawnShockwave(transform.position, new Color(1f, 0.65f, 0.2f, 0.9f), 0.25f, 1.8f, 0.25f);
    }
}
