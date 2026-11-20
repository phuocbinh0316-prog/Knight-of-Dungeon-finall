using UnityEngine;

/// <summary>
/// Đạn của enemy bắn xa & Boss. Hỗ trợ bị Parry phản ngược lại gây chí mạng cho kẻ địch!
/// Prefab cần: SpriteRenderer + Collider2D (Is Trigger = bật) + script này.
/// </summary>
public class Projectile : MonoBehaviour
{
    [SerializeField] private float lifetime = 4f;
    [SerializeField] private LayerMask groundLayer;

    private Vector2 direction = Vector2.right;
    private float speed = 8f;
    private float damage = 1f;
    private GameObject owner;

    public bool IsReflected { get; private set; }

    public void Init(Vector2 dir, float dmg, float spd, GameObject shooter = null)
    {
        direction = dir.normalized;
        damage = dmg;
        speed = spd;
        owner = shooter;
        IsReflected = false;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        Destroy(gameObject, lifetime);
    }

    /// <summary>Phản ngược viên đạn khi Player Parry thành công!</summary>
    public void Reflect(GameObject newOwner, float speedMultiplier = 1.85f, float damageMultiplier = 2.2f)
    {
        if (IsReflected) return;
        IsReflected = true;
        owner = newOwner;
        direction = -direction;
        speed *= speedMultiplier;
        damage *= damageMultiplier;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color(1f, 0.92f, 0.25f, 1f); // Đổi màu vàng kim rực rỡ
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;

        if (!IsReflected)
        {
            var player = other.GetComponentInParent<Player>();
            if (player != null)
            {
                var parry = player.GetComponent<ParryShield>();
                if (parry != null && parry.IsParrying)
                {
                    Reflect(player.gameObject);
                    parry.TryInterceptDamage(damage, owner);
                    return;
                }

                player.TakeDamageP(damage, transform.position, owner);
                Destroy(gameObject);
                return;
            }
        }
        else
        {
            // Đạn đã phản ngược -> gây sát thương chí mạng lên Enemy / Boss!
            GameObject root = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
            if (owner != null && root == owner) return;

            var e1 = root.GetComponent<PatrolEnemy1>();
            if (e1 != null)
            {
                e1.Stun(1.2f);
                e1.takeDamageE(damage, true, transform.position, bypassShield: true);
                Destroy(gameObject);
                return;
            }

            var boss = root.GetComponent<PatrolBoss>();
            if (boss != null)
            {
                boss.Stun(1.0f);
                boss.takeDamageE(damage, true);
                Destroy(gameObject);
                return;
            }

            var dmgable = root.GetComponent<IDamageable>();
            if (dmgable != null && root.GetComponent<Player>() == null)
            {
                var tel = root.GetComponent<EnemyTelegraph>();
                if (tel != null) tel.Stun(1.2f);
                dmgable.TakeDamage(damage, root.transform.position);
                DamagePopup.Spawn(root.transform.position, damage, Color.yellow, true);
                Destroy(gameObject);
                return;
            }
        }

        if (!other.isTrigger && (groundLayer.value & (1 << other.gameObject.layer)) != 0)
            Destroy(gameObject);
    }
}
