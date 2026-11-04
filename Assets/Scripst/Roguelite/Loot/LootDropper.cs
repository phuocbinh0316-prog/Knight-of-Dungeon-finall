using UnityEngine;

/// <summary>Gắn vào enemy mới: gọi Drop() khi chết (RangedEnemy tự gọi).</summary>
public class LootDropper : MonoBehaviour
{
    [SerializeField] private Pickup cellPrefab;
    [SerializeField] private Vector2Int cellCount = new Vector2Int(1, 3);

    [SerializeField] private Pickup heartPrefab;
    [Range(0f, 1f)] [SerializeField] private float heartChance = 0.12f;

    [SerializeField] private float popForce = 5f;

    public void Drop()
    {
        if (cellPrefab != null)
        {
            int n = Random.Range(cellCount.x, cellCount.y + 1);
            for (int i = 0; i < n; i++) Spawn(cellPrefab);
        }

        if (heartPrefab != null && Random.value <= heartChance) Spawn(heartPrefab);
    }

    private void Spawn(Pickup prefab)
    {
        var p = Instantiate(prefab, transform.position + Vector3.up * 0.4f, Quaternion.identity);
        var rb = p.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.AddForce(new Vector2(Random.Range(-1f, 1f), Random.Range(0.8f, 1.5f)) * popForce, ForceMode2D.Impulse);
    }
}
