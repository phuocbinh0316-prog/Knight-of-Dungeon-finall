using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemySpawnEntry
{
    public GameObject prefab;
    [Min(1)] public int weight = 10;
    [Tooltip("Chỉ xuất hiện từ độ sâu này trở đi")] public int minDepth = 1;
}

/// <summary>
/// Ghép ngẫu nhiên các phòng thành 1 màn: Start -> (Combat/Treasure)... -> Boss.
/// Đặt trong scene "Run". Gán Player để dịch chuyển tới điểm bắt đầu.
/// </summary>
public class LevelGenerator : MonoBehaviour
{
    [Header("Phòng")]
    [SerializeField] private Room startRoom;
    [SerializeField] private Room[] combatRooms;
    [SerializeField] private Room[] treasureRooms;
    [SerializeField] private Room bossRoom;

    [Header("Cấu hình")]
    [SerializeField] private int baseCombatRooms = 4;
    [SerializeField] private int extraRoomsPerDepth = 1;
    [Range(0f, 1f)] [SerializeField] private float treasureChance = 0.6f;
    [Tooltip("0 = ngẫu nhiên mỗi lần")] [SerializeField] private int seed = 0;

    [Header("Enemy")]
    [SerializeField] private EnemySpawnEntry[] enemyPool;
    [Range(0f, 1f)] [SerializeField] private float baseSpawnChance = 0.6f;
    [SerializeField] private float spawnChancePerDepth = 0.05f;

    [Header("Loot kho báu")]
    [SerializeField] private Pickup weaponPickupPrefab;
    [SerializeField] private WeaponData[] weaponPool;

    [Header("Player")]
    [SerializeField] private Transform player;

    private readonly List<Room> built = new List<Room>();

    private void Start()
    {
        Generate();
    }

    public void Generate()
    {
        if (seed != 0) Random.InitState(seed);
        int depth = RunManager.Get().Depth;

        Room current = Place(startRoom, null);
        if (player != null && current != null && current.entry != null)
        {
            Vector3 spawn = current.entry.position + Vector3.up * 1f;
            var pl = player.GetComponent<Player>();
            if (pl != null) pl.SetSpawn(spawn);   // đặt vị trí + checkpoint khi rơi bẫy
            else player.position = spawn;
        }

        int combatCount = baseCombatRooms + extraRoomsPerDepth * (depth - 1);
        int treasureAt = (treasureRooms != null && treasureRooms.Length > 0 && Random.value < treasureChance)
            ? Random.Range(1, Mathf.Max(2, combatCount)) : -1;

        string lastName = null;
        for (int i = 0; i < combatCount; i++)
        {
            if (i == treasureAt)
                current = Place(PickRandom(treasureRooms, null), current);

            Room template = PickRandom(combatRooms, lastName);
            current = Place(template, current);
            lastName = template != null ? template.name : null;
        }

        Place(bossRoom, current);
    }

    private Room Place(Room template, Room previous)
    {
        if (template == null)
        {
            Debug.LogError("LevelGenerator: thiếu prefab phòng!");
            return previous;
        }

        Room room = Instantiate(template, Vector3.zero, Quaternion.identity, transform);
        room.name = template.name;

        if (previous != null && previous.exit != null && room.entry != null)
            room.transform.position += previous.exit.position - room.entry.position;

        built.Add(room);
        PopulateEnemies(room);
        PopulateLoot(room);
        return room;
    }

    private void PopulateEnemies(Room room)
    {
        if (room.type != RoomType.Combat || room.enemySpawnPoints == null) return;

        int depth = RunManager.Get().Depth;
        float chance = Mathf.Clamp01(baseSpawnChance + spawnChancePerDepth * (depth - 1));

        foreach (var point in room.enemySpawnPoints)
        {
            if (point == null || Random.value > chance) continue;
            GameObject prefab = PickEnemy(depth);
            if (prefab != null) Instantiate(prefab, point.position, Quaternion.identity, room.transform);
        }
    }

    private void PopulateLoot(Room room)
    {
        if (room.type != RoomType.Treasure || room.lootSpawnPoints == null) return;
        if (weaponPickupPrefab == null || weaponPool == null || weaponPool.Length == 0) return;

        foreach (var point in room.lootSpawnPoints)
        {
            if (point == null) continue;
            var pickup = Instantiate(weaponPickupPrefab, point.position, Quaternion.identity, room.transform);
            pickup.Setup(PickupType.Weapon, weaponPool[Random.Range(0, weaponPool.Length)]);
        }
    }

    private GameObject PickEnemy(int depth)
    {
        if (enemyPool == null) return null;
        int total = 0;
        foreach (var e in enemyPool) if (e.prefab != null && depth >= e.minDepth) total += e.weight;
        if (total <= 0) return null;

        int roll = Random.Range(0, total);
        foreach (var e in enemyPool)
        {
            if (e.prefab == null || depth < e.minDepth) continue;
            roll -= e.weight;
            if (roll < 0) return e.prefab;
        }
        return null;
    }

    private static Room PickRandom(Room[] list, string avoidName)
    {
        if (list == null || list.Length == 0) return null;
        for (int tries = 0; tries < 15; tries++)
        {
            var r = list[Random.Range(0, list.Length)];
            if (r != null && r.name != avoidName) return r;
        }
        foreach (var r in list)
        {
            if (r != null) return r;
        }
        return null;
    }
}
