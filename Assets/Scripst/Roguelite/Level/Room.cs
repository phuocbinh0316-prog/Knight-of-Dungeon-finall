using UnityEngine;

public enum RoomType { Start, Combat, Treasure, Boss }

/// <summary>
/// Gắn vào root của mỗi prefab phòng.
/// entry = điểm vào (bên trái), exit = điểm ra (bên phải). Hai phòng nối nhau khi exit trùng entry.
/// </summary>
public class Room : MonoBehaviour
{
    public RoomType type = RoomType.Combat;
    public Transform entry;
    public Transform exit;
    [Tooltip("Vị trí có thể sinh enemy")] public Transform[] enemySpawnPoints;
    [Tooltip("Vị trí sinh loot/vũ khí (phòng kho báu)")] public Transform[] lootSpawnPoints;

    private void OnDrawGizmos()
    {
        if (entry != null) { Gizmos.color = Color.green; Gizmos.DrawWireSphere(entry.position, 0.4f); }
        if (exit != null) { Gizmos.color = Color.red; Gizmos.DrawWireSphere(exit.position, 0.4f); }
        Gizmos.color = Color.yellow;
        if (enemySpawnPoints != null)
            foreach (var p in enemySpawnPoints) if (p != null) Gizmos.DrawWireCube(p.position, Vector3.one * 0.5f);
    }
}
