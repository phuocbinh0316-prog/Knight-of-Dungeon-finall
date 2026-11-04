using UnityEngine;

public enum PickupType { Cell, Heart, Weapon }

/// <summary>
/// Vật phẩm nhặt được. Prefab cần: Rigidbody2D (Dynamic), 1 collider thường (rơi xuống đất)
/// + 1 collider Is Trigger (để nhặt). Cell tự bay về Player khi lại gần.
/// Vũ khí: đứng gần và bấm E để đổi.
/// </summary>
public class Pickup : MonoBehaviour
{
    [SerializeField] private PickupType type = PickupType.Cell;
    [SerializeField] private int amount = 1;
    [SerializeField] private float healAmount = 1f;
    [SerializeField] private WeaponData weapon;
    [SerializeField] private float magnetRange = 3.5f;
    [SerializeField] private float magnetSpeed = 12f;

    private Player player;
    private Rigidbody2D rb;
    private bool playerInside;

    public void Setup(PickupType t, WeaponData w)
    {
        type = t;
        weapon = w;
    }

    private void Start()
    {
        player = FindAnyObjectByType<Player>();
        rb = GetComponent<Rigidbody2D>();

        if (player != null)
        {
            var mine = GetComponents<Collider2D>();
            var theirs = player.GetComponentsInChildren<Collider2D>();
            foreach (var a in mine)
                foreach (var b in theirs)
                    if (!a.isTrigger) Physics2D.IgnoreCollision(a, b, true);
        }
    }

    private void Update()
    {
        if (player == null) return;

        if (type == PickupType.Cell &&
            Vector2.Distance(transform.position, player.transform.position) < magnetRange)
        {
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.linearVelocity = Vector2.zero;
            }
            transform.position = Vector3.MoveTowards(
                transform.position, player.transform.position + Vector3.up * 0.5f,
                magnetSpeed * Time.deltaTime);
        }

        if (type == PickupType.Weapon && playerInside && Input.GetKeyDown(KeyCode.E))
            EquipWeapon();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var p = other.GetComponentInParent<Player>();
        if (p == null) return;

        switch (type)
        {
            case PickupType.Cell:
                RunManager.Get().AddCells(amount);
                Destroy(gameObject);
                break;
            case PickupType.Heart:
                p.Heal(healAmount);
                Destroy(gameObject);
                break;
            case PickupType.Weapon:
                playerInside = true;
                break;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<Player>() != null) playerInside = false;
    }

    private void EquipWeapon()
    {
        var holder = player.GetComponent<WeaponHolder>();
        if (holder == null || weapon == null) return;
        holder.Equip(weapon);
        Destroy(gameObject);
    }
}
