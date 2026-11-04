using UnityEngine;

/// <summary>
/// Đạn của enemy bắn xa. Prefab cần: SpriteRenderer + Collider2D (Is Trigger = bật) + script này.
/// </summary>
public class Projectile : MonoBehaviour
{
    [SerializeField] private float lifetime = 4f;
    [SerializeField] private LayerMask groundLayer;

    private Vector2 direction = Vector2.right;
    private float speed = 8f;
    private float damage = 1f;

    public void Init(Vector2 dir, float dmg, float spd)
    {
        direction = dir.normalized;
        damage = dmg;
        speed = spd;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponentInParent<Player>();
        if (player != null)
        {
            player.TakeDamageP(damage);
            Destroy(gameObject);
            return;
        }

        if (!other.isTrigger && (groundLayer.value & (1 << other.gameObject.layer)) != 0)
            Destroy(gameObject);
    }
}
