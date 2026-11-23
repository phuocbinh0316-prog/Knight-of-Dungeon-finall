using UnityEngine;

/// <summary>
/// Enemy bắn xa: giữ khoảng cách, quay mặt về Player và bắn đạn (có thể bị Player Parry phản ngược!).
/// Cần: Health, Collider2D, đặt layer = Enemy để Player chém được.
/// </summary>
[RequireComponent(typeof(Health))]
public class RangedEnemy : MonoBehaviour
{
    [Header("Mục tiêu")]
    [SerializeField] private Transform player;
    [SerializeField] private float detectRange = 14f;
    [SerializeField] private float preferredDistance = 7f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private bool spriteFacesRight = true;

    [Header("Bắn")]
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireInterval = 1.8f;
    [SerializeField] private float projectileSpeed = 8f;
    [SerializeField] private float projectileDamage = 1f;

    private Health health;
    private LootDropper loot;
    private EnemyTelegraph telegraph;
    private StatusEffectController status;
    private float nextFireTime;
    private bool dead;

    private void Awake()
    {
        health = GetComponent<Health>();
        loot = GetComponent<LootDropper>();
        telegraph = GetComponent<EnemyTelegraph>() ?? gameObject.AddComponent<EnemyTelegraph>();
        status = GetComponent<StatusEffectController>();
        health.onDied.AddListener(OnDied);
    }

    private void Start()
    {
        if (player == null)
        {
            var p = FindAnyObjectByType<Player>();
            if (p != null) player = p.transform;
        }
        nextFireTime = Time.time + Random.Range(0.5f, fireInterval);
    }

    private void Update()
    {
        if (dead || player == null) return;
        if (telegraph != null && telegraph.IsStunned) return;

        float dx = player.position.x - transform.position.x;
        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > detectRange)
        {
            if (telegraph != null) telegraph.CancelWarning();
            return;
        }

        bool wantRight = dx >= 0f;
        bool faceRight = spriteFacesRight ? wantRight : !wantRight;
        transform.eulerAngles = new Vector3(0f, faceRight ? 0f : 180f, 0f);

        if (status == null) status = GetComponent<StatusEffectController>();
        float spd = moveSpeed * (status != null ? status.SpeedMultiplier : 1f);

        if (dist < preferredDistance - 1.5f)
            transform.position += new Vector3(-Mathf.Sign(dx), 0f, 0f) * spd * Time.deltaTime;
        else if (dist > preferredDistance + 1.5f)
            transform.position += new Vector3(Mathf.Sign(dx), 0f, 0f) * spd * Time.deltaTime;

        if (Time.time >= nextFireTime)
        {
            if (telegraph != null && !telegraph.IsPreparingAttack)
            {
                telegraph.ShowWarning(0.35f, () =>
                {
                    if (!dead && player != null && (telegraph == null || !telegraph.IsStunned))
                    {
                        Shoot();
                    }
                });
                nextFireTime = Time.time + fireInterval;
            }
            else if (telegraph == null)
            {
                Shoot();
                nextFireTime = Time.time + fireInterval;
            }
        }
    }

    private void Shoot()
    {
        if (projectilePrefab == null) return;
        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        var proj = Instantiate(projectilePrefab, origin, Quaternion.identity);
        Vector2 dir = (player.position + Vector3.up * 0.5f) - origin;
        proj.Init(dir, projectileDamage, projectileSpeed, gameObject);
    }

    private void OnDied()
    {
        if (dead) return;
        dead = true;
        if (telegraph != null) telegraph.CancelWarning();
        if (loot != null) loot.Drop();
        RunManager.Get().AddGold(6);
        CurseSystem.OnEnemyKilled();
        var st = GetComponent<StatusEffectController>();
        MutationSystem.OnEnemyKilled(st != null && st.HasAnyAffliction, transform.position);
        var elite = GetComponent<EliteEnemy>();
        if (elite != null) elite.OnEliteKilled();
        KillStreakManager.Get().RegisterKill(transform.position);
        Destroy(gameObject, 0.05f);
    }
}
