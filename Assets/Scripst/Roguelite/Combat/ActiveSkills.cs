using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 2 Ô Kỹ năng chủ động (Active Skills) kiểu Dead Cells với hồi chiêu độc lập:
/// - Phím [Q]: Fire/Shock Grenade — Ném lựu đạn vòng cung nổ diện rộng, gây Thiêu đốt (Burn) + Choáng 0.8s.
/// - Phím [E]: Frost/Blood Wave — Phóng kiếm khí xuyên thấu nhiều kẻ địch, gây Chảy máu (Bleed) + Làm chậm (Frost).
/// </summary>
public class ActiveSkills : MonoBehaviour
{
    [Header("Skill Q: Fire Grenade")]
    [SerializeField] private float grenadeCooldown = 6f;
    [SerializeField] private float grenadeDamageMult = 2.0f;
    [SerializeField] private float grenadeExplosionRadius = 3.2f;

    [Header("Skill E: Frost/Blood Wave")]
    [SerializeField] private float waveCooldown = 4.5f;
    [SerializeField] private float waveDamageMult = 1.6f;
    [SerializeField] private float waveSpeed = 16f;

    private Player player;
    private float nextGrenadeTime;
    private float nextWaveTime;

    public float GrenadeCooldownRemaining => Mathf.Max(0f, nextGrenadeTime - Time.time);
    public float GrenadeCooldownNormalized => grenadeCooldown > 0f ? Mathf.Clamp01(1f - GrenadeCooldownRemaining / (grenadeCooldown * ScrollUpgradeUI.TacticsCooldownMultiplier)) : 1f;

    public float WaveCooldownRemaining => Mathf.Max(0f, nextWaveTime - Time.time);
    public float WaveCooldownNormalized => waveCooldown > 0f ? Mathf.Clamp01(1f - WaveCooldownRemaining / (waveCooldown * ScrollUpgradeUI.TacticsCooldownMultiplier)) : 1f;

    private void Awake()
    {
        player = GetComponent<Player>();
    }

    private void Update()
    {
        if (Time.timeScale <= 0f || player == null) return;

        if (Input.GetKeyDown(KeyCode.Q) && Time.time >= nextGrenadeTime)
        {
            CastFireGrenade();
        }

        if (Input.GetKeyDown(KeyCode.E) && Time.time >= nextWaveTime)
        {
            CastFrostBloodWave();
        }
    }

    private void CastFireGrenade()
    {
        nextGrenadeTime = Time.time + grenadeCooldown * ScrollUpgradeUI.TacticsCooldownMultiplier;

        float dir = player.IsFacingRight ? 1f : -1f;
        float dmg = player.GetBaseDamage() * grenadeDamageMult * MetaUpgrades.DamageMultiplier * ScrollUpgradeUI.TacticsSkillDamageMultiplier;

        var go = new GameObject("Skill_FireGrenade");
        go.transform.position = transform.position + new Vector3(dir * 0.6f, 0.5f, 0f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CombatVFX.GetCircleSprite();
        sr.color = new Color(1f, 0.45f, 0.1f, 1f);
        sr.sortingOrder = 58;
        go.transform.localScale = Vector3.one * 0.48f;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 2.2f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.linearVelocity = new Vector2(dir * 10.5f, 6.8f);

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.25f;
        col.isTrigger = true;

        var bomb = go.AddComponent<FireGrenadeProjectile>();
        bomb.Init(player, dmg, grenadeExplosionRadius);

        if (AudioManager.Instance != null) AudioManager.Instance.SFX_Attack();
    }

    private void CastFrostBloodWave()
    {
        nextWaveTime = Time.time + waveCooldown * ScrollUpgradeUI.TacticsCooldownMultiplier;

        float dir = player.IsFacingRight ? 1f : -1f;
        float dmg = player.GetBaseDamage() * waveDamageMult * MetaUpgrades.DamageMultiplier * ScrollUpgradeUI.TacticsSkillDamageMultiplier;

        var go = new GameObject("Skill_FrostBloodWave");
        go.transform.position = transform.position + new Vector3(dir * 0.9f, 0.1f, 0f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CombatVFX.GetSlashSprite();
        sr.color = new Color(0.45f, 0.92f, 1f, 0.95f);
        sr.flipX = dir < 0f;
        sr.sortingOrder = 60;
        go.transform.localScale = new Vector3(1.5f, 1.8f, 1f);

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.75f;
        col.isTrigger = true;

        var wave = go.AddComponent<FrostWaveProjectile>();
        wave.Init(player, dir, waveSpeed, dmg);

        CameraShake.Shake(0.18f);
        if (AudioManager.Instance != null) AudioManager.Instance.SFX_Attack();
    }
}

public class FireGrenadeProjectile : MonoBehaviour
{
    private Player owner;
    private float damage;
    private float radius;
    private float spawnTime;
    private bool exploded;

    public void Init(Player p, float dmg, float rad)
    {
        owner = p;
        damage = dmg;
        radius = rad;
        spawnTime = Time.time;
    }

    private void Update()
    {
        // Đuôi lửa bay theo bom
        if (Random.value < 0.45f)
        {
            CombatVFX.SpawnBurst(transform.position, new Color(1f, 0.65f, 0.15f, 0.85f), 1, 0.5f, 1.5f, 0.14f, 0.2f, 0f);
        }

        if (Time.time - spawnTime > 2.2f) Explode();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (exploded || other == null) return;
        if (owner != null && (other.gameObject == owner.gameObject || other.transform.IsChildOf(owner.transform))) return;
        if (other.isTrigger && other.GetComponentInParent<PatrolEnemy1>() == null && other.GetComponentInParent<PatrolBoss>() == null && other.GetComponentInParent<IDamageable>() == null) return;

        Explode();
    }

    private void Explode()
    {
        if (exploded) return;
        exploded = true;

        CameraShake.Shake(0.42f);
        HitStop.Do(0.06f);
        CombatVFX.SpawnShockwave(transform.position, new Color(1f, 0.5f, 0.1f, 0.95f), 0.35f, radius * 1.4f, 0.3f);
        CombatVFX.SpawnBurst(transform.position, new Color(1f, 0.45f, 0.1f, 1f), 18, 3f, 9f, 0.26f, 0.45f, 6f);
        if (AudioManager.Instance != null) AudioManager.Instance.SFX_Hit();

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
        var done = new HashSet<GameObject>();

        foreach (var h in hits)
        {
            GameObject root = h.attachedRigidbody != null ? h.attachedRigidbody.gameObject : h.gameObject;
            if (owner != null && root == owner.gameObject) continue;
            if (!done.Add(root)) continue;

            bool isEnemy = false;

            var shield = root.GetComponent<ShieldedEnemy>();
            if (shield != null) shield.BreakGuard(0.8f);

            var e1 = root.GetComponent<PatrolEnemy1>();
            if (e1 != null)
            {
                isEnemy = true;
                e1.Stun(0.8f);
                e1.takeDamageE(damage, true, transform.position, bypassShield: true);
            }
            else
            {
                var boss = root.GetComponent<PatrolBoss>();
                if (boss != null)
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

        if (elapsed >= lifetime) Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;
        GameObject root = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
        if (owner != null && root == owner.gameObject) return;
        if (!hitTargets.Add(root)) return;

        bool hitEnemy = false;

        var e1 = root.GetComponent<PatrolEnemy1>();
        if (e1 != null)
        {
            hitEnemy = true;
            e1.takeDamageE(damage, false, transform.position, bypassShield: true);
        }
        else
        {
            var boss = root.GetComponent<PatrolBoss>();
            if (boss != null)
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
