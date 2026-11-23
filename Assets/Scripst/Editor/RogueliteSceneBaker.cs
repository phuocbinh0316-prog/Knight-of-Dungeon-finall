using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Knight.Editor
{
    /// <summary>
    /// Công cụ nướng (Bake) toàn bộ yếu tố Roguelite trực tiếp vào Scene (.unity):
    /// - Cửa Hàng Thương Nhân (InRunMerchant) ngay đầu mỗi map cạnh điểm xuất phát Player.
    /// - Rương Kho Báu Vàng (Treasure Chests) trên các bệ nhảy / sàn phẳng.
    /// - Rương Nguyền Rủa Tím (Cursed Chests) trên cao.
    /// - Cuộn Sức Mạnh (Power Scrolls) lơ lửng trên bệ.
    /// - Quái Tinh Anh Địa Ngục (Infernal Elite Demon) to lớn ngầu lòi với Horned Helmet & Lava Greatsword.
    /// - Quái Mini Giáo Binh Địa Ngục (Hellspawn Mini Cultist) với ngọn giáo phát sáng.
    ///
    /// Tất cả xuất hiện 100% TRỰC TIẾP TRONG SCENE VIEW NGAY Ở EDIT MODE TRƯỚC KHI BẤM PLAY!
    /// </summary>
    public static class RogueliteSceneBaker
    {
        private const string RootName = "[Roguelite_Objects]";

        [MenuItem("Roguelite/Bake Roguelite Elements Into Scenes", false, 10)]
        public static void BakeAllScenes()
        {
            string[] scenePaths = new string[]
            {
                "Assets/Scenes/map 1.unity",
                "Assets/Scenes/map 2.unity",
                "Assets/Scenes/map 3.unity"
            };

            foreach (var path in scenePaths)
            {
                BakeSceneAtPath(path);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("<color=#10B981><b>[RogueliteSceneBaker]</b> Đã nướng hoàn tất toàn bộ yếu tố Roguelite vào map 1, map 2, map 3!</color>");
        }

        [MenuItem("Roguelite/Bake Current Scene Only", false, 11)]
        public static void BakeCurrentScene()
        {
            var active = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(active.path))
            {
                Debug.LogWarning("[RogueliteSceneBaker] Scene hiện tại chưa được lưu.");
                return;
            }
            BakeSceneAtPath(active.path);
            AssetDatabase.SaveAssets();
            Debug.Log($"<color=#10B981><b>[RogueliteSceneBaker]</b> Đã nướng thành công vào Scene: {active.name}</color>");
        }

        public static void BakeSceneAtPath(string path)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[RogueliteSceneBaker] Không thể mở Scene: {path}");
                return;
            }

            int tier = DetectTier(scene.name);

            // Tải Sprites
            var sMerchant = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Roguelite/merchant.png");
            var sTreasure = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Roguelite/treasure_chest.png");
            var sCursed = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Roguelite/cursed_chest.png");
            var sScroll = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Roguelite/power_scroll.png");
            var sElite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/HellElite/hell_idle.png");
            var sMini = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/HellMini/mini_idle.png");

            // Tải Prefabs nếu có
            var pMerchant = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Roguelite/InRunMerchant.prefab");
            var pTreasure = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Roguelite/TreasureChest.prefab");
            var pCursed = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Roguelite/CursedChest.prefab");
            var pScroll = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Roguelite/PowerScroll.prefab");

            // 1. Quản lý GameObject Root [Roguelite_Objects]
            GameObject root = GameObject.Find(RootName);
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
            root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Bake Roguelite Objects");

            // 2. Tìm Player
            var player = Object.FindAnyObjectByType<Player>();
            Vector3 pPos = player != null ? player.transform.position : GetFallbackPlayerPos(tier);

            // 3. Đặt Thương Nhân (InRunMerchant) ở đầu map cạnh Player
            Vector3 merchantPos = pPos + new Vector3(2.8f, 0f, 0f);
            var mObj = pMerchant != null ? (GameObject)PrefabUtility.InstantiatePrefab(pMerchant) : new GameObject("InRunMerchant_Shop");
            mObj.name = "InRunMerchant_Shop";
            mObj.transform.SetParent(root.transform);
            mObj.transform.position = merchantPos;
            EnsureSpriteAndCollider(mObj, sMerchant, typeof(InRunMerchant), new Vector2(2.2f, 2.2f), new Vector2(0f, 0.9f));

            // 4. Lấy danh sách toạ độ đặt vật phẩm theo từng Map
            var (scrollPositions, treasurePositions, cursedPositions) = GetPlatformPositions(tier, pPos);

            // Đặt Power Scrolls
            for (int i = 0; i < scrollPositions.Length; i++)
            {
                var sObj = pScroll != null ? (GameObject)PrefabUtility.InstantiatePrefab(pScroll) : new GameObject($"PowerScroll_{i + 1}");
                sObj.name = $"PowerScroll_{i + 1}";
                sObj.transform.SetParent(root.transform);
                sObj.transform.position = scrollPositions[i];
                EnsureSpriteAndCollider(sObj, sScroll, typeof(PowerScrollPickup), new Vector2(1.4f, 1.4f), Vector2.zero);
            }

            // Đặt Treasure Chests
            for (int i = 0; i < treasurePositions.Length; i++)
            {
                var tObj = pTreasure != null ? (GameObject)PrefabUtility.InstantiatePrefab(pTreasure) : new GameObject($"TreasureChest_{i + 1}");
                tObj.name = $"TreasureChest_{i + 1}";
                tObj.transform.SetParent(root.transform);
                tObj.transform.position = treasurePositions[i];
                EnsureSpriteAndCollider(tObj, sTreasure, typeof(TreasureChest), new Vector2(1.6f, 1.4f), new Vector2(0f, 0.5f));
            }

            // Đặt Cursed Chests
            for (int i = 0; i < cursedPositions.Length; i++)
            {
                var cObj = pCursed != null ? (GameObject)PrefabUtility.InstantiatePrefab(pCursed) : new GameObject($"CursedChest_{i + 1}");
                cObj.name = $"CursedChest_{i + 1}";
                cObj.transform.SetParent(root.transform);
                cObj.transform.position = cursedPositions[i];
                EnsureSpriteAndCollider(cObj, sCursed, typeof(CursedChest), new Vector2(1.6f, 1.4f), new Vector2(0f, 0.5f));
            }

            // 5. Cập nhật Tạo hình Quái trong Scene View (Quái Tinh Anh & Quái Mini Địa Ngục)
            BakeEnemies(tier, sElite, sMini);

            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[RogueliteSceneBaker] Đã lưu Scene {scene.name} với đầy đủ Thương nhân, Rương, Cuộn sách và Quái vật.");
        }

        private static void BakeEnemies(int tier, Sprite sElite, Sprite sMini)
        {
            var enemies = new List<PatrolEnemy1>(Object.FindObjectsByType<PatrolEnemy1>(FindObjectsSortMode.None));
            if (enemies.Count == 0) return;

            enemies.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));

            // Số lượng Elite tương ứng với độ khó của Map
            int eliteTargetCount = tier == 1 ? 1 : (tier == 2 ? 2 : 3);
            var eliteIndices = new HashSet<int>();

            if (tier == 1)
            {
                // Map 1: Chọn quái ở giữa bản đồ (vị trí đảo đá hoặc cầu gỗ trung tâm)
                int midIndex = Mathf.Clamp(enemies.Count / 2, 0, enemies.Count - 1);
                eliteIndices.Add(midIndex);
            }
            else if (tier == 2)
            {
                if (enemies.Count >= 2) { eliteIndices.Add(enemies.Count / 3); eliteIndices.Add(enemies.Count * 2 / 3); }
                else eliteIndices.Add(0);
            }
            else
            {
                for (int i = 0; i < eliteTargetCount && i < enemies.Count; i++)
                {
                    eliteIndices.Add((i * enemies.Count) / eliteTargetCount);
                }
            }

            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if (e == null) continue;

                bool isElite = eliteIndices.Contains(i);
                if (isElite)
                {
                    // Quái Tinh Anh Địa Ngục (Infernal Elite Demon)
                    e.gameObject.name = $"Infernal_Elite_Demon_{i + 1}";
                    if (e.GetComponent<EliteEnemy>() == null) e.gameObject.AddComponent<EliteEnemy>();

                    // Ẩn SpriteRenderer cũ của quái gốc để Animator không đè hình con quạ
                    var hostSr = e.GetComponent<SpriteRenderer>();
                    if (hostSr != null) hostSr.enabled = false;

                    // Gắn hình ảnh Quái Tinh Anh Địa Ngục
                    Transform visualT = e.transform.Find("HellDemonVisual");
                    GameObject visualObj = visualT != null ? visualT.gameObject : new GameObject("HellDemonVisual");
                    visualObj.transform.SetParent(e.transform, false);
                    visualObj.transform.localPosition = Vector3.zero;
                    visualObj.transform.localScale = new Vector3(1.16f, 1.16f, 1f);

                    var vSr = visualObj.GetComponent<SpriteRenderer>() ?? visualObj.AddComponent<SpriteRenderer>();
                    if (sElite != null) vSr.sprite = sElite;
                    vSr.sortingLayerName = "Enemy";
                    vSr.sortingOrder = 6;
                    vSr.enabled = true;
                }
                else
                {
                    // Quái Mini Giáo Binh Địa Ngục (Hellspawn Cultist)
                    e.gameObject.name = $"Hellspawn_Mini_Cultist_{i + 1}";
                    if (e.GetComponent<MiniHellspawn>() == null) e.gameObject.AddComponent<MiniHellspawn>();

                    var hostSr = e.GetComponent<SpriteRenderer>();
                    if (hostSr != null) hostSr.enabled = false;

                    // Gắn hình ảnh Quái Mini Địa Ngục
                    Transform visualT = e.transform.Find("HellMiniVisual");
                    GameObject visualObj = visualT != null ? visualT.gameObject : new GameObject("HellMiniVisual");
                    visualObj.transform.SetParent(e.transform, false);
                    visualObj.transform.localPosition = Vector3.zero;
                    visualObj.transform.localScale = Vector3.one;

                    var vSr = visualObj.GetComponent<SpriteRenderer>() ?? visualObj.AddComponent<SpriteRenderer>();
                    if (sMini != null) vSr.sprite = sMini;
                    vSr.sortingLayerName = "Enemy";
                    vSr.sortingOrder = 5;
                    vSr.enabled = true;
                }

                EditorUtility.SetDirty(e.gameObject);
            }
        }

        private static void EnsureSpriteAndCollider(GameObject go, Sprite sprite, System.Type compType, Vector2 colSize, Vector2 colOffset)
        {
            var sr = go.GetComponent<SpriteRenderer>() ?? go.AddComponent<SpriteRenderer>();
            if (sprite != null) sr.sprite = sprite;
            sr.sortingOrder = 5;

            var col = go.GetComponent<BoxCollider2D>() ?? go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = colSize;
            col.offset = colOffset;

            if (compType != null && go.GetComponent(compType) == null)
            {
                go.AddComponent(compType);
            }
            EditorUtility.SetDirty(go);
        }

        private static (Vector3[] scrolls, Vector3[] treasures, Vector3[] cursed) GetPlatformPositions(int tier, Vector3 playerPos)
        {
            if (tier == 1)
            {
                return (
                    new Vector3[]
                    {
                        new Vector3(-2.8f, 0.4f, 0f),
                        new Vector3(6.5f, 2.2f, 0f)
                    },
                    new Vector3[]
                    {
                        new Vector3(-0.5f, -1.63f, 0f),
                        new Vector3(8.2f, -1.63f, 0f)
                    },
                    new Vector3[]
                    {
                        new Vector3(0.5f, 0.5f, 0f)
                    }
                );
            }
            else if (tier == 2)
            {
                return (
                    new Vector3[]
                    {
                        playerPos + new Vector3(8f, 1.5f, 0f),
                        playerPos + new Vector3(22f, 3.0f, 0f),
                        playerPos + new Vector3(38f, 1.2f, 0f)
                    },
                    new Vector3[]
                    {
                        playerPos + new Vector3(14f, 0f, 0f),
                        playerPos + new Vector3(28f, 0.5f, 0f),
                        playerPos + new Vector3(45f, 0f, 0f)
                    },
                    new Vector3[]
                    {
                        playerPos + new Vector3(18f, 3.5f, 0f),
                        playerPos + new Vector3(34f, 4.0f, 0f)
                    }
                );
            }
            else
            {
                return (
                    new Vector3[]
                    {
                        playerPos + new Vector3(6f, 1.5f, 0f),
                        playerPos + new Vector3(18f, 2.8f, 0f),
                        playerPos + new Vector3(32f, 2.0f, 0f)
                    },
                    new Vector3[]
                    {
                        playerPos + new Vector3(11f, 0f, 0f),
                        playerPos + new Vector3(24f, 0.5f, 0f),
                        playerPos + new Vector3(40f, 0f, 0f)
                    },
                    new Vector3[]
                    {
                        playerPos + new Vector3(15f, 3.2f, 0f),
                        playerPos + new Vector3(29f, 3.8f, 0f)
                    }
                );
            }
        }

        private static Vector3 GetFallbackPlayerPos(int tier)
        {
            if (tier == 1) return new Vector3(-9.92f, -1.56f, 0f);
            if (tier == 2) return new Vector3(73.37f, 0.81f, 0f);
            return new Vector3(29.5f, -5.9f, 0f);
        }

        private static int DetectTier(string sceneName)
        {
            string s = sceneName.ToLowerInvariant();
            if (s.Contains("3")) return 3;
            if (s.Contains("2")) return 2;
            return 1;
        }
    }
}
