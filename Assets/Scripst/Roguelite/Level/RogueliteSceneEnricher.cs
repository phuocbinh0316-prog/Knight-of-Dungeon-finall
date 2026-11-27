using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Bộ gieo nội dung Roguelite NGẪU NHIÊN tự động cho mọi màn chơi (map 1, map 2, map 3, Dungeon_Level_01, Run):
/// - Luôn đặt 1 Cửa Hàng Thương Nhân (InRunMerchant) ở NGAY ĐẦU MỖI MAP (cạnh điểm xuất phát của Player).
/// - Quét toàn bộ địa hình sàn & bệ nhảy hợp lệ của màn, xáo trộn ngẫu nhiên mỗi lượt chơi để đặt:
///   + Power Scrolls (Cuộn Sức Mạnh chọn 1 trong 3 nhánh)
///   + Treasure Chests (Rương Kho Báu Vàng rơi Vàng + Hồi máu/Flask/Relic)
///   + Cursed Chests (Rương Nguyền Rủa thưởng lớn kèm Curse x2.5 DMG)
///   + Quái Tinh Anh ★ ELITE ★ (chọn ngẫu nhiên trong đàn quái của màn!)
/// - 100% không sửa file .unity gốc trên đĩa; nếu bạn tự đặt tay trong Scene thì tự động tôn trọng, không tạo trùng.
/// </summary>
public static class RogueliteSceneEnricher
{
    public static void EnrichCurrentScene(Player player)
    {
        if (player == null) return;

        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName.Equals("MainMenu", System.StringComparison.OrdinalIgnoreCase)) return;

        int mapTier = DetectMapTier(sceneName);

        // 1. Luôn đảm bảo có Cửa Hàng Vật Phẩm ngay đầu mỗi map!
        Vector3 merchantPos = EnsureMerchantAtStart(player);

        // 2. Quét & xáo trộn ngẫu nhiên toàn bộ vị trí mặt đất hợp lệ trên bản đồ
        List<Vector3> randomSpots = CollectShuffledGroundSpots(player, merchantPos);

        // Số lượng vật phẩm & độ giàu có tăng dần theo Map 1 -> Map 2 -> Map 3
        int targetScrolls = mapTier == 1 ? 2 : 3;
        int targetTreasureChests = mapTier == 1 ? 2 : 3;
        int targetCursedChests = mapTier == 1 ? 1 : (mapTier == 2 ? UnityEngine.Random.Range(1, 3) : 2);
        int targetElites = mapTier == 1 ? UnityEngine.Random.Range(1, 3) : (mapTier == 2 ? UnityEngine.Random.Range(2, 4) : 3);

        var usedPositions = new List<Vector3> { player.transform.position, merchantPos };
        AddKeyPositionsToExclusion(usedPositions);

        // 3. Rải ngẫu nhiên Power Scrolls
        int existingScrolls = Object.FindObjectsByType<PowerScrollPickup>(FindObjectsSortMode.None).Length;
        for (int i = existingScrolls; i < targetScrolls; i++)
        {
            if (!TryPickSpacedSpot(randomSpots, usedPositions, 5.0f, out Vector3 spot)) break;
            SpawnPowerScroll(spot, i + 1);
            usedPositions.Add(spot);
        }

        // 4. Rải ngẫu nhiên Rương Kho Báu Vàng (Treasure Chests)
        int existingTreasures = Object.FindObjectsByType<TreasureChest>(FindObjectsSortMode.None).Length;
        for (int i = existingTreasures; i < targetTreasureChests; i++)
        {
            if (!TryPickSpacedSpot(randomSpots, usedPositions, 4.5f, out Vector3 spot)) break;
            SpawnTreasureChest(spot, mapTier, i + 1);
            usedPositions.Add(spot);
        }

        // 5. Rải ngẫu nhiên Rương Nguyền Rủa (Cursed Chests)
        int existingCursed = Object.FindObjectsByType<CursedChest>(FindObjectsSortMode.None).Length;
        for (int i = existingCursed; i < targetCursedChests; i++)
        {
            if (!TryPickSpacedSpot(randomSpots, usedPositions, 5.5f, out Vector3 spot)) break;
            SpawnCursedChest(spot, i + 1);
            usedPositions.Add(spot);
        }

        // 6. Thăng cấp ngẫu nhiên Quái Tinh Anh ★ ELITE ★
        PromoteRandomElites(player, targetElites);

