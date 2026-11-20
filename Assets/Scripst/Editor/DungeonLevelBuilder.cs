#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using Unity.Cinemachine;

namespace Knight.Editor
{
    /// <summary>
    /// Reusable Unity Editor tool to build a complete 2D Gothic Dungeon level (Dead Cells inspired)
    /// using ONLY the sprites extracted from Assets/Art/Source/DungeonTileset_Original.png.
    /// Hierarchy:
    /// Dungeon_Level_01
    /// ├── Grid
    /// │   ├── Background_Tilemap
    /// │   ├── BackWall_Tilemap
    /// │   ├── Ground_Tilemap
    /// │   ├── Platform_Tilemap
    /// │   ├── Foreground_Tilemap
    /// │   └── Decoration_Tilemap
    /// ├── Architecture
    /// ├── Props
    /// ├── Lighting
    /// ├── PlayerSpawn
    /// └── Exit
    /// </summary>
    public static class DungeonLevelBuilder
    {
        private const string ExtractedRoot = "Assets/Art/Extracted";
        private const string TileRoot = "Assets/Tile/Dungeon_Level_01";
        private const string ScenePath = "Assets/Scenes/Dungeon_Level_01.unity";
        private const string Map1Path = "Assets/Scenes/map 1.unity";

        [MenuItem("Dead Cells Studio/Build Dungeon Level 01")]
        public static void BuildMenu()
        {
            BuildDungeon(false);
        }

        [MenuItem("Dead Cells Studio/Build and Apply to Map 1")]
        public static void BuildAndApplyToMap1Menu()
        {
            BuildDungeon(true);
        }

        public static void BuildFromCommandLine()
        {
            Debug.Log("[DungeonLevelBuilder] Starting batch build from command line...");
            BuildDungeon(true);
            Debug.Log("[DungeonLevelBuilder] Batch build finished successfully.");
        }

        public static void BuildDungeon(bool alsoUpdateMap1)
        {
            Debug.Log("<color=cyan>=== [DungeonLevelBuilder] Phase 3: Configure Sprite Importers ===</color>");
            ConfigureSpriteImporters();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=cyan>=== [DungeonLevelBuilder] Phase 3: Create Tile Assets ===</color>");
            var tileDict = CreateTileAssets();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=cyan>=== [DungeonLevelBuilder] Phase 4 & 5: Construct Dungeon_Level_01 Scene ===</color>");
            ConstructScene(ScenePath, tileDict);

            if (alsoUpdateMap1)
            {
                Debug.Log("<color=cyan>=== [DungeonLevelBuilder] Applying layout to Map 1 ===</color>");
                ConstructScene(Map1Path, tileDict);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=green>=== [DungeonLevelBuilder] SUCCESS! Dungeon Level Built Completely ===</color>");
        }

        private static void ConfigureSpriteImporters()
        {
            if (!Directory.Exists(ExtractedRoot))
            {
                Debug.LogError($"Extracted folder not found: {ExtractedRoot}");
                return;
            }

            string[] pngFiles = Directory.GetFiles(ExtractedRoot, "*.png", SearchOption.AllDirectories);
            int count = 0;

            foreach (var path in pngFiles)
            {
                string unityPath = path.Replace('\\', '/');
                if (unityPath.Contains("preview") || unityPath.Contains("test")) continue;

                var importer = AssetImporter.GetAtPath(unityPath) as TextureImporter;
                if (importer != null)
                {
                    bool changed = false;
                    if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; changed = true; }
                    if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; changed = true; }
                    if (importer.filterMode != FilterMode.Point) { importer.filterMode = FilterMode.Point; changed = true; }
                    if (importer.textureCompression != TextureImporterCompression.Uncompressed) { importer.textureCompression = TextureImporterCompression.Uncompressed; changed = true; }
                    if (importer.mipmapEnabled) { importer.mipmapEnabled = false; changed = true; }
                    if (Mathf.Abs(importer.spritePixelsPerUnit - 32f) > 0.1f) { importer.spritePixelsPerUnit = 32f; changed = true; }

                    if (changed)
                    {
                        importer.SaveAndReimport();
                        count++;
                    }
                }
            }
            Debug.Log($"Configured {count} sprite texture importers (Point, No Mipmaps, 32 PPU).");
        }

