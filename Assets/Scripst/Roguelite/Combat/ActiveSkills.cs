using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 2 Ô Kỹ năng chủ động (Active Skills) kiểu Dead Cells với hồi chiêu độc lập:
/// - Phím [Q] (hoặc [U] / Click ô BOMB): Fire/Shock Bomb — Ném bom lửa vòng cung nổ diện rộng, gây Thiêu đốt (Burn) + Choáng 0.8s + Phá khiên.
/// - Phím [E] (hoặc Click ô WAVE): Frost/Blood Wave — Phóng kiếm khí xuyên thấu nhiều kẻ địch, gây Chảy máu (Bleed) + Làm chậm (Frost).
/// </summary>
public class ActiveSkills : MonoBehaviour
{
    [Header("Skill Q: Fire Bomb")]
    [SerializeField] private float grenadeCooldown = 5.5f;
    [SerializeField] private float grenadeDamageMult = 2.1f;
    [SerializeField] private float grenadeExplosionRadius = 3.5f;

    [Header("Skill E: Frost/Blood Wave")]
    [SerializeField] private float waveCooldown = 4.5f;
    [SerializeField] private float waveDamageMult = 1.6f;
    [SerializeField] private float waveSpeed = 16f;

    private Player player;
    private float nextGrenadeTime;
    private float nextWaveTime;

    public float GrenadeCooldownRemaining => Mathf.Max(0f, nextGrenadeTime - Time.time);
    public float GrenadeCooldownNormalized
    {
        get
        {
            float eff = grenadeCooldown * ScrollUpgradeUI.TacticsCooldownMultiplier;
            return eff > 0f ? Mathf.Clamp01(1f - GrenadeCooldownRemaining / eff) : 1f;
        }
    }

    public float WaveCooldownRemaining => Mathf.Max(0f, nextWaveTime - Time.time);
    public float WaveCooldownNormalized
    {
        get
        {
            float eff = waveCooldown * ScrollUpgradeUI.TacticsCooldownMultiplier;
            return eff > 0f ? Mathf.Clamp01(1f - WaveCooldownRemaining / eff) : 1f;
        }
    }

    private void Awake()
    {
        player = GetComponent<Player>();
    }

