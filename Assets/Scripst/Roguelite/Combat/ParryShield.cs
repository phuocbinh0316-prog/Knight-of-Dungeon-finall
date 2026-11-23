using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cơ chế Parry / Khiên phản đòn chuẩn thời điểm kiểu Dead Cells.
/// Phím: [L] hoặc [Chuột Phải].
/// - Chặn 100% sát thương trong khung parry (0.26s).
/// - Làm choáng (STUN đỏ 1.5s) và phản sát thương lại kẻ tấn công gần đó.
/// - Phản ngược đạn của RangedEnemy bay về gây chí mạng!
/// </summary>
public class ParryShield : MonoBehaviour
{
    [Header("Parry Settings")]
    [SerializeField] private float parryWindow = 0.26f;
    [SerializeField] private float cooldown = 0.65f;
    [SerializeField] private float counterRadius = 3.2f;
    [SerializeField] private float parryStunDuration = 1.5f;
    [SerializeField] private float counterDamageMultiplier = 1.5f;

    public bool IsParrying { get; private set; }
    public float CooldownRemaining => Mathf.Max(0f, nextParryTime - Time.time);
    public float CooldownNormalized => cooldown > 0f ? Mathf.Clamp01(1f - CooldownRemaining / cooldown) : 1f;

    private Player player;
    private float nextParryTime;
    private GameObject shieldVisualObj;
    private SpriteRenderer shieldVisualSr;

    private void Awake()
    {
        player = GetComponent<Player>();
        CreateShieldVisual();
    }

    private void CreateShieldVisual()
    {
        shieldVisualObj = new GameObject("ParryShieldVisual");
        shieldVisualObj.transform.SetParent(transform);
        shieldVisualObj.transform.localPosition = new Vector3(0.55f, 0.1f, 0f);
        shieldVisualObj.transform.localScale = new Vector3(0.75f, 1.35f, 1f);

        shieldVisualSr = shieldVisualObj.AddComponent<SpriteRenderer>();
        shieldVisualSr.sprite = CombatVFX.GetRingSprite();
        shieldVisualSr.color = new Color(0.35f, 0.85f, 1f, 0.9f);
        CombatVFX.ApplyVfxLayer(shieldVisualSr, 65);
        shieldVisualObj.SetActive(false);
    }

    public bool TryTriggerParry()
    {
        if (Time.timeScale <= 0f || IsParrying || Time.time < nextParryTime) return false;
        StartCoroutine(ParryRoutine());
        return true;
    }

    private void Update()
    {
        if (Time.timeScale <= 0f) return;

        bool pressed = Input.GetKeyDown(KeyCode.L) || Input.GetMouseButtonDown(1);
        if (pressed)
        {
            TryTriggerParry();
        }

        // Quét đạn đang bay tới trong khung Parry để phản ngược ngay lập tức
        if (IsParrying)
        {
            ReflectNearbyProjectiles();
        }
    }

    private IEnumerator ParryRoutine()
    {
        IsParrying = true;
        float cdMult = ScrollUpgradeUI.TacticsCooldownMultiplier;
        nextParryTime = Time.time + cooldown * cdMult;

        if (shieldVisualObj != null)
        {
            shieldVisualObj.SetActive(true);
            shieldVisualSr.color = new Color(0.35f, 0.88f, 1f, 0.92f);
        }

        if (AudioManager.Instance != null) AudioManager.Instance.SFX_Attack();

        float t = 0f;
        while (t < parryWindow)
        {
            t += Time.deltaTime;
            if (shieldVisualObj != null)
            {
                float s = 1f + 0.12f * Mathf.Sin(t * 45f);
                shieldVisualObj.transform.localScale = new Vector3(0.75f * s, 1.35f * s, 1f);
            }
            yield return null;
        }

        if (shieldVisualObj != null) shieldVisualObj.SetActive(false);
        IsParrying = false;
    }

    /// <summary>Gọi từ Player.TakeDamageP — trả về true nếu đã Parry thành công (chặn đòn).</summary>
    public bool TryInterceptDamage(float incomingDamage, GameObject attacker = null)
    {
        if (!IsParrying) return false;

        // Parry thành công: reset ngay cooldown để thưởng người chơi giỏi!
        nextParryTime = Time.time + 0.15f;

        TriggerParrySuccessEffects(attacker);
        return true;
    }

    private void ReflectNearbyProjectiles()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 2.2f);
        foreach (var h in hits)
        {
            var proj = h.GetComponent<Projectile>();
            if (proj != null && !proj.IsReflected)
            {
                proj.Reflect(gameObject, 1.85f, 2.2f);
                nextParryTime = Time.time + 0.15f;
                TriggerParrySuccessEffects(null);
            }
        }
    }

    private void TriggerParrySuccessEffects(GameObject directAttacker)
    {
        HitStop.Do(0.11f);
        CameraShake.Shake(0.45f);
        DamagePopup.SpawnText(transform.position + Vector3.up * 0.9f, "PARRY!", new Color(1f, 0.9f, 0.2f), 8.2f);
        CombatVFX.SpawnShockwave(transform.position, new Color(1f, 0.9f, 0.25f, 0.95f), 0.35f, 2.6f, 0.28f);
        CombatVFX.SpawnBurst(transform.position, new Color(1f, 0.92f, 0.35f, 1f), 14, 3f, 8f, 0.22f, 0.38f, 4f);
        if (AudioManager.Instance != null) AudioManager.Instance.SFX_Hit();

        KillStreakManager.Get().RegisterParry();

        float counterDmg = (player != null ? player.GetBaseDamage() : 2f) * counterDamageMultiplier * MetaUpgrades.DamageMultiplier * ScrollUpgradeUI.BrutalityDamageMultiplier;

        var done = new HashSet<GameObject>();
        if (directAttacker != null) ApplyParryCounter(directAttacker, counterDmg, done);

        Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, counterRadius);
        foreach (var h in nearby)
        {
            GameObject root = h.attachedRigidbody != null ? h.attachedRigidbody.gameObject : h.gameObject;
            if (root == gameObject) continue;
            ApplyParryCounter(root, counterDmg, done);
        }
    }

    private void ApplyParryCounter(GameObject root, float counterDmg, HashSet<GameObject> done)
    {
        if (root == null || root == gameObject || !done.Add(root)) return;

        var shield = root.GetComponent<ShieldedEnemy>();
        if (shield != null) shield.BreakGuard(parryStunDuration);

        var telegraph = root.GetComponent<EnemyTelegraph>();
        if (telegraph != null) telegraph.Stun(parryStunDuration);

        var e1 = root.GetComponent<PatrolEnemy1>();
        if (e1 != null)
        {
            e1.Stun(parryStunDuration);
            e1.takeDamageE(counterDmg, true, transform.position, bypassShield: true);
            if (player != null) player.NotifyDamageDealt(counterDmg);
            return;
        }

        var boss = root.GetComponent<PatrolBoss>();
        if (boss != null)
        {
            boss.Stun(parryStunDuration * 0.8f);
            boss.takeDamageE(counterDmg, true);
            if (player != null) player.NotifyDamageDealt(counterDmg);
            return;
        }

        var dmgable = root.GetComponent<IDamageable>();
        if (dmgable != null)
        {
            dmgable.TakeDamage(counterDmg, root.transform.position);
            DamagePopup.Spawn(root.transform.position, counterDmg, Color.yellow, true);
            if (player != null) player.NotifyDamageDealt(counterDmg);
        }
    }
}
