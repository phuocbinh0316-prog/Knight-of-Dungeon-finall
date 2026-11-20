#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Knight.Editor
{
    /// <summary>Rebuilds map 1 as a playable scene from the separated DeadCells_Unity_Tileset sprites.</summary>
    public static class SeparatedTilesetMap1Builder
    {
        private const string ScenePath = "Assets/Scenes/map 1.unity";
        private const string ArtRoot = "Assets/Art/DeadCells_Unity_Tileset";
        private const string TileRoot = ArtRoot + "/Generated_TileAssets";
        private const string PrefabRoot = ArtRoot + "/Generated_Prefabs";
        private static readonly HashSet<string> RuntimeRoots = new HashSet<string>
        {
            "Main Camera", "CinemachineCamera", "GameManager", "AudioManager",
            "EventSystem", "Canvas", "Global Light 2D", "CheckRoi"
        };

        [MenuItem("Knight of Dungeon/Build Map 1 From Separated Dead Cells Tileset")]
        public static void Build()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError("[SeparatedTilesetMap1Builder] Could not open map 1.");
                return;
            }

            ConfigureSpriteImporters();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            EnsureFolder(TileRoot);
            EnsureFolder(PrefabRoot);
            var tiles = CreateTerrainTiles();
            if (tiles.Count == 0)
            {
                Debug.LogError("[SeparatedTilesetMap1Builder] No terrain sprites were imported.");
                return;
            }

            var oldLevel = FindRoot("Level_01");
            if (oldLevel != null) UnityEngine.Object.DestroyImmediate(oldLevel);
            var legacy = FindRoot("Legacy_Map1_Archive");
            if (legacy == null) legacy = new GameObject("Legacy_Map1_Archive");
            legacy.SetActive(true);
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root == legacy || root.name == "Level_01" || RuntimeRoots.Contains(root.name)) continue;
                root.transform.SetParent(legacy.transform, true);
            }
            legacy.SetActive(false);

            var level = new GameObject("Level_01");
            var environment = Child(level.transform, "Environment");
            var background = Child(environment, "Background");
            var bgSky = Child(background, "Background_Sky");
            var bgMountains = Child(background, "Background_Mountains");
            var bgCastle = Child(background, "Background_Castle");
            var bgFog = Child(background, "Background_Fog");
            var midground = Child(environment, "Midground");
            var ruins = Child(midground, "Midground_Ruins");
            var architecture = Child(midground, "Midground_Architecture");
            var midDecor = Child(midground, "Midground_Decoration");
            var gridRoot = Child(Child(environment, "Tilemaps"), "Grid").gameObject;
            var grid = gridRoot.AddComponent<Grid>();
            grid.cellSize = Vector3.one;
            var ground = CreateTilemap(gridRoot.transform, "TM_Ground", "Ground", 0, 6, true, false);
            var walls = CreateTilemap(gridRoot.transform, "TM_SolidWalls", "Ground", 1, 6, true, false);
            var platforms = CreateTilemap(gridRoot.transform, "TM_OneWayPlatforms", "Ground", 2, 6, true, true);
            var decoration = CreateTilemap(gridRoot.transform, "TM_Decoration", "Fround", -1, 0, false, false);
            var foreground = CreateTilemap(gridRoot.transform, "TM_Foreground", "Fround", 4, 0, false, false);

            BuildGeometry(ground, walls, platforms, decoration, tiles);
            BuildBackdrops(bgSky, bgMountains, bgCastle, bgFog);
            BuildArchitecture(ruins, architecture, midDecor);
            var propRoot = Child(environment, "Props");
            var torches = Child(propRoot, "Torches");
            var lanterns = Child(propRoot, "Lanterns");
            var barrels = Child(propRoot, "Barrels");
            var crates = Child(propRoot, "Crates");
            var chests = Child(propRoot, "Chests");
            var chains = Child(propRoot, "Chains");
            var banners = Child(propRoot, "Banners");
            BuildProps(torches, lanterns, barrels, crates, chests, chains, banners, scene);

            var lightRoot = Child(environment, "Lighting");
            var globalLight = FindRoot("Global Light 2D");
            if (globalLight != null) globalLight.transform.SetParent(lightRoot, true);

            var gameplay = Child(level.transform, "Gameplay");
            var spawn = Child(gameplay, "PlayerSpawn");
            spawn.position = new Vector3(-29f, 0.05f, 0f);
            var enemySpawns = Child(gameplay, "EnemySpawns");
            var checkpoints = Child(gameplay, "Checkpoints");
            var hazards = Child(gameplay, "Hazards");
            var collectibles = Child(gameplay, "Collectibles");
            var exitGroup = Child(gameplay, "LevelExit");
            BuildGameplay(scene, spawn, enemySpawns, hazards, collectibles, exitGroup);

            var cameraGroup = Child(level.transform, "Camera");
            ConfigureCamera(scene, cameraGroup, ground);

            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            bool saved = EditorSceneManager.SaveScene(scene);
            EditorSceneManager.SaveOpenScenes();
            const string levelScenePath = "Assets/Scenes/Level_01.unity";
            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(levelScenePath))
                AssetDatabase.CopyAsset(ScenePath, levelScenePath);
            Debug.Log($"[SeparatedTilesetMap1Builder] Built playable six-area map 1 from separated sprites. Save result: {saved}");
        }

        private static void ConfigureSpriteImporters()
        {
            string absoluteRoot = Path.GetFullPath(ArtRoot);
            if (!Directory.Exists(absoluteRoot))
            {
                Debug.LogError("[SeparatedTilesetMap1Builder] Tileset import folder is missing: " + ArtRoot);
                return;
            }
            foreach (string absolute in Directory.GetFiles(absoluteRoot, "*.png", SearchOption.AllDirectories))
            {
                string path = absolute.Replace('\\', '/');
                int assetsIndex = path.IndexOf("Assets/", StringComparison.Ordinal);
                if (assetsIndex >= 0) path = path.Substring(assetsIndex);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                bool changed = false;
                if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; changed = true; }
                if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; changed = true; }
                if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; changed = true; }
                if (importer.filterMode != FilterMode.Point) { importer.filterMode = FilterMode.Point; changed = true; }
                if (importer.textureCompression != TextureImporterCompression.Uncompressed) { importer.textureCompression = TextureImporterCompression.Uncompressed; changed = true; }
                if (importer.mipmapEnabled) { importer.mipmapEnabled = false; changed = true; }
                if (Mathf.Abs(importer.spritePixelsPerUnit - 88f) > 0.01f) { importer.spritePixelsPerUnit = 88f; changed = true; }
                if (changed) importer.SaveAndReimport();
            }
        }

        private static Dictionary<int, Tile> CreateTerrainTiles()
        {
            var result = new Dictionary<int, Tile>();
            int[] oneWay = { 14, 15, 16, 34, 35, 36 };
            int[] solid = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 21, 22, 23, 24, 25, 26, 27, 28, 29 };
            for (int id = 1; id <= 42; id++)
            {
                string name = $"terrain_{id:000}";
                string spritePath = $"{ArtRoot}/Terrain_Tiles/{name}.png";
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (sprite == null) continue;
                string tilePath = $"{TileRoot}/{name}_Tile.asset";
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
                if (tile == null)
                {
                    tile = ScriptableObject.CreateInstance<Tile>();
                    tile.name = name + "_Tile";
                    AssetDatabase.CreateAsset(tile, tilePath);
                }
                tile.sprite = sprite;
                tile.colliderType = Array.IndexOf(solid, id) >= 0 || Array.IndexOf(oneWay, id) >= 0
                    ? Tile.ColliderType.Grid : Tile.ColliderType.None;
                tile.transform = Array.IndexOf(oneWay, id) >= 0
                    ? Matrix4x4.Translate(new Vector3(0f, 0.25f, 0f)) : Matrix4x4.identity;
                EditorUtility.SetDirty(tile);
                result[id] = tile;
            }
            AssetDatabase.SaveAssets();
            return result;
        }

        private static void BuildGeometry(Tilemap ground, Tilemap walls, Tilemap platforms,
            Tilemap decoration, Dictionary<int, Tile> tiles)
        {
            void Block(Tilemap map, int x0, int x1, int y0, int y1, int room)
            {
                for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++)
                {
                    int hash = Mathf.Abs((x * 73856093) ^ (y * 19349663) ^ (room * 83492791));
                    int tileId;
                    if (y == y1)
                    {
                        int[] surface = room switch
                        {
                            0 => new[] { 2, 3, 4, 21, 22 },
                            1 => new[] { 1, 2, 7, 23, 27 },
                            2 => new[] { 2, 3, 8, 9, 22 },
                            3 => new[] { 1, 7, 21, 23, 28 },
                            _ => new[] { 2, 3, 4, 8, 9, 22 }
                        };
                        tileId = surface[hash % surface.Length];
                    }
                    else
                    {
                        int[] fill = room switch
                        {
                            0 => new[] { 2, 3, 4, 5, 21, 22 },
                            1 => new[] { 1, 2, 3, 7, 23, 27, 28 },
                            2 => new[] { 2, 3, 4, 8, 9, 22 },
                            3 => new[] { 1, 2, 7, 21, 23, 27, 28 },
                            _ => new[] { 2, 3, 4, 8, 9, 22, 27 }
                        };
                        tileId = fill[hash % fill.Length];
                    }
                    if (tiles.TryGetValue(tileId, out var tile)) map.SetTile(new Vector3Int(x, y, 0), tile);
                }
            }

            // Continuous main route with two short, jumpable breaks and distinct room stonework.
            Block(ground, -33, -18, -4, -1, 0); // entrance
            Block(ground, -17, -6, -4, -1, 1);  // exploration west
            Block(ground, -3, 12, -4, -1, 1);   // exploration east
            Block(ground, 13, 20, -4, -1, 2);   // combat approach
            Block(ground, 23, 37, -4, -1, 2);   // combat arena
            Block(ground, 38, 64, -4, -1, 3);   // vertical challenge
            Block(ground, 65, 92, -4, -1, 4);   // final run

            // Raised boss platform and short, double-jump reachable approach steps.
            Block(walls, 71, 72, 0, 0, 4);
            Block(walls, 73, 74, 0, 1, 4);
            Block(walls, 75, 85, 0, 1, 4);
            Block(walls, 87, 88, 0, 0, 4);

            // Low masonry ribs shape doorways without sealing the running route.
            Block(walls, -18, -18, 2, 5, 0);
            Block(walls, 12, 12, 2, 5, 1);
            Block(walls, 37, 37, 2, 5, 2);
            Block(walls, 64, 64, 2, 5, 3);

            // Optional, stepped vertical route. Each rise is modest; the lower route remains open.
            Ledge(platforms, tiles, -25, -22, 0, 35); // entrance cache shelf
            Ledge(platforms, tiles, -5, -4, 0, 35);   // short, jumpable floor break
            Ledge(platforms, tiles, -13, -10, 1, 35);
            Ledge(platforms, tiles, -8, -5, 2, 35);
            Ledge(platforms, tiles, 3, 7, 1, 35);
            Ledge(platforms, tiles, 9, 12, 2, 35);
            Ledge(platforms, tiles, 20, 22, 0, 35);   // combat arena traversal gap
            Ledge(platforms, tiles, 20, 24, 1, 35);
            Ledge(platforms, tiles, 31, 35, 2, 35);
            Ledge(platforms, tiles, 40, 43, 0, 35);
            Ledge(platforms, tiles, 44, 47, 1, 35);
            Ledge(platforms, tiles, 48, 51, 2, 35);
            Ledge(platforms, tiles, 52, 56, 3, 35); // secret chamber shelf
            Ledge(platforms, tiles, 57, 61, 4, 35);
            Ledge(platforms, tiles, 66, 69, 1, 35);
            Ledge(platforms, tiles, 72, 75, 1, 35);
            Ledge(platforms, tiles, 83, 86, 2, 35);

            // Secret alcove masonry leaves a clear entrance and a safe jump back to the route.
            Block(walls, 52, 52, 4, 6, 3);
            Block(walls, 52, 56, 7, 7, 3);
            Block(walls, 62, 62, 5, 7, 3);

            // A quiet, deliberate scattering of rubble uses non-colliding terrain sprites.
            int[] rubble = { 10, 17, 18, 20, 30, 32, 33, 37, 39, 40, 41, 42 };
            int[,] rubbleAt = { { -31, 0 }, { -21, 0 }, { -12, 0 }, { 7, 0 }, { 15, 0 }, { 35, 0 }, { 40, 0 }, { 63, 0 }, { 69, 0 }, { 90, 0 } };
            for (int i = 0; i < rubbleAt.GetLength(0); i++)
                if (tiles.TryGetValue(rubble[i], out var tile)) decoration.SetTile(new Vector3Int(rubbleAt[i, 0], rubbleAt[i, 1], 0), tile);
            foreach (var map in new[] { ground, walls, platforms, decoration }) map.RefreshAllTiles();
        }

        private static void Ledge(Tilemap map, Dictionary<int, Tile> tiles, int x0, int x1, int y, int tileId)
        {
            if (!tiles.TryGetValue(tileId, out var tile)) return;
            for (int x = x0; x <= x1; x++) map.SetTile(new Vector3Int(x, y, 0), tile);
        }

        private static void BuildBackdrops(Transform sky, Transform mountains, Transform castle, Transform fog)
        {
            var skyline = Sprite("Large_Structures_Backgrounds/structure_bg_013.png");
            var castleFog = Sprite("Large_Structures_Backgrounds/structure_bg_010.png");
            var archSilhouette = Sprite("Large_Structures_Backgrounds/structure_bg_014.png");
            if (skyline != null)
            {
                for (int i = 0; i < 12; i++)
                {
                    float x = -35f + i * 11.5f;
                    MakeSprite(mountains, "DistantRuins_" + i, skyline, new Vector3(x, 5.2f, 24f), "Default", -30, 1.6f, i % 2 == 1);
                }
            }
            if (castleFog != null)
            {
                for (int i = 0; i < 14; i++)
                {
                    float x = -34f + i * 10.5f;
                    MakeSprite(fog, "FogBank_" + i, castleFog, new Vector3(x, 1.9f, 20f), "Default", -25, 1.35f, i % 2 == 0);
                }
            }
            if (archSilhouette != null)
            {
                for (int i = 0; i < 6; i++)
                {
                    float x = -28f + i * 24f;
                    MakeSprite(castle, "FarArch_" + i, archSilhouette, new Vector3(x, 4.3f, 15f), "Default", -12, 1.2f, i % 2 == 1);
                }
            }
            _ = sky; // Explicit hierarchy node for a future sky gradient/background asset.
        }

        private static void BuildArchitecture(Transform ruins, Transform architecture, Transform decoration)
        {
            // Non-colliding silhouettes and framing pieces are deliberately behind the gameplay tiles.
            PlaceArch(ruins, "FarGothicArch", "Architecture/arch_door_platform_009.png", -27f, 4.2f, 12f, -10, 1.25f);
            PlaceArch(ruins, "NaveGothicArch", "Architecture/arch_door_platform_009.png", -2f, 4.3f, 12f, -10, 1.25f);
            PlaceArch(ruins, "OssuaryGothicArch", "Architecture/arch_door_platform_009.png", 42f, 4.2f, 12f, -10, 1.25f);
            PlaceArch(ruins, "BossGothicArch", "Architecture/arch_door_platform_009.png", 78f, 4.3f, 12f, -10, 1.4f);

            float[] columns = { -18f, -11f, 1f, 12f, 18f, 37f, 44f, 64f, 70f, 88f };
            for (int i = 0; i < columns.Length; i++)
                PlaceArch(architecture, "GothicPillar_" + i, i % 3 == 0 ? "Architecture/wall_column_003.png" : "Architecture/wall_column_007.png", columns[i], 0.9f, 4f, 1, 1f);

            PlaceArch(architecture, "EntranceRelief", "Architecture/wall_column_002.png", -25.5f, 4.1f, 7f, -1, 1f);
            PlaceArch(architecture, "NaveRelief", "Architecture/wall_column_006.png", 6f, 4.1f, 7f, -1, 1f);
            PlaceArch(architecture, "OssuaryRelief", "Architecture/wall_column_020.png", 46f, 4f, 7f, -1, 1f);
            PlaceArch(architecture, "BossRelief", "Architecture/wall_column_021.png", 85f, 4f, 7f, -1, 1f);

            PlaceArch(architecture, "EntranceWindow", "Architecture/arch_door_platform_001.png", -21f, 4.1f, 8f, 1, 1f);
            PlaceArch(architecture, "NaveWindow", "Architecture/arch_door_platform_002.png", 14f, 4.1f, 8f, 1, 1f);
            PlaceArch(architecture, "ArenaWindow", "Architecture/arch_door_platform_003.png", 34f, 4.1f, 8f, 1, 1f);
            PlaceArch(architecture, "SecretBarredWindow", "Architecture/arch_door_platform_005.png", 60f, 10f, 8f, 1, 1f);
            PlaceArch(architecture, "BossGateFrame", "Architecture/arch_door_platform_005.png", 90f, 1.5f, 2f, 4, 1.05f);

            PlaceArch(decoration, "SuspendedBridge_01", "Architecture/arch_door_platform_006.png", -4f, 6.4f, 9f, 0, 1f);
            PlaceArch(decoration, "SuspendedBridge_02", "Architecture/arch_door_platform_007.png", 54f, 11f, 9f, 0, 1f);
            PlaceArch(decoration, "Balcony_01", "Architecture/arch_door_platform_012.png", 22f, 4f, 6f, 1, 1f);
            PlaceArch(decoration, "Balcony_02", "Architecture/arch_door_platform_013.png", 81f, 5f, 6f, 1, 1f);
            PlaceArch(decoration, "SecretChainA", "Architecture/arch_door_platform_010.png", 53f, 5.5f, 9f, 2, 1f);
            PlaceArch(decoration, "SecretChainB", "Architecture/arch_door_platform_011.png", 62f, 5.7f, 9f, 2, 1f);
        }

        private static void BuildProps(Transform torches, Transform lanterns, Transform barrels,
            Transform crates, Transform chests, Transform chains, Transform banners, Scene scene)
        {
            float[] torchX = { -30f, -17f, -6f, 11f, 19f, 36f, 43f, 63f, 68f, 87f };
            var torchPrefab = CreateSpritePrefab("EnvironmentTorch", "Props_Decoration/prop_002.png", "Fround", 10, true);
            var lanternPrefab = CreateSpritePrefab("HangingLantern", "Props_Decoration/prop_006.png", "Fround", 10, true);
            if (torchPrefab != null)
                for (int i = 0; i < torchX.Length; i++) InstantiateAt(torchPrefab, scene, torches, "WallTorch_" + i, new Vector3(torchX[i], 2.1f, -0.3f));
            if (lanternPrefab != null)
                foreach (var x in new[] { -9f, 28f, 50f, 75f }) InstantiateAt(lanternPrefab, scene, lanterns, "HangingLantern_" + x, new Vector3(x, 5.5f, -0.2f));

            var banner = CreateSpritePrefab("GothicBanner", "Props_Decoration/prop_011.png", "Fround", 5, false);
            if (banner != null)
                for (int i = 0; i < 5; i++) InstantiateAt(banner, scene, banners, "Banner_" + i, new Vector3(-8f + i * 21f, 4.6f, -0.25f));

            PlaceProp(chests, "TreasureChest_Entrance", "Props_Decoration/prop_007.png", -23.5f, 1.4f, -0.2f, "Fround", 8);
            PlaceProp(chests, "TreasureChest_Secret", "Props_Decoration/prop_008.png", 54f, 4.45f, -0.2f, "Fround", 8);
            PlaceProp(crates, "Crate_01", "Props_Decoration/prop_022.png", 17f, 0.6f, -0.2f, "Fround", 7);
            PlaceProp(crates, "Crate_02", "Props_Decoration/prop_025.png", 67f, 0.6f, -0.2f, "Fround", 7);
            PlaceProp(barrels, "Barrel_01", "Props_Decoration/prop_024.png", 15.7f, 0.5f, -0.2f, "Fround", 7);
            PlaceProp(barrels, "Barrel_02", "Props_Decoration/prop_026.png", 68.5f, 0.5f, -0.2f, "Fround", 7);
            PlaceProp(midDecorTransform(chains), "IronCage", "Props_Decoration/prop_018.png", 31f, 5.8f, 4f, "Default", -2);
            PlaceProp(midDecorTransform(chains), "Chandelier", "Props_Decoration/prop_014.png", 79f, 6.6f, 4f, "Default", -2);
            PlaceProp(chains, "SkullAndBones", "Props_Decoration/prop_023.png", 41f, 0.45f, -0.15f, "Fround", 5);
            PlaceProp(chains, "ThornVine_01", "Props_Decoration/prop_029.png", 49f, 3.2f, -0.15f, "Default", -1);
            PlaceProp(chains, "ThornVine_02", "Props_Decoration/prop_033.png", 88f, 3.2f, -0.15f, "Default", -1);
            PlaceProp(midDecorTransform(chains), "Statue_01", "Props_Decoration/prop_016.png", -14f, 1.1f, 0.5f, "Default", 0);
            PlaceProp(midDecorTransform(chains), "Statue_02", "Props_Decoration/prop_016.png", 60.5f, 5f, 0.5f, "Default", 0);
        }

        // Keep architecture-like chain and statue silhouettes grouped in the non-colliding midground.
        private static Transform midDecorTransform(Transform propsGroup)
        {
            var level = propsGroup;
            while (level.parent != null && level.name != "Level_01") level = level.parent;
            var mid = level.Find("Environment/Midground/Midground_Decoration");
            return mid != null ? mid : propsGroup;
        }

        private static void BuildGameplay(Scene scene, Transform spawn, Transform enemySpawns, Transform hazards,
            Transform collectibles, Transform exitGroup)
        {
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            if (playerPrefab == null)
            {
                Debug.LogError("[SeparatedTilesetMap1Builder] Assets/Prefabs/Player.prefab is missing.");
                return;
            }
            var player = PrefabUtility.InstantiatePrefab(playerPrefab, scene) as GameObject;
            player.name = "Player";
            player.transform.SetParent(spawn.parent, true);
            player.transform.position = spawn.position;

            SpawnEncounter("Assets/Prefabs/Scarecrow 1.prefab", "Encounter_01_Melee", new Vector3(-3.5f, 0f, 0f), enemySpawns, player.transform, scene);
            SpawnEncounter("Assets/Prefabs/Roguelite/RangedEnemy.prefab", "Encounter_02_Ranged", new Vector3(28f, 0.33f, 0f), enemySpawns, player.transform, scene);
            SpawnEncounter("Assets/Prefabs/Scarecrow 1.prefab", "Encounter_03_Melee", new Vector3(53f, 0f, 0f), enemySpawns, player.transform, scene);
            SpawnEncounter("Assets/Prefabs/Boos.prefab", "Encounter_Final_Boss", new Vector3(80f, 2f, 0f), enemySpawns, player.transform, scene);

            // The existing CheckRoi handler respawns the player at the level's spawn marker.
            var fall = FindRoot("CheckRoi");
            if (fall != null)
            {
                fall.transform.SetParent(hazards, false);
                fall.transform.position = new Vector3(29f, -9f, 0f);
                fall.tag = "checkroi";
                foreach (var renderer in fall.GetComponentsInChildren<SpriteRenderer>(true)) renderer.enabled = false;
                var box = fall.GetComponent<BoxCollider2D>() ?? fall.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                box.size = new Vector2(140f, 1.5f);
            }

            var exit = new GameObject("Exit_To_Map_2");
            exit.transform.SetParent(exitGroup, false);
            exit.transform.position = new Vector3(91f, 0.9f, 0f);
            exit.tag = "Key";
            var exitCollider = exit.AddComponent<BoxCollider2D>();
            exitCollider.isTrigger = true;
            exitCollider.size = new Vector2(1.5f, 2.5f);
            var coins = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Coin_0.prefab");
            if (coins != null)
            {
                var coin = PrefabUtility.InstantiatePrefab(coins, scene) as GameObject;
                coin.name = "Secret_Treasure_Coin";
                coin.transform.SetParent(collectibles, true);
                coin.transform.position = new Vector3(55f, 4.8f, 0f);
            }
        }

        private static void SpawnEncounter(string prefabPath, string name, Vector3 position, Transform markerParent,
            Transform player, Scene scene)
        {
            var marker = new GameObject("SpawnMarker_" + name);
            marker.transform.SetParent(markerParent, false);
            marker.transform.position = position;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning("[SeparatedTilesetMap1Builder] Enemy prefab missing: " + prefabPath);
                return;
            }
            var instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (instance == null) return;
            instance.name = name;
            instance.transform.SetParent(markerParent, true);
            instance.transform.position = position;
            foreach (var behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null) continue;
                var serialized = new SerializedObject(behaviour);
                var playerField = serialized.FindProperty("player");
                if (playerField != null && playerField.propertyType == SerializedPropertyType.ObjectReference)
                {
                    playerField.objectReferenceValue = player;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.RecordPrefabInstancePropertyModifications(behaviour);
                }
            }
        }

        private static void ConfigureCamera(Scene scene, Transform cameraGroup, Tilemap ground)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == "Main Camera" || root.name == "CinemachineCamera") root.transform.SetParent(cameraGroup, true);
            }
            var player = GameObject.Find("Player");
            var main = GameObject.Find("Main Camera");
            if (main != null)
            {
                var camera = main.GetComponent<Camera>();
                if (camera != null) camera.backgroundColor = new Color(0.045f, 0.04f, 0.075f, 1f);
            }
            var vcam = GameObject.Find("CinemachineCamera");
            if (vcam == null) return;
            foreach (var component in vcam.GetComponents<Component>())
            {
                if (component == null) continue;
                if (component is CinemachineCamera cine && player != null)
                {
                    var so = new SerializedObject(cine);
                    var target = so.FindProperty("Target.TrackingTarget");
                    if (target != null)
                    {
                        target.objectReferenceValue = player.transform;
                        so.ApplyModifiedPropertiesWithoutUndo();
                        EditorUtility.SetDirty(cine);
                    }
                }
                if (component.GetType().Name.Contains("CinemachineConfiner2D"))
                {
                    var so = new SerializedObject(component);
                    var bounds = so.FindProperty("BoundingShape2D");
                    if (bounds != null)
                    {
                        bounds.objectReferenceValue = ground.GetComponent<TilemapCollider2D>();
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
            }
        }

        private static Tilemap CreateTilemap(Transform parent, string name, string sortingLayer, int order,
            int physicsLayer, bool collision, bool oneWay)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.layer = physicsLayer;
            var map = go.AddComponent<Tilemap>();
            map.tileAnchor = new Vector3(0.5f, 0.5f, 0f);
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.sortingLayerName = sortingLayer;
            renderer.sortingOrder = order;
            renderer.mode = TilemapRenderer.Mode.Individual;
            if (collision)
            {
                var rb = go.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Static;
                var collider = go.AddComponent<TilemapCollider2D>();
                collider.usedByComposite = true;
                var composite = go.AddComponent<CompositeCollider2D>();
                composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
                if (oneWay)
                {
                    var effector = go.AddComponent<PlatformEffector2D>();
                    effector.useOneWay = true;
                    effector.surfaceArc = 170f;
                    composite.usedByEffector = true;
                }
            }
            return map;
        }

        private static GameObject CreateSpritePrefab(string prefabName, string spriteRelativePath,
            string sortingLayer, int order, bool light)
        {
            string prefabPath = PrefabRoot + "/" + prefabName + ".prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existing != null) return existing;
            var sprite = Sprite(spriteRelativePath);
            if (sprite == null) return null;
            var obj = new GameObject(prefabName);
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerName = sortingLayer;
            renderer.sortingOrder = order;
            if (light)
            {
                var point = obj.AddComponent<Light2D>();
                point.lightType = Light2D.LightType.Point;
                point.color = new Color(1f, 0.49f, 0.2f, 1f);
                point.intensity = 0.75f;
                point.pointLightOuterRadius = 2.1f;
                point.pointLightInnerRadius = 0.3f;
                point.transform.localPosition = new Vector3(0f, 0.55f, -0.1f);
            }
            var saved = PrefabUtility.SaveAsPrefabAsset(obj, prefabPath);
            UnityEngine.Object.DestroyImmediate(obj);
            return saved;
        }

        private static void PlaceProp(Transform parent, string name, string path, float x, float y,
            float z, string sortingLayer, int order)
        {
            var sprite = Sprite(path);
            if (sprite == null) return;
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.position = new Vector3(x, y, z);
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerName = sortingLayer;
            renderer.sortingOrder = order;
        }

        private static GameObject MakeSprite(Transform parent, string name, Sprite sprite, Vector3 position,
            string sortingLayer, int order, float scale = 1f, bool flipX = false)
        {
            if (sprite == null) return null;
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.position = position;
            obj.transform.localScale = new Vector3(flipX ? -scale : scale, scale, 1f);
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerName = sortingLayer;
            renderer.sortingOrder = order;
            return obj;
        }

        private static void PlaceArch(Transform parent, string name, string relativePath,
            float x, float y, float z, int order, float scale)
        {
            MakeSprite(parent, name, Sprite(relativePath), new Vector3(x, y, z), "Default", order, scale);
        }

        private static void InstantiateAt(GameObject prefab, Scene scene, Transform parent, string name, Vector3 position)
        {
            var instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (instance == null) return;
            instance.name = name;
            instance.transform.SetParent(parent, true);
            instance.transform.position = position;
        }

        private static Sprite Sprite(string relativePath) =>
            AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + "/" + relativePath);

        private static Transform Child(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static GameObject FindRoot(string name)
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                if (root.name == name) return root;
            return null;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folder = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            if (!string.IsNullOrEmpty(parent)) AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
#endif