    private void Update()
    {
        if (Time.timeScale <= 0f || player == null) return;

        if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.U))
        {
            TryCastBomb();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            TryCastWave();
        }
    }

    public bool TryCastBomb()
    {
        if (Time.timeScale <= 0f || player == null || Time.time < nextGrenadeTime) return false;
        CastFireGrenade();
        return true;
    }

    public bool TryCastWave()
    {
        if (Time.timeScale <= 0f || player == null || Time.time < nextWaveTime) return false;
        CastFrostBloodWave();
        return true;
    }

    private void CastFireGrenade()
    {
        nextGrenadeTime = Time.time + grenadeCooldown * ScrollUpgradeUI.TacticsCooldownMultiplier;

        float dir = player.IsFacingRight ? 1f : -1f;
        float dmg = player.GetBaseDamage() * grenadeDamageMult * MetaUpgrades.DamageMultiplier * ScrollUpgradeUI.TacticsSkillDamageMultiplier;

        var go = new GameObject("Skill_FireGrenade");
        go.transform.position = transform.position + new Vector3(dir * 0.65f, 0.7f, 0f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CombatVFX.GetBombSprite();
        sr.color = Color.white;
        CombatVFX.ApplyVfxLayer(sr, 62);
        go.transform.localScale = Vector3.one * 0.9f;

        var bomb = go.AddComponent<FireGrenadeProjectile>();
        bomb.Init(player, new Vector2(dir * 10.5f, 6.2f), dmg, grenadeExplosionRadius);

        CombatVFX.SpawnBurst(go.transform.position, new Color(1f, 0.65f, 0.15f, 0.9f), 5, 1.5f, 3.5f, 0.16f, 0.25f, 3f);
        if (AudioManager.Instance != null) AudioManager.Instance.SFX_Attack();
    }

    private void CastFrostBloodWave()
    {
        nextWaveTime = Time.time + waveCooldown * ScrollUpgradeUI.TacticsCooldownMultiplier;

        float dir = player.IsFacingRight ? 1f : -1f;
        float dmg = player.GetBaseDamage() * waveDamageMult * MetaUpgrades.DamageMultiplier * ScrollUpgradeUI.TacticsSkillDamageMultiplier;

        var go = new GameObject("Skill_FrostBloodWave");
        go.transform.position = transform.position + new Vector3(dir * 0.9f, 0.2f, 0f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CombatVFX.GetSlashSprite();
        sr.color = new Color(0.45f, 0.92f, 1f, 0.95f);
        sr.flipX = dir < 0f;
        CombatVFX.ApplyVfxLayer(sr, 60);
        go.transform.localScale = new Vector3(1.5f, 1.8f, 1f);

        var wave = go.AddComponent<FrostWaveProjectile>();
        wave.Init(player, dir, waveSpeed, dmg);

        CameraShake.Shake(0.18f);
        if (AudioManager.Instance != null) AudioManager.Instance.SFX_Attack();
    }
}

/// <summary>
/// Bom lửa dùng quỹ đạo parabol xác định (không bị lỗi mất va chạm Rigidbody2D hay tự nổ trúng Player).
/// Chạm quái vật hoặc mặt đất sẽ nổ diện rộng ngay lập tức!
/// </summary>
public class FireGrenadeProjectile : MonoBehaviour
{
    private Player owner;
    private Vector2 velocity;
    private float damage;
    private float radius;
    private float elapsed;
    private bool exploded;

    private const float Gravity = 20f;
    private const float ArmDelay = 0.06f;
    private const float MaxLifetime = 2.0f;
    private const float HitCheckRadius = 0.38f;

    public void Init(Player p, Vector2 initialVelocity, float dmg, float rad)
    {
        owner = p;
        velocity = initialVelocity;
        damage = dmg;
        radius = rad;
    }

    private void Update()
    {
        if (exploded) return;

        float dt = Time.deltaTime;
        elapsed += dt;

        // Di chuyển parabol mượt mà và xoay quả bom theo chiều bay
        velocity.y -= Gravity * dt;
        transform.position += (Vector3)(velocity * dt);
        transform.Rotate(0f, 0f, -Mathf.Sign(velocity.x) * 420f * dt);

        // Tia lửa đuôi bom
        if (Random.value < 0.55f)
        {
            CombatVFX.SpawnBurst(transform.position, new Color(1f, 0.65f, 0.15f, 0.9f), 1, 0.6f, 1.8f, 0.15f, 0.22f, 0f);
        }

        if (elapsed >= MaxLifetime)
        {
            Explode();
            return;
        }

        // Sau 0.06s kích hoạt cảm biến chạm Quái hoặc Tường/Sàn
        if (elapsed >= ArmDelay)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, HitCheckRadius);
            foreach (var h in hits)
            {
                if (h == null) continue;
                GameObject root = h.attachedRigidbody != null ? h.attachedRigidbody.gameObject : h.gameObject;
                if (owner != null && (root == owner.gameObject || h.transform.IsChildOf(owner.transform))) continue;

                bool isEnemy = root.GetComponent<PatrolEnemy1>() != null
                            || root.GetComponent<PatrolBoss>() != null
                            || root.GetComponent<RangedEnemy>() != null
                            || root.GetComponent<IDamageable>() != null;

                if (isEnemy || !h.isTrigger)
                {
                    Explode();
                    return;
                }
            }
        }
    }

    private void Explode()
    {
        if (exploded) return;
        exploded = true;

        CameraShake.Shake(0.42f);
        HitStop.Do(0.06f);
        CombatVFX.SpawnShockwave(transform.position, new Color(1f, 0.5f, 0.1f, 0.95f), 0.4f, radius * 1.45f, 0.32f);
        CombatVFX.SpawnBurst(transform.position, new Color(1f, 0.45f, 0.1f, 1f), 20, 3f, 9.5f, 0.28f, 0.45f, 6f);
        if (AudioManager.Instance != null) AudioManager.Instance.SFX_Hit();

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
        var done = new HashSet<GameObject>();

        foreach (var h in hits)
        {
            if (h == null) continue;
            GameObject root = h.attachedRigidbody != null ? h.attachedRigidbody.gameObject : h.gameObject;
            if (owner != null && root == owner.gameObject) continue;
            if (!done.Add(root)) continue;

            bool isEnemy = false;

            var shield = root.GetComponent<ShieldedEnemy>();
            if (shield != null) shield.BreakGuard(0.8f);

            var e1 = root.GetComponent<PatrolEnemy1>();
            if (e1 != null && !e1.IsDead)
            {
                isEnemy = true;
                e1.Stun(0.85f);
                e1.takeDamageE(damage, true, transform.position, bypassShield: true);
            }
            else
            {
                var boss = root.GetComponent<PatrolBoss>();
                if (boss != null && boss.IsAlive)
                {
                    isEnemy = true;
                    boss.takeDamageE(damage, true);
                }
                else
                {
                    var d = root.GetComponent<IDamageable>();
                    if (d != null)
                    {
                        isEnemy = true;
                        d.TakeDamage(damage, root.transform.position);
                        DamagePopup.Spawn(root.transform.position, damage, new Color(1f, 0.55f, 0.1f), true);
                    }
                }
            }

            if (isEnemy)
            {
                var status = StatusEffectController.GetOrAdd(root);
                if (status != null) status.Apply(StatusType.Burn, 4.0f, 0.85f);
                if (owner != null) owner.NotifyDamageDealt(damage);
            }
        }

        Destroy(gameObject);
    }
}

