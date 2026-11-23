using TMPro;
using UnityEngine;

public enum StatusType
{
    Bleed,
    Burn,
    Poison,
    Frost
}

/// <summary>
/// Quản lý hiệu ứng trạng thái kiểu Dead Cells (Chảy máu, Thiêu đốt, Độc, Làm chậm Băng)
/// kèm cơ chế Synergy (+40% sát thương khi đánh vào mục tiêu đang dính trạng thái).
/// </summary>
public class StatusEffectController : MonoBehaviour
{
    private float bleedTimer;
    private float burnTimer;
    private float poisonTimer;
    private float frostTimer;

    private float bleedDps = 0.6f;
    private float burnDps = 0.8f;
    private float poisonDps = 0.55f;

    private float tickTimer;
    private const float TickInterval = 0.5f;

    private SpriteRenderer[] renderers;
    private Color[] baseColors;
    private TextMeshPro statusLabel;
    private GameObject labelObj;

    public bool IsBleeding => bleedTimer > 0f;
    public bool IsBurning => burnTimer > 0f;
    public bool IsPoisoned => poisonTimer > 0f;
    public bool IsFrosted => frostTimer > 0f;

    /// <summary>Trả về true nếu mục tiêu đang dính bất kỳ hiệu ứng bất lợi nào để kích hoạt Synergy.</summary>
    public bool HasAnyAffliction => IsBleeding || IsBurning || IsPoisoned || IsFrosted;

    /// <summary>Hệ số tốc độ di chuyển (bị làm chậm 45% khi dính Frost).</summary>
    public float SpeedMultiplier => IsFrosted ? 0.55f : 1f;

    public static StatusEffectController GetOrAdd(GameObject target)
    {
        if (target == null) return null;
        return target.GetComponent<StatusEffectController>() ?? target.AddComponent<StatusEffectController>();
    }

    private void Awake()
    {
        CacheRenderers();
        CreateLabel();
    }