        private static Dictionary<string, Tile> CreateTileAssets()
        {
            if (!Directory.Exists(TileRoot)) Directory.CreateDirectory(TileRoot);

            var dict = new Dictionary<string, Tile>();
            string[] searchFolders = {
                ExtractedRoot + "/Ground_Floor",
                ExtractedRoot + "/Platforms",
                ExtractedRoot + "/Walls_Architecture"
            };

            foreach (var folder in searchFolders)
            {
                if (!Directory.Exists(folder)) continue;
                string[] files = Directory.GetFiles(folder, "*.png", SearchOption.TopDirectoryOnly);

                foreach (var file in files)
                {
                    string assetPath = file.Replace('\\', '/');
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                    if (sprite == null) continue;

                    string tileAssetPath = $"{TileRoot}/{sprite.name}_Tile.asset";
                    var existingTile = AssetDatabase.LoadAssetAtPath<Tile>(tileAssetPath);

                    if (existingTile == null)
                    {
                        var tile = ScriptableObject.CreateInstance<Tile>();
                        tile.sprite = sprite;
                        tile.name = sprite.name;
                        tile.colliderType = Tile.ColliderType.Grid;
                        AssetDatabase.CreateAsset(tile, tileAssetPath);
                        dict[sprite.name] = tile;
                    }
                    else
                    {
                        existingTile.sprite = sprite;
                        existingTile.colliderType = Tile.ColliderType.Grid;
                        EditorUtility.SetDirty(existingTile);
                        dict[sprite.name] = existingTile;
                    }
                }
            }
            Debug.Log($"Loaded/Created {dict.Count} Tile assets in {TileRoot}.");
            return dict;
        }