        // 7. Biến quái thường Map 1 thành Quái Mini Địa Ngục (Hellspawn Cultist) cùng concept với Quái Tinh Anh
        if (mapTier == 1)
        {
            foreach (var e in Object.FindObjectsByType<PatrolEnemy1>(FindObjectsSortMode.None))
            {
                if (e == null || e.GetComponent<EliteEnemy>() != null || e.GetComponent<MiniHellspawn>() != null) continue;
                e.gameObject.AddComponent<MiniHellspawn>();
            }
        }
    }

    private static int DetectMapTier(string sceneName)
    {
        string lower = sceneName.ToLowerInvariant();
        if (lower.Contains("3")) return 3;
        if (lower.Contains("2")) return 2;
        return Mathf.Clamp(RunManager.Get().Depth, 1, 3);
    }

    private static Vector3 EnsureMerchantAtStart(Player player)
    {
        var existing = Object.FindAnyObjectByType<InRunMerchant>();
        if (existing != null) return existing.transform.position;

        // Thử các vị trí gần điểm xuất phát trên sàn phẳng an toàn (ưu tiên ngay trước mặt hoặc sau lưng Player)
        float[] offsets = { 2.8f, 3.6f, -2.6f, -3.5f, 1.9f, 4.5f };
        Vector3 chosen = player.transform.position + Vector3.right * 2.5f;

        foreach (float dx in offsets)
        {
            Vector3 probe = player.transform.position + new Vector3(dx, 0f, 0f);
            if (TryFindSafeGroundAt(probe, out Vector3 groundPos, requireWideLedge: true))
            {
                chosen = groundPos;
                break;
            }
        }

        var go = new GameObject("InRunMerchant_Start");
        go.transform.position = chosen;
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(2.2f, 2.2f);
        go.AddComponent<InRunMerchant>();
        return chosen;
    }

    private static List<Vector3> CollectShuffledGroundSpots(Player player, Vector3 merchantPos)
    {
        var candidates = new List<Vector3>();
        Vector3 pPos = player.transform.position;

        // Nguồn 1: Quét theo khung bao toàn bộ địa hình solid của bản đồ (sàn chính + các bệ nhảy trên cao!)
        float minX = pPos.x - 10f, maxX = pPos.x + 30f;
        float minY = pPos.y - 6f, maxY = pPos.y + 12f;
        bool hasBounds = false;

        var allCols = Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None);
        foreach (var c in allCols)
        {
            if (c == null || c.isTrigger || !c.enabled) continue;
            if (IsIgnoredCollider(c)) continue;

            Bounds b = c.bounds;
            if (b.size.x < 1f && b.size.y < 0.2f) continue;

            if (!hasBounds)
            {
                minX = b.min.x; maxX = b.max.x;
                minY = b.min.y; maxY = b.max.y;
                hasBounds = true;
            }
            else
            {
                minX = Mathf.Min(minX, b.min.x);
                maxX = Mathf.Max(maxX, b.max.x);
                minY = Mathf.Min(minY, b.min.y);
                maxY = Mathf.Max(maxY, b.max.y);
            }
        }

        // Nguồn 2: Thu thập thêm vị trí xung quanh các quái vật & đồng xu hiện hữu trong màn (luôn đảm bảo là khu vực đi tới được!)
        var enemies = Object.FindObjectsByType<PatrolEnemy1>(FindObjectsSortMode.None);
        foreach (var e in enemies)
        {
            if (e == null) continue;
            TryAddValidSpot(candidates, e.transform.position + Vector3.right * 1.4f, pPos, merchantPos);
            TryAddValidSpot(candidates, e.transform.position + Vector3.left * 1.4f, pPos, merchantPos);
        }

        var ranged = Object.FindObjectsByType<RangedEnemy>(FindObjectsSortMode.None);
        foreach (var r in ranged)
        {
            if (r == null) continue;
            TryAddValidSpot(candidates, r.transform.position + Vector3.right * 1.2f, pPos, merchantPos);
        }

        var coins = GameObject.FindGameObjectsWithTag("Coin");
        foreach (var coin in coins)
        {
            if (coin == null) continue;
            TryAddValidSpot(candidates, coin.transform.position, pPos, merchantPos);
        }

        // Quét lưới ngẫu nhiên theo chiều ngang across toàn bộ chiều dài map ở 3 tầng độ cao
        float spanX = Mathf.Max(10f, maxX - minX);
        int samples = Mathf.Clamp(Mathf.RoundToInt(spanX / 2.2f), 16, 70);
        float[] probeHeights = { maxY + 1.5f, Mathf.Lerp(minY, maxY, 0.55f) + 1.5f, pPos.y + 3.5f };

        for (int i = 0; i < samples; i++)
        {
            float t = (i + UnityEngine.Random.Range(0.15f, 0.85f)) / samples;
            float x = Mathf.Lerp(minX + 1.5f, maxX - 1.5f, t);

            foreach (float py in probeHeights)
            {
                TryAddValidSpot(candidates, new Vector3(x, py, 0f), pPos, merchantPos);
            }
        }

        // Xáo trộn Fisher-Yates để mỗi lượt chơi các vật phẩm nằm hoàn toàn ngẫu nhiên!
        Shuffle(candidates);
        return candidates;
    }

    private static void TryAddValidSpot(List<Vector3> list, Vector3 probe, Vector3 playerPos, Vector3 merchantPos)
    {
        if (!TryFindSafeGroundAt(probe, out Vector3 ground, requireWideLedge: true)) return;

        // Cách điểm xuất phát và cửa hàng đầu map ít nhất 4.5m để rải đều vào sâu trong màn
        if (Vector2.Distance(ground, playerPos) < 4.8f) return;
        if (Vector2.Distance(ground, merchantPos) < 4.0f) return;

        // Tránh trùng điểm quá gần trong pool
        foreach (var existing in list)
        {
            if (Vector2.Distance(existing, ground) < 1.8f) return;
        }

        list.Add(ground);
    }

    private static bool TryFindSafeGroundAt(Vector3 probe, out Vector3 groundPos, bool requireWideLedge)
    {
        groundPos = Vector3.zero;
        Vector2 rayStart = new Vector2(probe.x, probe.y + 1.8f);
        RaycastHit2D[] hits = Physics2D.RaycastAll(rayStart, Vector2.down, 11f);

        foreach (var h in hits)
        {
            if (h.collider == null || h.collider.isTrigger) continue;
            if (IsIgnoredCollider(h.collider)) continue;
            if (h.normal.y < 0.75f) continue; // Chỉ đặt trên mặt phẳng nằm ngang, không đặt lên vách đứng

            Vector2 pt = h.point;

            // Kiểm tra không gian thoáng phía trên (không kẹt trong tường/trần)
            Collider2D[] headroom = Physics2D.OverlapBoxAll(pt + new Vector2(0f, 0.85f), new Vector2(0.8f, 0.9f), 0f);
            bool blocked = false;
            foreach (var hr in headroom)
            {
                if (hr == null || hr.isTrigger) continue;
                if (IsIgnoredCollider(hr)) continue;
                if (hr.GetComponent<PlatformEffector2D>() != null) continue;
                blocked = true;
                break;
            }
            if (blocked) continue;

            // Tránh vùng rơi chết (tag checkroi)
            Collider2D[] danger = Physics2D.OverlapCircleAll(pt + Vector2.up * 0.3f, 0.8f);
            bool inDanger = false;
            foreach (var d in danger)
            {
                if (d != null && d.CompareTag("checkroi")) { inDanger = true; break; }
            }
            if (inDanger) continue;

            // Kiểm tra mặt sàn đủ rộng hai bên (±0.55m) để rương không bị treo lơ lửng ngoài mép vực
            if (requireWideLedge)
            {
                bool leftOk = HasSolidUnder(pt + new Vector2(-0.55f, 0.45f));
                bool rightOk = HasSolidUnder(pt + new Vector2(0.55f, 0.45f));
                if (!leftOk || !rightOk) continue;
            }

            groundPos = new Vector3(pt.x, pt.y, 0f);
            return true;
        }

        return false;
    }

    private static bool HasSolidUnder(Vector2 origin)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, Vector2.down, 1.2f);
        foreach (var h in hits)
        {
            if (h.collider == null || h.collider.isTrigger) continue;
            if (IsIgnoredCollider(h.collider)) continue;
            return true;
        }
        return false;
    }

    private static bool IsIgnoredCollider(Collider2D col)
    {
        if (col == null) return true;
        if (col.CompareTag("Player") || col.CompareTag("Enemy") || col.CompareTag("Coin") || col.CompareTag("Key") || col.CompareTag("checkroi") || col.CompareTag("File"))
            return true;
        if (col.GetComponentInParent<Player>() != null) return true;
        if (col.GetComponentInParent<PatrolEnemy1>() != null) return true;
        if (col.GetComponentInParent<PatrolBoss>() != null) return true;
        if (col.GetComponentInParent<RangedEnemy>() != null) return true;
        return false;
    }

    private static void AddKeyPositionsToExclusion(List<Vector3> used)
    {
        var keys = GameObject.FindGameObjectsWithTag("Key");
        foreach (var k in keys)
        {
            if (k != null) used.Add(k.transform.position);
        }
    }

    private static bool TryPickSpacedSpot(List<Vector3> pool, List<Vector3> used, float minSpacing, out Vector3 chosen)
    {
        for (int i = 0; i < pool.Count; i++)
        {
            Vector3 candidate = pool[i];
            bool tooClose = false;
            foreach (var u in used)
            {
                if (Vector2.Distance(candidate, u) < minSpacing)
                {
                    tooClose = true;
                    break;
                }
            }
            if (!tooClose)
            {
                chosen = candidate;
                pool.RemoveAt(i);
                return true;
            }
        }
        chosen = Vector3.zero;
        return false;
    }

    private static void SpawnPowerScroll(Vector3 pos, int idx)
    {
        var go = new GameObject("PowerScroll_Random_" + idx);
        go.layer = 2; // Ignore Raycast để quái luôn đi xuyên qua
        go.transform.position = pos;
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.8f, 1.8f);
        go.AddComponent<PowerScrollPickup>();
    }

    private static void SpawnTreasureChest(Vector3 pos, int mapTier, int idx)
    {
        var go = new GameObject("TreasureChest_Random_" + idx);
        go.layer = 2;
        go.transform.position = pos;
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(2.0f, 1.9f);
        var chest = go.AddComponent<TreasureChest>();
        chest.ConfigureForMapTier(mapTier);
    }

    private static void SpawnCursedChest(Vector3 pos, int idx)
    {
        var go = new GameObject("CursedChest_Random_" + idx);
        go.layer = 2;
        go.transform.position = pos;
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(2.0f, 1.9f);
        go.AddComponent<CursedChest>();
    }

    private static void PromoteRandomElites(Player player, int targetElites)
    {
        // Đếm cả Elite Map 1/3 (EliteEnemy) lẫn Tử Thần Tinh Anh Map 2 (Map2ReaperElite)
        int existing = Object.FindObjectsByType<EliteEnemy>(FindObjectsSortMode.None).Length
                     + Object.FindObjectsByType<Map2ReaperElite>(FindObjectsSortMode.None).Length;
        int needed = targetElites - existing;
        if (needed <= 0) return;

        bool isMap2 = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.ToLowerInvariant().Contains("map 2")
                   || UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.ToLowerInvariant().Contains("map2");

        var enemies = Object.FindObjectsByType<PatrolEnemy1>(FindObjectsSortMode.None);
        if (enemies == null || enemies.Length == 0) return;

        var sorted = new List<PatrolEnemy1>(enemies);
        sorted.Sort((a, b) =>
            Vector2.Distance(player.transform.position, a.transform.position)
            .CompareTo(Vector2.Distance(player.transform.position, b.transform.position)));

        // Giữ con quái gần điểm spawn nhất là quái thường, không đè lên Phù Thủy hay Tinh Anh có sẵn
        var pool = new List<PatrolEnemy1>();
        int startIdx = sorted.Count > 1 ? 1 : 0;
        for (int i = startIdx; i < sorted.Count; i++)
        {
            if (sorted[i] != null
                && sorted[i].GetComponent<EliteEnemy>() == null
                && sorted[i].GetComponent<Map2ReaperElite>() == null
                && sorted[i].GetComponent<DarkWitchEnemy>() == null)
            {
                pool.Add(sorted[i]);
            }
        }

        Shuffle(pool);
        int count = Mathf.Min(needed, pool.Count);
        for (int i = 0; i < count; i++)
        {
            if (isMap2) pool[i].gameObject.AddComponent<Map2ReaperElite>();
            else pool[i].gameObject.AddComponent<EliteEnemy>();
        }
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int j = UnityEngine.Random.Range(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