    private void CacheRenderers()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>();
        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            baseColors[i] = renderers[i] != null ? renderers[i].color : Color.white;
        }
    }

    private void CreateLabel()
    {
        labelObj = new GameObject("StatusLabel");
        labelObj.transform.SetParent(transform);
        labelObj.transform.localPosition = new Vector3(0f, 1.85f, 0f);

        statusLabel = labelObj.AddComponent<TextMeshPro>();
        statusLabel.fontSize = 4.2f;
        statusLabel.fontStyle = FontStyles.Bold;
        statusLabel.alignment = TextAlignmentOptions.Center;
        CombatVFX.ApplyVfxLayer(statusLabel.renderer, 118);
        labelObj.SetActive(false);
    }

    public void Apply(StatusType type, float duration, float dps = -1f)
    {
        switch (type)
        {
            case StatusType.Bleed:
                bleedTimer = Mathf.Max(bleedTimer, duration);
                if (dps > 0f) bleedDps = dps;
                DamagePopup.SpawnText(transform.position + Vector3.up * 0.4f, "BLEED", new Color(0.95f, 0.15f, 0.25f), 5.2f);
                CombatVFX.SpawnBurst(transform.position, new Color(0.9f, 0.1f, 0.2f, 0.9f), 6, 1.5f, 4f, 0.18f, 0.3f);
                break;
            case StatusType.Burn:
                burnTimer = Mathf.Max(burnTimer, duration);
                if (dps > 0f) burnDps = dps;
                DamagePopup.SpawnText(transform.position + Vector3.up * 0.4f, "BURN", new Color(1f, 0.5f, 0.1f), 5.2f);
                CombatVFX.SpawnBurst(transform.position, new Color(1f, 0.55f, 0.1f, 0.95f), 7, 1.5f, 4.5f, 0.2f, 0.35f, 2f);
                break;
            case StatusType.Poison:
                poisonTimer = Mathf.Max(poisonTimer, duration);
                if (dps > 0f) poisonDps = dps;
                DamagePopup.SpawnText(transform.position + Vector3.up * 0.4f, "POISON", new Color(0.35f, 0.95f, 0.35f), 5.2f);
                CombatVFX.SpawnBurst(transform.position, new Color(0.3f, 0.95f, 0.35f, 0.9f), 6, 1.2f, 3.5f, 0.2f, 0.35f, 1.5f);
                break;
            case StatusType.Frost:
                frostTimer = Mathf.Max(frostTimer, duration);
                DamagePopup.SpawnText(transform.position + Vector3.up * 0.4f, "FROST", new Color(0.45f, 0.9f, 1f), 5.2f);
                CombatVFX.SpawnBurst(transform.position, new Color(0.55f, 0.92f, 1f, 0.9f), 6, 1.5f, 3.5f, 0.2f, 0.35f, 2f);
                break;
        }
        UpdateVisuals();
    }

    private void Update()
    {
        if (!HasAnyAffliction)
        {
            if (labelObj != null && labelObj.activeSelf)
            {
                labelObj.SetActive(false);
                RestoreTint();
            }
            return;
        }

        float dt = Time.deltaTime;
        if (bleedTimer > 0f) bleedTimer -= dt;
        if (burnTimer > 0f) burnTimer -= dt;
        if (poisonTimer > 0f) poisonTimer -= dt;
        if (frostTimer > 0f) frostTimer -= dt;

        tickTimer += dt;
        if (tickTimer >= TickInterval)
        {
            tickTimer -= TickInterval;
            ApplyTickDamage();
        }

        UpdateVisuals();
        if (labelObj != null && labelObj.activeSelf)
        {
            labelObj.transform.rotation = Quaternion.identity;
        }
    }

    private void ApplyTickDamage()
    {
        float dmg = 0f;
        Color popupCol = Color.white;

        if (IsBurning)
        {
            dmg += burnDps * TickInterval;
            popupCol = new Color(1f, 0.5f, 0.1f);
            CombatVFX.SpawnBurst(transform.position + Vector3.up * 0.3f, popupCol, 2, 0.8f, 2.2f, 0.14f, 0.25f, -2f);
        }
        if (IsBleeding)
        {
            dmg += bleedDps * TickInterval;
            popupCol = new Color(0.95f, 0.15f, 0.25f);
            CombatVFX.SpawnBurst(transform.position, popupCol, 2, 0.5f, 1.8f, 0.13f, 0.25f, 6f);
        }
        if (IsPoisoned)
        {
            dmg += poisonDps * TickInterval;
            popupCol = new Color(0.3f, 0.95f, 0.35f);
            CombatVFX.SpawnBurst(transform.position + Vector3.up * 0.2f, popupCol, 2, 0.5f, 1.8f, 0.14f, 0.28f, -1.5f);
        }

        if (dmg <= 0f) return;

        // Gây sát thương DoT trực tiếp lên loại entity tương ứng
        var p = GetComponent<Player>();
        if (p != null)
        {
            p.TakeDamageP(dmg);
            return;
        }

        var e1 = GetComponent<PatrolEnemy1>();
        if (e1 != null)
        {
            e1.TakeDotDamage(dmg, popupCol);
            return;
        }

        var boss = GetComponent<PatrolBoss>();
        if (boss != null)
        {
            boss.TakeDotDamage(dmg, popupCol);
            return;
        }

        var dmgable = GetComponent<IDamageable>();
        if (dmgable != null)
        {
            dmgable.TakeDamage(dmg, transform.position);
            DamagePopup.Spawn(transform.position, dmg, popupCol, false);
        }
    }

    private void UpdateVisuals()
    {
        if (labelObj == null || statusLabel == null) return;

        if (!HasAnyAffliction)
        {
            labelObj.SetActive(false);
            RestoreTint();
            return;
        }

        string tag = "";
        Color col = Color.white;
        if (IsBurning) { tag += "BURN "; col = new Color(1f, 0.55f, 0.15f); }
        if (IsBleeding) { tag += "BLEED "; col = new Color(1f, 0.25f, 0.3f); }
        if (IsPoisoned) { tag += "POISON "; col = new Color(0.35f, 1f, 0.4f); }
        if (IsFrosted) { tag += "FROST "; col = new Color(0.5f, 0.92f, 1f); }

        statusLabel.text = tag.Trim();
        statusLabel.color = col;
        labelObj.SetActive(true);

        // Nhuộm nhẹ màu băng/lửa/độc lên sprite nhân vật
        Color tint = IsFrosted ? new Color(0.65f, 0.92f, 1f, 1f)
                   : IsBurning ? new Color(1f, 0.78f, 0.55f, 1f)
                   : IsPoisoned ? new Color(0.7f, 1f, 0.72f, 1f)
                   : new Color(1f, 0.72f, 0.75f, 1f);

        if (renderers != null)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].gameObject != labelObj)
                {
                    var cur = renderers[i].color;
                    renderers[i].color = new Color(tint.r, tint.g, tint.b, cur.a);
                }
            }
        }
    }

    private void RestoreTint()
    {
        if (renderers == null || baseColors == null) return;
        for (int i = 0; i < renderers.Length && i < baseColors.Length; i++)
        {
            if (renderers[i] != null)
            {
                var cur = renderers[i].color;
                renderers[i].color = new Color(baseColors[i].r, baseColors[i].g, baseColors[i].b, cur.a);
            }
        }
    }
}