public class FrostWaveProjectile : MonoBehaviour
{
    private Player owner;
    private float direction;
    private float speed;
    private float damage;
    private float lifetime = 0.75f;
    private float elapsed;
    private readonly HashSet<GameObject> hitTargets = new HashSet<GameObject>();

    public void Init(Player p, float dir, float spd, float dmg)
    {
        owner = p;
        direction = dir;
        speed = spd;
        damage = dmg;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        elapsed += dt;
        transform.position += new Vector3(direction * speed * dt, 0f, 0f);

        if (Random.value < 0.55f)
        {
            CombatVFX.SpawnBurst(transform.position, new Color(0.5f, 0.92f, 1f, 0.8f), 1, 0.5f, 2f, 0.16f, 0.22f, 1f);
        }

        // Quét va chạm chủ động từng frame (không phụ thuộc OnTriggerEnter2D)
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.95f);
        foreach (var h in hits)
        {
            if (h == null) continue;
            GameObject root = h.attachedRigidbody != null ? h.attachedRigidbody.gameObject : h.gameObject;
            if (owner != null && root == owner.gameObject) continue;
            if (!hitTargets.Add(root)) continue;

            HitTarget(root);
        }

        if (elapsed >= lifetime) Destroy(gameObject);
    }

    private void HitTarget(GameObject root)
    {
        bool hitEnemy = false;

        var e1 = root.GetComponent<PatrolEnemy1>();
        if (e1 != null && !e1.IsDead)
        {
            hitEnemy = true;
            e1.takeDamageE(damage, false, transform.position, bypassShield: true);
        }
        else
        {
            var boss = root.GetComponent<PatrolBoss>();
            if (boss != null && boss.IsAlive)
            {
                hitEnemy = true;
                boss.takeDamageE(damage, false);
            }
            else
            {
                var d = root.GetComponent<IDamageable>();
                if (d != null)
                {
                    hitEnemy = true;
                    d.TakeDamage(damage, root.transform.position);
                    DamagePopup.Spawn(root.transform.position, damage, new Color(0.45f, 0.92f, 1f), false);
                }
            }
        }

        if (hitEnemy)
        {
            var status = StatusEffectController.GetOrAdd(root);
            if (status != null)
            {
                status.Apply(StatusType.Frost, 3.5f);
                status.Apply(StatusType.Bleed, 3.5f, 0.65f);
            }
            CombatVFX.SpawnBurst(root.transform.position, new Color(0.45f, 0.92f, 1f, 0.95f), 8, 2f, 5f, 0.18f, 0.3f, 3f);
            if (owner != null) owner.NotifyDamageDealt(damage);
        }
    }
}
