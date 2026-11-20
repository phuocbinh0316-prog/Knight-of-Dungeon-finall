#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Knight.Editor
{
    public static class ChamberedMap1Builder
    {
        private const string ScenePath = "Assets/Scenes/map 1.unity";
        private const string TileDir = "Assets/Tile/DeadCellsDungeon";

        [MenuItem("Knight of Dungeon/Build Chambered Map 1")]
        public static void Build()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError("[ChamberedMap1Builder] Could not open map 1.");
                return;
            }

            var ground = GameObject.Find("Grid/Ground");
            if (ground == null) ground = GameObject.Find("Ground");
            var groundMap = ground != null ? ground.GetComponent<Tilemap>() : null;
            if (groundMap == null)
            {
                Debug.LogError("[ChamberedMap1Builder] Ground Tilemap is missing.");
                return;
            }

            var surface = AssetDatabase.LoadAssetAtPath<Tile>($"{TileDir}/dc_surface.asset");
            var fill = AssetDatabase.LoadAssetAtPath<Tile>($"{TileDir}/dc_fill.asset");
            var wallLeft = AssetDatabase.LoadAssetAtPath<Tile>($"{TileDir}/dc_wall_left.asset");
            var wallRight = AssetDatabase.LoadAssetAtPath<Tile>($"{TileDir}/dc_wall_right.asset");
            var ceiling = AssetDatabase.LoadAssetAtPath<Tile>($"{TileDir}/dc_ceiling.asset");
            var platform = AssetDatabase.LoadAssetAtPath<Tile>($"{TileDir}/dc_platform.asset");
            var cracked = AssetDatabase.LoadAssetAtPath<Tile>($"{TileDir}/dc_cracked.asset");
            var mossy = AssetDatabase.LoadAssetAtPath<Tile>($"{TileDir}/dc_mossy.asset");
            if (surface == null || fill == null || platform == null)
            {
                Debug.LogError("[ChamberedMap1Builder] Required Dead Cells tiles are missing.");
                return;
            }

            BuildGround(groundMap, surface, fill, wallLeft, wallRight, ceiling, platform, cracked, mossy);
            BuildBackWalls(ground, fill);
            ArrangeBackdropAndProps();
            PlacePlayerAndEncounters(scene);
            ConfigureCamera();

            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            bool saved = EditorSceneManager.SaveScene(scene);
            EditorSceneManager.SaveOpenScenes();
            Debug.Log($"[ChamberedMap1Builder] Saved four connected chambers and encounter layout. Save result: {saved}");
        }

        private static void BuildGround(Tilemap map, Tile surface, Tile fill, Tile wallLeft, Tile wallRight,
            Tile ceiling, Tile platform, Tile cracked, Tile mossy)
        {
            var occupied = new HashSet<Vector3Int>();
            void Block(int x0, int x1, int y0, int y1)
            {
                for (int x = x0; x <= x1; x++)
                    for (int y = y0; y <= y1; y++)
                        occupied.Add(new Vector3Int(x, y, 0));
            }
            void Ledge(int x0, int x1, int y)
            {
                for (int x = x0; x <= x1; x++) occupied.Add(new Vector3Int(x, y, 0));
            }

            // Four rooms share a continuous lower route so the critical path stays readable.
            Block(-16, 12, -3, -1);   // entrance crypt
            Block(13, 43, -3, -1);    // cathedral nave
            Block(44, 77, -3, -1);    // ossuary
            Block(78, 112, -3, -1);   // final bastion

            // Optional upper routes use broad, reachable ledges and short stair steps.
            Block(15, 17, 1, 1);
            Block(16, 18, 2, 2);
            Ledge(18, 27, 3);
            Block(27, 29, 2, 2);
            Block(29, 31, 1, 1);

            Block(47, 49, 1, 1);
            Block(48, 50, 2, 2);
            Ledge(50, 59, 3);
            Block(59, 61, 4, 4);
            Ledge(61, 65, 5); // optional treasure shelf

            // Raised boss stage with two steps up from the continuous main floor.
            Block(86, 88, 1, 1);
            Block(88, 90, 2, 2);
            Block(90, 106, -3, 1);
            Ledge(92, 104, 2);

            // Short side walls frame the room transitions without sealing the route.
            Block(10, 10, 2, 7);
            Block(11, 11, 5, 7);
            Block(42, 42, 2, 7);
            Block(43, 43, 5, 7);
            Block(76, 76, 2, 7);
            Block(77, 77, 5, 7);

            var positions = new List<Vector3Int>(occupied);
            var tiles = new List<TileBase>(positions.Count);
            foreach (var p in positions)
            {
                bool above = occupied.Contains(p + Vector3Int.up);
                bool below = occupied.Contains(p + Vector3Int.down);
                bool left = occupied.Contains(p + Vector3Int.left);
                bool right = occupied.Contains(p + Vector3Int.right);
                Tile chosen;
                if (!above && !below) chosen = platform;
                else if (!above)
                {
                    int hash = Mathf.Abs((p.x * 73856093) ^ (p.y * 19349663));
                    if (!left && wallLeft != null) chosen = wallLeft;
                    else if (!right && wallRight != null) chosen = wallRight;
                    else if (mossy != null && hash % 7 == 0) chosen = mossy;
                    else if (cracked != null && hash % 11 == 0) chosen = cracked;
                    else chosen = surface;
                }
                else if (!below) chosen = ceiling != null ? ceiling : fill;
                else
                {
                    int hash = Mathf.Abs((p.x * 37) ^ (p.y * 101));
                    if (!left && wallLeft != null) chosen = wallLeft;
                    else if (!right && wallRight != null) chosen = wallRight;
                    else if (cracked != null && hash % 9 == 0) chosen = cracked;
                    else chosen = fill;
                }
                tiles.Add(chosen);
            }

            map.ClearAllTiles();
            map.SetTiles(positions.ToArray(), tiles.ToArray());
            map.CompressBounds();
            map.RefreshAllTiles();
            EditorUtility.SetDirty(map);
        }

        private static void BuildBackWalls(GameObject ground, Tile fill)
        {
            var grid = ground.transform.parent != null ? ground.transform.parent.gameObject : null;
            if (grid == null) return;
            var wallObject = GameObject.Find("Dungeon_BackWall");
            if (wallObject == null)
            {
                wallObject = new GameObject("Dungeon_BackWall");
                wallObject.transform.SetParent(grid.transform, false);
                wallObject.AddComponent<Tilemap>();
                wallObject.AddComponent<TilemapRenderer>();
            }
            var wallMap = wallObject.GetComponent<Tilemap>();
            wallMap.ClearAllTiles();
            var renderer = wallObject.GetComponent<TilemapRenderer>();
            renderer.sortingLayerName = "Background";
            renderer.sortingOrder = -25;
            var material = ground.GetComponent<TilemapRenderer>();
            if (material != null) renderer.sharedMaterial = material.sharedMaterial;

            // Brick panels read as rooms; openings align with the lower critical path.
            FillWallPanel(wallMap, fill, -16, 9, 2, 8);
            FillWallPanel(wallMap, fill, 13, 41, 2, 8);
            FillWallPanel(wallMap, fill, 44, 75, 2, 8);
            FillWallPanel(wallMap, fill, 78, 112, 2, 8);
            EditorUtility.SetDirty(wallMap);
        }

        private static void FillWallPanel(Tilemap map, Tile tile, int x0, int x1, int y0, int y1)
        {
            for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++)
                {
                    bool windowBay = (x >= 22 && x <= 24) || (x >= 54 && x <= 56) || (x >= 91 && x <= 93);
                    if (!windowBay) map.SetTile(new Vector3Int(x, y, 0), tile);
                }
        }

        private static void ArrangeBackdropAndProps()
        {
            // Until a new generated panorama is available, use one scaled copy of the current art.
            var bg0 = GameObject.Find("DC_CathedralBackdrop_0");
            var bg1 = GameObject.Find("DC_CathedralBackdrop_1");
            if (bg0 != null)
            {
                bg0.transform.position = new Vector3(48f, 4f, 25f);
                bg0.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
            }
            if (bg1 != null) bg1.SetActive(false);

            PlaceSeries("DC_BgCathedral_", new[] { -1f, 32f, 64f, 98f }, 3f, 18f);
            PlaceSeries("DC_BgFortress_", new[] { 12f, 55f, 91f }, 1.5f, 15f);
            PlaceSeries("DC_GothicArch_", new[] { -11f, 2f, 17f, 32f, 47f, 62f, 77f, 92f, 107f }, 0f, 10f);
            PlaceSeries("DC_StainedWindow_", new[] { -3f, 18f, 37f, 55f, 73f, 93f }, 3f, 11f);
            PlaceSeries("DC_Column_", new[] { -14f, 10f, 28f, 43f, 60f, 77f, 91f, 108f }, 0f, 2f);
            PlaceSeries("DC_WallTorch_", new[] { -8f, 8f, 20f, 37f, 49f, 70f, 83f, 104f, 30f, 62f }, 2f, 0f);
            PlaceSeries("DC_Banner_", new[] { -4f, 30f, 64f, 94f, 17f, 77f }, 2f, 1f);
            PlaceSeries("DC_HangingCage_", new[] { 16f, 46f, 71f }, 6f, 1f);
            PlaceSeries("DC_Crates_", new[] { -7f, 35f, 71f, 84f, 104f, 54f }, 0f, 0f);
            PlaceSeries("DC_Chest_", new[] { 34f, 63f }, 0f, 0f);
            Place("DC_Chest_1", new Vector3(63f, 5f, 0f));
            PlaceSeries("DC_Skeleton_", new[] { 26f, 69f, 88f, 102f }, 0f, 0f);
            PlaceSeries("DC_MagicCrystal_", new[] { 59f, 96f, 101f, 104f }, 1f, 0f);
            Place("DC_GothicStatue", new Vector3(62f, 0f, 0.5f));
            Place("DC_AncientRunes", new Vector3(98f, 3f, 0f));
            Place("DC_EntranceDoor", new Vector3(-14f, 0f, 0.5f));
            Place("DC_BossGate", new Vector3(106f, 2f, 0.5f));

            var oldActor = GameObject.Find("enemy");
            if (oldActor != null) oldActor.SetActive(false);
            var oldPlatform = GameObject.Find("platform");
            if (oldPlatform != null) oldPlatform.SetActive(false);

            var coin = GameObject.Find("coin");
            if (coin != null) coin.transform.position = new Vector3(34f, 0f, 0f);
            var file = GameObject.Find("Filee");
            if (file != null) file.transform.position = new Vector3(63f, 5f, 0f);
        }

        private static void PlaceSeries(string prefix, float[] xs, float y, float z)
        {
            int count = prefix == "DC_WallTorch_" ? 14 :
                prefix == "DC_Banner_" ? 6 :
                prefix == "DC_HangingCage_" ? 5 :
                prefix == "DC_Crates_" ? 6 :
                prefix == "DC_Chest_" ? 3 :
                prefix == "DC_Skeleton_" ? 4 :
                prefix == "DC_MagicCrystal_" ? 4 :
                prefix == "DC_BgCathedral_" ? 4 :
                prefix == "DC_BgFortress_" ? 3 :
                prefix == "DC_GothicArch_" ? 9 :
                prefix == "DC_StainedWindow_" ? 9 :
                prefix == "DC_Column_" ? 10 : xs.Length;
            for (int i = 0; i < count; i++)
            {
                var obj = GameObject.Find(prefix + i);
                if (obj == null) continue;
                bool include = i < xs.Length;
                obj.SetActive(include);
                if (include) obj.transform.position = new Vector3(xs[i], y, z);
            }
        }

        private static void Place(string name, Vector3 position)
        {
            var obj = GameObject.Find(name);
            if (obj != null) obj.transform.position = position;
        }

        private static void PlacePlayerAndEncounters(Scene scene)
        {
            var player = GameObject.Find("Player");
            if (player == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
                if (prefab != null) player = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            }
            if (player == null)
            {
                Debug.LogError("[ChamberedMap1Builder] Player.prefab could not be instantiated.");
                return;
            }
            player.name = "Player";
            player.transform.position = new Vector3(-13f, 0.05f, 0f);

            RemoveEncounter("Map1_Encounter_1");
            RemoveEncounter("Map1_Encounter_2");
            RemoveEncounter("Map1_Encounter_3");
            RemoveEncounter("Map1_Encounter_Boss");
            SpawnEnemy("Assets/Prefabs/Scarecrow 1.prefab", "Map1_Encounter_1", new Vector3(4f, 0f, 0f), player.transform);
            SpawnEnemy("Assets/Prefabs/Roguelite/RangedEnemy.prefab", "Map1_Encounter_2", new Vector3(32f, 0.33f, 0f), player.transform);
            SpawnEnemy("Assets/Prefabs/Scarecrow 1.prefab", "Map1_Encounter_3", new Vector3(66f, 0f, 0f), player.transform);
            SpawnEnemy("Assets/Prefabs/Boos.prefab", "Map1_Encounter_Boss", new Vector3(97f, 2f, 0f), player.transform);
        }

        private static void RemoveEncounter(string name)
        {
            var existing = GameObject.Find(name);
            if (existing != null) Object.DestroyImmediate(existing);
        }

        private static void SpawnEnemy(string path, string name, Vector3 position, Transform player)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError($"[ChamberedMap1Builder] Missing enemy prefab: {path}");
                return;
            }
            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null) return;
            instance.name = name;
            instance.transform.position = position;
            foreach (var behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true))
            {
                var serialized = new SerializedObject(behaviour);
                var playerField = serialized.FindProperty("player");
                if (playerField != null && playerField.propertyType == SerializedPropertyType.ObjectReference)
                {
                    playerField.objectReferenceValue = player;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        private static void ConfigureCamera()
        {
            var player = GameObject.Find("Player");
            var vcam = GameObject.Find("CinemachineCamera");
            if (player == null || vcam == null) return;
            foreach (var component in vcam.GetComponents<Component>())
            {
                if (component == null || component.GetType().Name != "CinemachineCamera") continue;
                var serialized = new SerializedObject(component);
                var target = serialized.FindProperty("Target.TrackingTarget");
                if (target != null)
                {
                    target.objectReferenceValue = player.transform;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(component);
                }
            }
        }
    }
}
#endif