        private static Sprite LoadExtractedSprite(string category, string name)
        {
            string path = $"{ExtractedRoot}/{category}/{name}.png";
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sp == null)
            {
                // Fallback search
                string[] found = Directory.GetFiles(ExtractedRoot, $"{name}.png", SearchOption.AllDirectories);
                if (found.Length > 0)
                {
                    sp = AssetDatabase.LoadAssetAtPath<Sprite>(found[0].Replace('\\', '/'));
                }
            }
            return sp;
        }

        private static void ConstructScene(string targetScenePath, Dictionary<string, Tile> tiles)
        {
            // Open or create target scene
            UnityEngine.SceneManagement.Scene scene;
            if (File.Exists(targetScenePath))
            {
                scene = EditorSceneManager.OpenScene(targetScenePath, OpenSceneMode.Single);
            }
            else
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }

            // Remove existing Dungeon_Level_01 root if present to allow idempotent rebuilds
            var existingRoot = GameObject.Find("Dungeon_Level_01");
            if (existingRoot != null) GameObject.DestroyImmediate(existingRoot);

            // Create Master Hierarchy
            var root = new GameObject("Dungeon_Level_01");

            var gridObj = new GameObject("Grid");
            gridObj.transform.SetParent(root.transform);
            var grid = gridObj.AddComponent<Grid>();
            grid.cellSize = new Vector3(1.625f, 1.625f, 0f); // 52px / 32ppu ≈ 1.625 units

            // 6 Tilemaps per prompt specification
            var bgTilemap = CreateTilemap(gridObj, "Background_Tilemap", -20);
            var backWallTilemap = CreateTilemap(gridObj, "BackWall_Tilemap", -10);
            var groundTilemap = CreateTilemap(gridObj, "Ground_Tilemap", 0, addCollision: true);
            var platformTilemap = CreateTilemap(gridObj, "Platform_Tilemap", 5, addCollision: true, isPlatformEffector: true);
            var fgTilemap = CreateTilemap(gridObj, "Foreground_Tilemap", 20);
            var decoTilemap = CreateTilemap(gridObj, "Decoration_Tilemap", 10);

            var archGroup = new GameObject("Architecture"); archGroup.transform.SetParent(root.transform);
            var propsGroup = new GameObject("Props"); propsGroup.transform.SetParent(root.transform);
            var lightsGroup = new GameObject("Lighting"); lightsGroup.transform.SetParent(root.transform);
            var playerSpawn = new GameObject("PlayerSpawn"); playerSpawn.transform.SetParent(root.transform);
            playerSpawn.transform.position = new Vector3(-20f, 1.5f, 0f);
            var exitObj = new GameObject("Exit"); exitObj.transform.SetParent(root.transform);
            exitObj.transform.position = new Vector3(28f, 12.0f, 0f);

            // ==========================================
            // TILEMAP POPULATION
            // ==========================================

            // Helper tile lookups
            Tile GetTile(string name) => tiles.ContainsKey(name) ? tiles[name] : null;

            var tFloor = GetTile("tile_stone_floor") ?? GetTile("tile_stone_block_01");
            var tCracked = GetTile("tile_stone_cracked_01") ?? tFloor;
            var tBlock = GetTile("tile_stone_block_01") ?? tFloor;
            var tBlock2 = GetTile("tile_stone_block_02") ?? tBlock;
            var tCornerL = GetTile("tile_stone_corner_L") ?? tFloor;
            var tCornerR = GetTile("tile_stone_corner_R") ?? tFloor;
            var tMossFloor = GetTile("tile_mossy_floor") ?? tFloor;
            var tMossBlock = GetTile("tile_mossy_block_01") ?? tBlock;
            var tWallPanel = GetTile("wall_brick_large_panel") ?? tBlock;
            var tWallCol = GetTile("wall_brick_column_01") ?? tBlock;
            var tPlatStone = GetTile("platform_long_stone") ?? tFloor;
            var tPlatMoss = GetTile("platform_long_mossy") ?? tMossFloor;

            // 1. LOWER LEVEL GROUND FLOOR (x = -25 to +35, y = 0 to -3)
            for (int x = -25; x <= 35; x++)
            {
                // Surface tile (y = 0)
                Tile surfaceTile = tFloor;
                if (x == -25) surfaceTile = tCornerL;
                else if (x == 35) surfaceTile = tCornerR;
                else if (x % 5 == 0) surfaceTile = tMossFloor;
                else if (x % 3 == 0) surfaceTile = tCracked;

                groundTilemap.SetTile(new Vector3Int(x, 0, 0), surfaceTile);

                // Deep bedrock (y = -1, -2, -3)
                for (int y = -1; y >= -3; y--)
                {
                    Tile subTile = ((x + y) % 2 == 0) ? tBlock : tBlock2;
                    if (y == -1 && (x % 6 == 0)) subTile = tMossBlock;
                    groundTilemap.SetTile(new Vector3Int(x, y, 0), subTile);
                }
            }

            // Left & Right boundary walls
            for (int y = 1; y <= 16; y++)
            {
                groundTilemap.SetTile(new Vector3Int(-25, y, 0), tBlock);
                groundTilemap.SetTile(new Vector3Int(35, y, 0), tBlock);
            }

            // 2. BACK WALL (y = 0 to 14, x = -24 to 34)
            for (int x = -24; x <= 34; x += 2)
            {
                for (int y = 0; y <= 14; y += 2)
                {
                    backWallTilemap.SetTile(new Vector3Int(x, y, 0), tWallPanel ?? tBlock);
                }
            }

            // 3. MIDDLE LEVEL PLATFORMS (y = 4 to 8)
            // Platform Tier 1: x in [-18, -12], y = 4
            for (int x = -18; x <= -12; x++) platformTilemap.SetTile(new Vector3Int(x, 4, 0), tPlatStone);

            // Platform Tier 2: x in [-9, -3], y = 6
            for (int x = -9; x <= -3; x++) platformTilemap.SetTile(new Vector3Int(x, 6, 0), tPlatStone);

            // Platform Tier 3 (Bridge): x in [2, 10], y = 5
            for (int x = 2; x <= 10; x++) platformTilemap.SetTile(new Vector3Int(x, 5, 0), tPlatStone);

            // Platform Tier 4: x in [15, 23], y = 7
            for (int x = 15; x <= 23; x++) platformTilemap.SetTile(new Vector3Int(x, 7, 0), tPlatMoss);

            // 4. UPPER LEVEL PLATFORMS (y = 10 to 12)
            // Upper Terrace Left: x in [-15, -6], y = 10
            for (int x = -15; x <= -6; x++) platformTilemap.SetTile(new Vector3Int(x, 10, 0), tPlatStone);

            // Upper Shrine Center: x in [-2, 12], y = 11
            for (int x = -2; x <= 12; x++) platformTilemap.SetTile(new Vector3Int(x, 11, 0), tPlatStone);

            // Upper Exit Terrace Right: x in [18, 30], y = 11
            for (int x = 18; x <= 30; x++) platformTilemap.SetTile(new Vector3Int(x, 11, 0), tPlatMoss);

            // ==========================================
            // ARCHITECTURE SPRITES (Columns, Arches, Windows)
            // ==========================================
            void PlaceArch(string cat, string name, Vector3 pos, int sortingOrder = -5)
            {
                var sp = LoadExtractedSprite(cat, name);
                if (sp == null) return;
                var go = new GameObject(name);
                go.transform.SetParent(archGroup.transform);
                go.transform.position = pos;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sp;
                sr.sortingOrder = sortingOrder;
            }

            // Portal Entrance (x = -22, y = 1.0)
            PlaceArch("Doors_Windows", "arch_grand_gateway", new Vector3(-21f, 1.8f, 0f), sortingOrder: -2);

            // Pillars supporting bridge and platforms
            PlaceArch("Walls_Architecture", "pillar_fluted", new Vector3(-15f, 2.3f, 0f));
            PlaceArch("Walls_Architecture", "pillar_ornate_capital", new Vector3(-6f, 3.8f, 0f));
            PlaceArch("Walls_Architecture", "pillar_plain_round", new Vector3(3f, 3.0f, 0f));
            PlaceArch("Walls_Architecture", "pillar_cracked_round", new Vector3(9f, 3.0f, 0f));
            PlaceArch("Walls_Architecture", "pillar_fluted", new Vector3(18f, 4.3f, 0f));
            PlaceArch("Walls_Architecture", "pillar_ornate_capital", new Vector3(24f, 4.3f, 0f));

            // Bridge balustrade on middle platform
            PlaceArch("Doors_Windows", "bridge_balustrade", new Vector3(6f, 8.8f, 0f), sortingOrder: 6);

            // Upper Level Ruined arches & stairs
            PlaceArch("Walls_Architecture", "arch_ruined_pillar", new Vector3(-8f, 12.5f, 0f));
            PlaceArch("Walls_Architecture", "stairs_stone_broken_M", new Vector3(14f, 11.5f, 0f));

            // 4 Gothic Windows in Upper Back Wall
            PlaceArch("Doors_Windows", "window_gothic_empty", new Vector3(-10f, 18.5f, 0f), sortingOrder: -8);
            PlaceArch("Doors_Windows", "window_gothic_rose_cracked", new Vector3(-3f, 19.0f, 0f), sortingOrder: -8);
            PlaceArch("Doors_Windows", "window_gothic_stained_glass", new Vector3(5f, 19.0f, 0f), sortingOrder: -8);
            PlaceArch("Doors_Windows", "window_gothic_iron_grate", new Vector3(12f, 18.5f, 0f), sortingOrder: -8);

            // Grand Exit Door (x = 28, y = 18.5)
            PlaceArch("Doors_Windows", "door_grand_wooden", new Vector3(26.5f, 19.0f, 0f), sortingOrder: 2);

            // ==========================================
            // PROPS & DECORATIONS
            // ==========================================
            void PlaceProp(string cat, string name, Vector3 pos, int sortingOrder = 8)
            {
                var sp = LoadExtractedSprite(cat, name);
                if (sp == null) return;
                var go = new GameObject(name);
                go.transform.SetParent(propsGroup.transform);
                go.transform.position = pos;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sp;
                sr.sortingOrder = sortingOrder;
            }

            // Lower Level Props (Barrels, crates, skulls, lanterns, candles)
            PlaceProp("Decorations", "barrel_pair", new Vector3(-17f, 1.2f, 0f));
            PlaceProp("Props_Lighting", "lantern_standing_01", new Vector3(-14f, 1.2f, 0f));
            PlaceProp("Decorations", "crate_wooden_01", new Vector3(-10f, 1.2f, 0f));
            PlaceProp("Decorations", "barrel_single_01", new Vector3(-8f, 1.2f, 0f));
            PlaceProp("Props_Lighting", "candles_cluster_large", new Vector3(-3f, 1.2f, 0f));
            PlaceProp("Decorations", "skulls_and_bones_01", new Vector3(0f, 1.0f, 0f));
            PlaceProp("Decorations", "brambles_thorns_01", new Vector3(4f, 1.2f, 0f));
            PlaceProp("Decorations", "shield_metal_kite", new Vector3(8f, 1.2f, 0f));
            PlaceProp("Decorations", "crate_wooden_02", new Vector3(14f, 1.2f, 0f));
            PlaceProp("Decorations", "skull_floor_pile", new Vector3(17f, 1.0f, 0f));
            PlaceProp("Props_Lighting", "candles_candelabra", new Vector3(22f, 1.2f, 0f));
            PlaceProp("Decorations", "remains_bones_pile", new Vector3(25f, 1.0f, 0f));

            // Wall Torches
            PlaceProp("Props_Lighting", "torch_wall_lit_01", new Vector3(-18f, 3.5f, 0f));
            PlaceProp("Props_Lighting", "torch_wall_lit_02", new Vector3(-2f, 3.5f, 0f));
            PlaceProp("Props_Lighting", "torch_wall_lit_01", new Vector3(12f, 3.5f, 0f));
            PlaceProp("Props_Lighting", "torch_wall_lit_02", new Vector3(24f, 20.0f, 0f));
            PlaceProp("Props_Lighting", "torch_wall_lit_01", new Vector3(29f, 20.0f, 0f));

            // Hanging Chains & Cages
            PlaceProp("Decorations", "chains_hanging_long", new Vector3(-6f, 9.5f, 0f));
            PlaceProp("Decorations", "cage_iron_hanging", new Vector3(0f, 8.5f, 0f));
            PlaceProp("Decorations", "chains_hanging_short", new Vector3(18f, 11.0f, 0f));

            // Middle & Upper Level Props & Rewards
            PlaceProp("Decorations", "chest_closed", new Vector3(-16f, 7.5f, 0f));
            PlaceProp("Decorations", "banner_heraldic_orange", new Vector3(-13f, 8.2f, 0f));
            PlaceProp("Decorations", "shield_wooden_cracked", new Vector3(-8f, 10.8f, 0f));
            PlaceProp("Decorations", "chest_half_open", new Vector3(7f, 9.2f, 0f));
            PlaceProp("Decorations", "banner_tattered_blue", new Vector3(16f, 12.2f, 0f));
            PlaceProp("Decorations", "chest_treasure_open", new Vector3(22f, 12.2f, 0f));

            // Sacred Shrine on Upper Platform
            PlaceProp("Decorations", "statue_hooded_justice", new Vector3(4f, 19.5f, 0f), sortingOrder: 4);
            PlaceProp("Decorations", "crystals_purple_large", new Vector3(1.5f, 18.5f, 0f), sortingOrder: 6);
            PlaceProp("Decorations", "crystals_green_purple", new Vector3(6.5f, 18.5f, 0f), sortingOrder: 6);
            PlaceProp("Decorations", "runes_glowing_symbols", new Vector3(4f, 18.2f, 0f), sortingOrder: 6);

            // ==========================================
            // BACKGROUND SILHOUETTES
            // ==========================================
            void PlaceSilhouette(string name, Vector3 pos)
            {
                var sp = LoadExtractedSprite("Background_Silhouettes", name);
                if (sp == null) return;
                var go = new GameObject(name);
                go.transform.SetParent(root.transform);
                go.transform.position = pos;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sp;
                sr.sortingOrder = -25;
                sr.color = new Color(0.4f, 0.38f, 0.48f, 1f);
            }

            PlaceSilhouette("bg_silhouette_cathedral_towers", new Vector3(-12f, 5f, 5f));
            PlaceSilhouette("bg_silhouette_arcade_arches", new Vector3(6f, 5f, 5f));
            PlaceSilhouette("bg_silhouette_pillars_drapery", new Vector3(24f, 5f, 5f));

            // ==========================================
            // LIGHTING (Warm torch lights & Cool crystal glows)
            // ==========================================
            void CreateLight(Vector3 pos, Color color, float intensity, float radius)
            {
                var lObj = new GameObject("Light_2D");
                lObj.transform.SetParent(lightsGroup.transform);
                lObj.transform.position = pos;
                var light2d = lObj.AddComponent<Light2D>();
                light2d.lightType = Light2D.LightType.Point;
                light2d.color = color;
                light2d.intensity = intensity;
                light2d.pointLightOuterRadius = radius;
            }

            Color warmTorch = new Color(1.0f, 0.65f, 0.25f);
            Color coolCrystal = new Color(0.6f, 0.3f, 1.0f);

            CreateLight(new Vector3(-18f, 3.5f, 0f), warmTorch, 1.2f, 5f);
            CreateLight(new Vector3(-2f, 3.5f, 0f), warmTorch, 1.2f, 5f);
            CreateLight(new Vector3(12f, 3.5f, 0f), warmTorch, 1.2f, 5f);
            CreateLight(new Vector3(4f, 19.5f, 0f), coolCrystal, 1.5f, 7f);
            CreateLight(new Vector3(26.5f, 19.0f, 0f), warmTorch, 1.5f, 6f);

            // Global ambient light
            var globalLightObj = new GameObject("GlobalLight");
            globalLightObj.transform.SetParent(lightsGroup.transform);
            var globalLight = globalLightObj.AddComponent<Light2D>();
            globalLight.lightType = Light2D.LightType.Global;
            globalLight.color = new Color(0.45f, 0.42f, 0.55f);
            globalLight.intensity = 0.85f;

            // ==========================================
            // GAMEPLAY ENTITIES (Player, Enemies, Pickups, Camera)
            // ==========================================

            // 1. Player
            GameObject playerInstance = null;
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            if (playerPrefab != null)
            {
                playerInstance = PrefabUtility.InstantiatePrefab(playerPrefab) as GameObject;
                if (playerInstance != null)
                {
                    playerInstance.name = "Player";
                    playerInstance.transform.position = playerSpawn.transform.position;
                }
            }

            // 2. Enemies
            var scarecrowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Scarecrow 1.prefab");
            if (scarecrowPrefab != null)
            {
                // Patrol enemy 1 on lower ground
                var e1 = PrefabUtility.InstantiatePrefab(scarecrowPrefab) as GameObject;
                e1.transform.position = new Vector3(-5f, 1.2f, 0f);

                // Patrol enemy 2 on middle platform
                var e2 = PrefabUtility.InstantiatePrefab(scarecrowPrefab) as GameObject;
                e2.transform.position = new Vector3(8f, 9.0f, 0f);
            }

            var rangedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Roguelite/RangedEnemy.prefab");
            if (rangedPrefab != null)
            {
                var r1 = PrefabUtility.InstantiatePrefab(rangedPrefab) as GameObject;
                r1.transform.position = new Vector3(19f, 12.2f, 0f);
            }

            var bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Boos.prefab");
            if (bossPrefab != null)
            {
                var boss = PrefabUtility.InstantiatePrefab(bossPrefab) as GameObject;
                boss.transform.position = new Vector3(23f, 19.5f, 0f);
            }

            // 3. Pickups (Key and Coins)
            var keyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Key_0.prefab");
            if (keyPrefab != null)
            {
                var key = PrefabUtility.InstantiatePrefab(keyPrefab) as GameObject;
                key.transform.position = new Vector3(-7f, 10.8f, 0f); // Hidden in middle ruins
            }

            var coinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Coin_0.prefab");
            if (coinPrefab != null)
            {
                for (int cx = -15; cx <= 20; cx += 5)
                {
                    var coin = PrefabUtility.InstantiatePrefab(coinPrefab) as GameObject;
                    coin.transform.position = new Vector3(cx, 1.5f, 0f);
                }
            }

            // 4. Exit Trigger Collider
            var exitCol = exitObj.AddComponent<BoxCollider2D>();
            exitCol.isTrigger = true;
            exitCol.size = new Vector2(2f, 3.5f);
            exitObj.tag = "Key"; // Interacts with Player trigger

            // 5. Camera & Cinemachine
            var camObj = GameObject.Find("Main Camera");
            if (camObj == null)
            {
                camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                var cam = camObj.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = 7.5f;
                cam.backgroundColor = new Color(0.05f, 0.04f, 0.08f);
                camObj.AddComponent<AudioListener>();
            }

            var cineBrain = camObj.GetComponent<CinemachineBrain>() ?? camObj.AddComponent<CinemachineBrain>();

            var vcamObj = GameObject.Find("CinemachineCamera");
            if (vcamObj == null)
            {
                vcamObj = new GameObject("CinemachineCamera");
                var vcam = vcamObj.AddComponent<CinemachineCamera>();
                vcam.Lens.OrthographicSize = 7.5f;
                vcamObj.AddComponent<CinemachineImpulseListener>();
                vcamObj.AddComponent<CameraShake>();

                if (playerInstance != null)
                {
                    var soVcam = new SerializedObject(vcam);
                    var trackProp = soVcam.FindProperty("Target.TrackingTarget");
                    if (trackProp != null)
                    {
                        trackProp.objectReferenceValue = playerInstance.transform;
                        soVcam.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
            }
            else
            {
                var vcam = vcamObj.GetComponent<CinemachineCamera>();
                if (vcam != null && playerInstance != null)
                {
                    var soVcam = new SerializedObject(vcam);
                    var trackProp = soVcam.FindProperty("Target.TrackingTarget");
                    if (trackProp != null)
                    {
                        trackProp.objectReferenceValue = playerInstance.transform;
                        soVcam.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
            }

            // 6. UI & GameManager
            var gmObj = GameObject.Find("GameManager");
            if (gmObj == null)
            {
                gmObj = new GameObject("GameManager");
                gmObj.AddComponent<GameManager>();
            }

            // Save Scene cleanly
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, targetScenePath);
            Debug.Log($"Scene saved to: {targetScenePath}");
        }

        private static Tilemap CreateTilemap(GameObject parent, string name, int sortingOrder, bool addCollision = false, bool isPlatformEffector = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform);
            var tm = go.AddComponent<Tilemap>();
            var tr = go.AddComponent<TilemapRenderer>();
            tr.sortingOrder = sortingOrder;

            if (addCollision)
            {
                var col = go.AddComponent<TilemapCollider2D>();
                var comp = go.AddComponent<CompositeCollider2D>();
                var rb = go.GetComponent<Rigidbody2D>();
                if (rb != null) rb.bodyType = RigidbodyType2D.Static;
                col.usedByComposite = true;

                if (isPlatformEffector)
                {
                    var eff = go.AddComponent<PlatformEffector2D>();
                    eff.useOneWay = true;
                    eff.surfaceArc = 170f;
                    comp.usedByEffector = true;
                }
            }

            return tm;
        }
    }

}
#endif
