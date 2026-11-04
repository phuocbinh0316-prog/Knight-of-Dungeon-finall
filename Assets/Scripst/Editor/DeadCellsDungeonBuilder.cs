#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace Knight.Editor
{
    public static class DeadCellsDungeonBuilder
    {
        private const string SourceDir = "D:/Knight2D-2334dacd35ba676431b98c387c08f972e9959f18/Asset title map";
        private const string TargetDir = "Assets/DeadCellsDungeon";
        private const string TileDir = "Assets/Tile/DeadCellsDungeon";
        private const string Map1Path = "Assets/Scenes/map 1.unity";

        private const string BgSrcPath = SourceDir + "/Background.jpg";
        private const string Sheet1SrcPath = SourceDir + "/Create_pixel_art_dungeon_tileset_325657ce.jpg";
        private const string Sheet2SrcPath = SourceDir + "/Create_pixel_art_dungeon_tileset_6b672cc3.jpg";

        private const string BgDstPath = TargetDir + "/Background.jpg";
        private const string Sheet1PngPath = TargetDir + "/dc_tileset_1.png";
        private const string Sheet2PngPath = TargetDir + "/dc_tileset_2.png";

        [MenuItem("Knight of Dungeon/Build Dead Cells Dungeon Map 1")]
        public static void Run()
        {
            Debug.Log("<color=cyan>=== [DeadCellsDungeonBuilder] Starting Dead Cells Dungeon Build ===</color>");

            try
            {
                EnsureDirectories();
                PrepareBackgroundAsset();
                ProcessTilesetTransparency(Sheet1SrcPath, Sheet1PngPath, new Color32(31, 36, 41, 255), 24, new int[] { 115, 230, 420, 575 });
                ProcessTilesetTransparency(Sheet2SrcPath, Sheet2PngPath, new Color32(18, 17, 25, 255), 24, new int[] { 93, 183, 325, 481, 631 });

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                SliceSheet1();
                SliceSheet2();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                CreateTileAssets();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                RedesignMap1();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("<color=green>=== [DeadCellsDungeonBuilder] SUCCESS! Map 1 is now a Dead Cells Dark Fantasy Dungeon! ===</color>");
            }
            catch (Exception ex)
            {
                Debug.LogError("[DeadCellsDungeonBuilder] Error during build: " + ex);
            }
        }

        private static void EnsureDirectories()
        {
            if (!AssetDatabase.IsValidFolder("Assets/DeadCellsDungeon"))
                AssetDatabase.CreateFolder("Assets", "DeadCellsDungeon");

            if (!AssetDatabase.IsValidFolder("Assets/Tile"))
                AssetDatabase.CreateFolder("Assets", "Tile");

            if (!AssetDatabase.IsValidFolder(TileDir))
                AssetDatabase.CreateFolder("Assets/Tile", "DeadCellsDungeon");
        }

        private static void PrepareBackgroundAsset()
        {
            if (!File.Exists(BgDstPath) && File.Exists(BgSrcPath))
            {
                File.Copy(BgSrcPath, BgDstPath, true);
                AssetDatabase.ImportAsset(BgDstPath, ImportAssetOptions.ForceUpdate);
            }

            var ti = AssetImporter.GetAtPath(BgDstPath) as TextureImporter;
            if (ti != null)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = 32f;
                ti.filterMode = FilterMode.Bilinear;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                EditorUtility.SetDirty(ti);
                ti.SaveAndReimport();
                Debug.Log("[DeadCellsDungeonBuilder] Configured Background.jpg texture importer");
            }
        }

        private static void ProcessTilesetTransparency(string srcJpeg, string dstPng, Color32 bgTarget, int tolerance, int[] gutters)
        {
            if (File.Exists(dstPng))
            {
                Debug.Log($"[DeadCellsDungeonBuilder] {dstPng} already exists; skipping transparency generation.");
                return;
            }

            if (!File.Exists(srcJpeg))
            {
                Debug.LogError($"[DeadCellsDungeonBuilder] Source file not found: {srcJpeg}");
                return;
            }

            byte[] bytes = File.ReadAllBytes(srcJpeg);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(bytes);

            int w = tex.width;
            int h = tex.height;
            Color32[] pixels = tex.GetPixels32();
            bool[] isBg = new bool[w * h];
            var queue = new Queue<int>();

            void TestPixel(int x, int y)
            {
                int idx = y * w + x;
                if (isBg[idx]) return;
                Color32 c = pixels[idx];
                int dist = Math.Abs(c.r - bgTarget.r) + Math.Abs(c.g - bgTarget.g) + Math.Abs(c.b - bgTarget.b);
                if (dist <= tolerance)
                {
                    isBg[idx] = true;
                    queue.Enqueue(idx);
                }
            }

            // Seed borders
            for (int x = 0; x < w; x++)
            {
                TestPixel(x, 0);
                TestPixel(x, h - 1);
            }
            for (int y = 0; y < h; y++)
            {
                TestPixel(0, y);
                TestPixel(w - 1, y);
            }

            // Seed gutters (in top-down coordinates converted to bottom-up)
            foreach (int gyTop in gutters)
            {
                int gy = (h - 1) - gyTop;
                if (gy >= 0 && gy < h)
                {
                    for (int x = 0; x < w; x++)
                    {
                        TestPixel(x, gy);
                    }
                }
            }

            // BFS Flood Fill
            while (queue.Count > 0)
            {
                int cur = queue.Dequeue();
                int cy = cur / w;
                int cx = cur % w;

                if (cx > 0) TestPixel(cx - 1, cy);
                if (cx < w - 1) TestPixel(cx + 1, cy);
                if (cy > 0) TestPixel(cx, cy - 1);
                if (cy < h - 1) TestPixel(cx, cy + 1);
            }

            for (int i = 0; i < pixels.Length; i++)
            {
                if (isBg[i])
                {
                    pixels[i] = new Color32(0, 0, 0, 0);
                }
                else
                {
                    Color32 p = pixels[i];
                    p.a = 255;
                    pixels[i] = p;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            byte[] pngBytes = tex.EncodeToPNG();
            File.WriteAllBytes(dstPng, pngBytes);
            UnityEngine.Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(dstPng, ImportAssetOptions.ForceUpdate);
            Debug.Log($"[DeadCellsDungeonBuilder] Generated transparent PNG at {dstPng}");
        }

        private static void SliceSheet1()
        {
            var ti = AssetImporter.GetAtPath(Sheet1PngPath) as TextureImporter;
            if (ti == null)
            {
                Debug.LogError($"[DeadCellsDungeonBuilder] Cannot find TextureImporter at {Sheet1PngPath}");
                return;
            }

            ti.isReadable = true;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Multiple;
            ti.spritePixelsPerUnit = 32f;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;

            int H = 768;
            Rect TopDownRect(int x, int yTop, int w, int h)
            {
                return new Rect(x, H - (yTop + h), w, h);
            }

            var metas = new List<SpriteMetaData>
            {
                // Terrain Blocks (Row 1)
                new SpriteMetaData { name = "dc1_fill_brick", rect = TopDownRect(20, 10, 80, 80), alignment = (int)SpriteAlignment.Center },
                new SpriteMetaData { name = "dc1_fill_blocks", rect = TopDownRect(140, 10, 80, 80), alignment = (int)SpriteAlignment.Center },
                new SpriteMetaData { name = "dc1_ceiling", rect = TopDownRect(360, 10, 80, 48), alignment = (int)SpriteAlignment.Center },
                new SpriteMetaData { name = "dc1_mossy_stone", rect = TopDownRect(700, 10, 80, 80), alignment = (int)SpriteAlignment.Center },
                new SpriteMetaData { name = "dc1_cracked_stone", rect = TopDownRect(805, 10, 80, 80), alignment = (int)SpriteAlignment.Center },
                new SpriteMetaData { name = "dc1_stairs", rect = TopDownRect(906, 10, 120, 95), alignment = (int)SpriteAlignment.Center },
                new SpriteMetaData { name = "dc1_platform_moss", rect = TopDownRect(1045, 10, 150, 60), alignment = (int)SpriteAlignment.Center },
                new SpriteMetaData { name = "dc1_platform_island", rect = TopDownRect(1211, 10, 140, 80), alignment = (int)SpriteAlignment.Center },

                // Platforms (Row 2)
                new SpriteMetaData { name = "dc1_surface_left", rect = TopDownRect(20, 125, 45, 55), alignment = (int)SpriteAlignment.Center },
                new SpriteMetaData { name = "dc1_surface_center", rect = TopDownRect(70, 125, 80, 55), alignment = (int)SpriteAlignment.Center },
                new SpriteMetaData { name = "dc1_surface_right", rect = TopDownRect(190, 125, 45, 55), alignment = (int)SpriteAlignment.Center },
                new SpriteMetaData { name = "dc1_surface_broken", rect = TopDownRect(356, 125, 85, 55), alignment = (int)SpriteAlignment.Center },
                new SpriteMetaData { name = "dc1_wall_left", rect = TopDownRect(570, 125, 35, 75), alignment = (int)SpriteAlignment.Center },
                new SpriteMetaData { name = "dc1_wall_right", rect = TopDownRect(460, 125, 35, 75), alignment = (int)SpriteAlignment.Center },
                new SpriteMetaData { name = "dc1_platform_ledge", rect = TopDownRect(1040, 140, 130, 35), alignment = (int)SpriteAlignment.Center },

                // Architecture (Row 3)
                new SpriteMetaData { name = "dc1_gothic_arch", rect = TopDownRect(20, 249, 138, 154), alignment = (int)SpriteAlignment.BottomCenter },
                new SpriteMetaData { name = "dc1_window_stained_red", rect = TopDownRect(171, 249, 90, 154), alignment = (int)SpriteAlignment.Center },
                new SpriteMetaData { name = "dc1_window_stained_purple", rect = TopDownRect(274, 249, 90, 154), alignment = (int)SpriteAlignment.Center },
                new SpriteMetaData { name = "dc1_window_iron_bars", rect = TopDownRect(377, 249, 81, 154), alignment = (int)SpriteAlignment.Center },
                new SpriteMetaData { name = "dc1_wooden_door", rect = TopDownRect(472, 249, 147, 154), alignment = (int)SpriteAlignment.BottomCenter },
                new SpriteMetaData { name = "dc1_iron_gate", rect = TopDownRect(631, 249, 125, 154), alignment = (int)SpriteAlignment.BottomCenter },
                new SpriteMetaData { name = "dc1_stone_altar", rect = TopDownRect(768, 249, 118, 154), alignment = (int)SpriteAlignment.BottomCenter },
                new SpriteMetaData { name = "dc1_column_1", rect = TopDownRect(899, 249, 35, 154), alignment = (int)SpriteAlignment.BottomCenter },
                new SpriteMetaData { name = "dc1_column_2", rect = TopDownRect(950, 249, 39, 154), alignment = (int)SpriteAlignment.BottomCenter },
                new SpriteMetaData { name = "dc1_column_3", rect = TopDownRect(1003, 249, 41, 154), alignment = (int)SpriteAlignment.BottomCenter },
                new SpriteMetaData { name = "dc1_column_4", rect = TopDownRect(1058, 249, 40, 154), alignment = (int)SpriteAlignment.BottomCenter },
                new SpriteMetaData { name = "dc1_railing_1", rect = TopDownRect(1112, 255, 111, 75), alignment = (int)SpriteAlignment.Center },
                new SpriteMetaData { name = "dc1_railing_broken", rect = TopDownRect(1237, 335, 118, 70), alignment = (int)SpriteAlignment.Center },

                // Props (Row 4)
                new SpriteMetaData { name = "dc1_torch_wall", rect = TopDownRect(20, 440, 75, 120), alignment = (int)SpriteAlignment.Center },
                new SpriteMetaData { name = "dc1_torch_wall_alt", rect = TopDownRect(100, 440, 75, 120), alignment = (int)SpriteAlignment.Center },
                new SpriteMetaData { name = "dc1_lantern", rect = TopDownRect(199, 445, 29, 85), alignment = (int)SpriteAlignment.TopCenter },
                new SpriteMetaData { name = "dc1_candles", rect = TopDownRect(251, 460, 37, 50), alignment = (int)SpriteAlignment.BottomCenter },
                new SpriteMetaData { name = "dc1_barrel", rect = TopDownRect(309, 475, 48, 50), alignment = (int)SpriteAlignment.BottomCenter },
                new SpriteMetaData { name = "dc1_crates_stacked", rect = TopDownRect(373, 470, 98, 55), alignment = (int)SpriteAlignment.BottomCenter },
                new SpriteMetaData { name = "dc1_chest_gold_closed", rect = TopDownRect(486, 475, 64, 50), alignment = (int)SpriteAlignment.BottomCenter },
                new SpriteMetaData { name = "dc1_chest_iron_closed", rect = TopDownRect(565, 475, 66, 50), alignment = (int)SpriteAlignment.BottomCenter },
                new SpriteMetaData { name = "dc1_chest_open_loot", rect = TopDownRect(642, 470, 75, 55), alignment = (int)SpriteAlignment.BottomCenter },
                new SpriteMetaData { name = "dc1_skull_bones", rect = TopDownRect(720, 480, 62, 45), alignment = (int)SpriteAlignment.BottomCenter },
                new SpriteMetaData { name = "dc1_shields", rect = TopDownRect(745, 480, 45, 45), alignment = (int)SpriteAlignment.BottomCenter },
                new SpriteMetaData { name = "dc1_banner_crimson", rect = TopDownRect(794, 440, 48, 115), alignment = (int)SpriteAlignment.TopCenter },
                new SpriteMetaData { name = "dc1_banner_purple", rect = TopDownRect(858, 440, 45, 115), alignment = (int)SpriteAlignment.TopCenter },
                new SpriteMetaData { name = "dc1_banner_blue", rect = TopDownRect(917, 440, 48, 115), alignment = (int)SpriteAlignment.TopCenter },
                new SpriteMetaData { name = "dc1_banner_tattered", rect = TopDownRect(978, 440, 48, 115), alignment = (int)SpriteAlignment.TopCenter },
                new SpriteMetaData { name = "dc1_cage_hanging", rect = TopDownRect(1040, 440, 45, 115), alignment = (int)SpriteAlignment.TopCenter },
                new SpriteMetaData { name = "dc1_chains_hooks", rect = TopDownRect(1150, 440, 27, 115), alignment = (int)SpriteAlignment.TopCenter },
                new SpriteMetaData { name = "dc1_rubble_pile", rect = TopDownRect(1239, 475, 114, 50), alignment = (int)SpriteAlignment.BottomCenter },

                // Background Silhouettes (Row 5)
                new SpriteMetaData { name = "dc1_bg_ruined_tower", rect = TopDownRect(30, 585, 214, 174), alignment = (int)SpriteAlignment.BottomCenter },
                new SpriteMetaData { name = "dc1_bg_cathedral", rect = TopDownRect(282, 585, 200, 174), alignment = (int)SpriteAlignment.BottomCenter },
                new SpriteMetaData { name = "dc1_bg_arch_left", rect = TopDownRect(523, 585, 95, 174), alignment = (int)SpriteAlignment.BottomCenter },
                new SpriteMetaData { name = "dc1_bg_arch_right", rect = TopDownRect(641, 585, 95, 174), alignment = (int)SpriteAlignment.BottomCenter },
                new SpriteMetaData { name = "dc1_bg_pillars", rect = TopDownRect(768, 585, 248, 174), alignment = (int)SpriteAlignment.BottomCenter },
                new SpriteMetaData { name = "dc1_bg_fortress", rect = TopDownRect(1037, 585, 303, 174), alignment = (int)SpriteAlignment.BottomCenter }
            };

#pragma warning disable CS0618
            ti.spritesheet = metas.ToArray();
#pragma warning restore CS0618
            EditorUtility.SetDirty(ti);
            ti.SaveAndReimport();
            Debug.Log($"[DeadCellsDungeonBuilder] Sliced {metas.Count} sprites from dc_tileset_1.png");
        }

        private static void SliceSheet2()
        {
            var ti = AssetImporter.GetAtPath(Sheet2PngPath) as TextureImporter;
            if (ti == null)
            {
                Debug.LogError($"[DeadCellsDungeonBuilder] Cannot find TextureImporter at {Sheet2PngPath}");
                return;
            }

            ti.isReadable = true;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Multiple;
            ti.spritePixelsPerUnit = 32f;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;

            int H = 768;
            Rect TopDownRect(int x, int yTop, int w, int h)
            {
                return new Rect(x, H - (yTop + h), w, h);
            }

            var metas = new List<SpriteMetaData>
            {
                new SpriteMetaData { name = "dc2_statue_hooded", rect = TopDownRect(745, 490, 65, 130), alignment = (int)SpriteAlignment.BottomCenter },
                new SpriteMetaData { name = "dc2_crystal_cyan", rect = TopDownRect(810, 520, 30, 70), alignment = (int)SpriteAlignment.BottomCenter },
                new SpriteMetaData { name = "dc2_crystal_purple", rect = TopDownRect(840, 520, 35, 70), alignment = (int)SpriteAlignment.BottomCenter },
                new SpriteMetaData { name = "dc2_runes_glowing", rect = TopDownRect(810, 580, 55, 45), alignment = (int)SpriteAlignment.Center },
                new SpriteMetaData { name = "dc2_thorny_vines", rect = TopDownRect(935, 520, 60, 60), alignment = (int)SpriteAlignment.Center },
                new SpriteMetaData { name = "dc2_bg_skyline", rect = TopDownRect(15, 640, 240, 115), alignment = (int)SpriteAlignment.BottomCenter },
                new SpriteMetaData { name = "dc2_bg_triple_arch", rect = TopDownRect(260, 640, 220, 115), alignment = (int)SpriteAlignment.BottomCenter },
                new SpriteMetaData { name = "dc2_bg_colonnade_vines", rect = TopDownRect(500, 640, 240, 115), alignment = (int)SpriteAlignment.BottomCenter }
            };

#pragma warning disable CS0618
            ti.spritesheet = metas.ToArray();
#pragma warning restore CS0618
            EditorUtility.SetDirty(ti);
            ti.SaveAndReimport();
            Debug.Log($"[DeadCellsDungeonBuilder] Sliced {metas.Count} sprites from dc_tileset_2.png");
        }

        private static void CreateTileAssets()
        {
            var sheet1Assets = AssetDatabase.LoadAllAssetsAtPath(Sheet1PngPath);
            var spriteMap = new Dictionary<string, Sprite>();
            foreach (var a in sheet1Assets)
            {
                if (a is Sprite s)
                    spriteMap[s.name] = s;
            }

            GetOrCreateTile(spriteMap.GetValueOrDefault("dc1_surface_center"), "dc_surface");
            GetOrCreateTile(spriteMap.GetValueOrDefault("dc1_fill_brick"), "dc_fill");
            GetOrCreateTile(spriteMap.GetValueOrDefault("dc1_wall_left"), "dc_wall_left");
            GetOrCreateTile(spriteMap.GetValueOrDefault("dc1_wall_right"), "dc_wall_right");
            GetOrCreateTile(spriteMap.GetValueOrDefault("dc1_ceiling"), "dc_ceiling");
            GetOrCreateTile(spriteMap.GetValueOrDefault("dc1_platform_ledge"), "dc_platform");
            GetOrCreateTile(spriteMap.GetValueOrDefault("dc1_cracked_stone"), "dc_cracked");
            GetOrCreateTile(spriteMap.GetValueOrDefault("dc1_mossy_stone"), "dc_mossy");

            Debug.Log("[DeadCellsDungeonBuilder] Created and verified all Dead Cells Tile assets");
        }

        private static Tile GetOrCreateTile(Sprite sprite, string name)
        {
            if (sprite == null) return null;
            string path = $"{TileDir}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (existing != null)
            {
                existing.sprite = sprite;
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.colliderType = Tile.ColliderType.Grid;
            AssetDatabase.CreateAsset(tile, path);
            return tile;
        }

        private static void RedesignMap1()
        {
            var scene = EditorSceneManager.OpenScene(Map1Path, OpenSceneMode.Single);
            EditorSceneManager.SetActiveScene(scene);

            // Load Tile assets
            var surfaceTile = AssetDatabase.LoadAssetAtPath<Tile>($"{TileDir}/dc_surface.asset");
            var fillTile = AssetDatabase.LoadAssetAtPath<Tile>($"{TileDir}/dc_fill.asset");
            var wallLeftTile = AssetDatabase.LoadAssetAtPath<Tile>($"{TileDir}/dc_wall_left.asset");
            var wallRightTile = AssetDatabase.LoadAssetAtPath<Tile>($"{TileDir}/dc_wall_right.asset");
            var ceilingTile = AssetDatabase.LoadAssetAtPath<Tile>($"{TileDir}/dc_ceiling.asset");
            var platformTile = AssetDatabase.LoadAssetAtPath<Tile>($"{TileDir}/dc_platform.asset");
            var crackedTile = AssetDatabase.LoadAssetAtPath<Tile>($"{TileDir}/dc_cracked.asset");
            var mossyTile = AssetDatabase.LoadAssetAtPath<Tile>($"{TileDir}/dc_mossy.asset");

            if (surfaceTile == null || fillTile == null)
            {
                Debug.LogError("[DeadCellsDungeonBuilder] CRITICAL: Required Dead Cells tiles not found! Aborting redesign.");
                return;
            }

            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(surfaceTile, out string g, out long id);
            Debug.Log($"[DeadCellsDungeonBuilder] surfaceTile persistent GUID: {g}, id: {id}");
            if (string.IsNullOrEmpty(g))
            {
                Debug.LogError("[DeadCellsDungeonBuilder] Tile assets not yet committed in AssetDatabase! Need second pass to serialize cleanly.");
                return;
            }

            // Clean up any previous environment containers
            var oldDungeonEnv = GameObject.Find("DungeonEnvironment");
            if (oldDungeonEnv != null) UnityEngine.Object.DestroyImmediate(oldDungeonEnv);

            var oldDCEnv = GameObject.Find("DeadCellsEnvironment");
            if (oldDCEnv != null) UnityEngine.Object.DestroyImmediate(oldDCEnv);

            var envGO = new GameObject("DeadCellsEnvironment");
            envGO.transform.position = Vector3.zero;

            // 1. Far Backdrop Panorama (Background.jpg)
            var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BgDstPath);
            if (bgSprite != null)
            {
                // Create panoramic slices covering from x=-30 to x=185
                float[] bgXPositions = new float[] { 20f, 105f };
                for (int b = 0; b < bgXPositions.Length; b++)
                {
                    var bgGO = new GameObject($"DC_CathedralBackdrop_{b}");
                    bgGO.transform.SetParent(envGO.transform, false);
                    bgGO.transform.position = new Vector3(bgXPositions[b], 3.5f, 25f);
                    bgGO.transform.localScale = new Vector3(1.15f, 1.15f, 1f);

                    var sr = bgGO.AddComponent<SpriteRenderer>();
                    sr.sprite = bgSprite;
                    sr.sortingLayerName = "Background";
                    sr.sortingOrder = -50;
                    sr.color = new Color(0.85f, 0.80f, 0.95f, 1f);
                }
            }

            // Load sliced sprites from Sheet 1 & 2
            var s1Sprites = new Dictionary<string, Sprite>();
            foreach (var a in AssetDatabase.LoadAllAssetsAtPath(Sheet1PngPath))
            {
                if (a is Sprite s) s1Sprites[s.name] = s;
            }

            var s2Sprites = new Dictionary<string, Sprite>();
            foreach (var a in AssetDatabase.LoadAllAssetsAtPath(Sheet2PngPath))
            {
                if (a is Sprite s) s2Sprites[s.name] = s;
            }

            // 2. Midground Silhouettes (Cathedrals, Ruined Towers, Fortress Battlements)
            float[] cathedralX = new float[] { -5f, 50f, 115f, 160f };
            for (int i = 0; i < cathedralX.Length; i++)
            {
                var catGO = new GameObject($"DC_BgCathedral_{i}");
                catGO.transform.SetParent(envGO.transform, false);
                catGO.transform.position = new Vector3(cathedralX[i], 3.0f, 18f);
                catGO.transform.localScale = new Vector3(1.4f, 1.4f, 1f);

                var sr = catGO.AddComponent<SpriteRenderer>();
                sr.sprite = s1Sprites.GetValueOrDefault("dc1_bg_cathedral");
                sr.sortingLayerName = "Background";
                sr.sortingOrder = -35;
                sr.color = new Color(0.55f, 0.50f, 0.70f, 0.85f);
            }

            float[] fortressX = new float[] { 18f, 75f, 138f };
            for (int i = 0; i < fortressX.Length; i++)
            {
                var fortGO = new GameObject($"DC_BgFortress_{i}");
                fortGO.transform.SetParent(envGO.transform, false);
                fortGO.transform.position = new Vector3(fortressX[i], 1.5f, 15f);
                fortGO.transform.localScale = new Vector3(1.3f, 1.3f, 1f);

                var sr = fortGO.AddComponent<SpriteRenderer>();
                sr.sprite = s1Sprites.GetValueOrDefault("dc1_bg_fortress");
                sr.sortingLayerName = "Background";
                sr.sortingOrder = -30;
                sr.color = new Color(0.65f, 0.60f, 0.80f, 0.90f);
            }

            // 3. Gothic Arches & Stained Glass Windows
            float[] archX = new float[] { -8f, 12f, 32f, 52f, 72f, 92f, 112f, 132f, 152f };
            for (int i = 0; i < archX.Length; i++)
            {
                float x = archX[i];
                var archGO = new GameObject($"DC_GothicArch_{i}");
                archGO.transform.SetParent(envGO.transform, false);
                archGO.transform.position = new Vector3(x, 0.0f, 10f);
                archGO.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

                var sr = archGO.AddComponent<SpriteRenderer>();
                sr.sprite = s1Sprites.GetValueOrDefault("dc1_gothic_arch");
                sr.sortingLayerName = "Background";
                sr.sortingOrder = -20;
                sr.color = new Color(0.85f, 0.82f, 0.95f, 1f);

                // Alternating Cathedral Stained Glass Windows in the background
                string winKey = (i % 3 == 0) ? "dc1_window_stained_red" : (i % 3 == 1) ? "dc1_window_stained_purple" : "dc1_window_iron_bars";
                var winSprite = s1Sprites.GetValueOrDefault(winKey);
                if (winSprite != null)
                {
                    var winGO = new GameObject($"DC_StainedWindow_{i}");
                    winGO.transform.SetParent(envGO.transform, false);
                    winGO.transform.position = new Vector3(x, 2.2f, 11f);
                    winGO.transform.localScale = new Vector3(1.1f, 1.1f, 1f);

                    var wsr = winGO.AddComponent<SpriteRenderer>();
                    wsr.sprite = winSprite;
                    wsr.sortingLayerName = "Background";
                    wsr.sortingOrder = -22;
                    wsr.color = new Color(0.95f, 0.95f, 1f, 1f);
                }
            }

            // 4. Gothic Columns & Stone Balustrades
            float[] colX = new float[] { -12f, 2f, 22f, 42f, 62f, 82f, 102f, 122f, 142f, 160f };
            for (int i = 0; i < colX.Length; i++)
            {
                float x = colX[i];
                string colKey = (i % 2 == 0) ? "dc1_column_2" : "dc1_column_3";
                var colSprite = s1Sprites.GetValueOrDefault(colKey);
                if (colSprite != null)
                {
                    var colGO = new GameObject($"DC_Column_{i}");
                    colGO.transform.SetParent(envGO.transform, false);
                    colGO.transform.position = new Vector3(x, 0.2f, 2f);
                    colGO.transform.localScale = new Vector3(1.1f, 1.1f, 1f);

                    var sr = colGO.AddComponent<SpriteRenderer>();
                    sr.sprite = colSprite;
                    sr.sortingLayerName = "Ground";
                    sr.sortingOrder = -1;
                    sr.color = new Color(0.92f, 0.90f, 0.98f, 1f);
                }
            }

            // 5. Wooden Dungeon Doors & Portcullis Gates
            var gateGO = new GameObject("DC_BossGate");
            gateGO.transform.SetParent(envGO.transform, false);
            gateGO.transform.position = new Vector3(147.5f, 6.5f, 0.5f);
            var gateSr = gateGO.AddComponent<SpriteRenderer>();
            gateSr.sprite = s1Sprites.GetValueOrDefault("dc1_iron_gate");
            gateSr.sortingLayerName = "Ground";
            gateSr.sortingOrder = 0;

            var doorGO = new GameObject("DC_EntranceDoor");
            doorGO.transform.SetParent(envGO.transform, false);
            doorGO.transform.position = new Vector3(-13.0f, -1.8f, 0.5f);
            var doorSr = doorGO.AddComponent<SpriteRenderer>();
            doorSr.sprite = s1Sprites.GetValueOrDefault("dc1_wooden_door");
            doorSr.sortingLayerName = "Ground";
            doorSr.sortingOrder = 0;

            // 6. Burning Wall Torches with 2D Point Lights (Dead Cells signature lighting)
            var torchSprite = s1Sprites.GetValueOrDefault("dc1_torch_wall");
            float[] torchX = new float[] { -9f, 6f, 20f, 36f, 50f, 66f, 80f, 96f, 110f, 126f, 138f, 146f, 154f, 162f };
            float[] torchY = new float[] { 0.2f, 0.2f, -1.5f, 1.5f, -0.5f, 1.2f, -2.0f, 0.5f, 2.0f, 0.0f, 3.5f, 7.5f, 11.5f, 12.0f };

            for (int i = 0; i < torchX.Length; i++)
            {
                float x = torchX[i];
                float y = (i < torchY.Length) ? torchY[i] : 1.0f;

                var torchGO = new GameObject($"DC_WallTorch_{i}");
                torchGO.transform.SetParent(envGO.transform, false);
                torchGO.transform.position = new Vector3(x, y, 0f);

                if (torchSprite != null)
                {
                    var sr = torchGO.AddComponent<SpriteRenderer>();
                    sr.sprite = torchSprite;
                    sr.sortingLayerName = "Player";
                    sr.sortingOrder = -1;
                }

                var light = torchGO.AddComponent<Light2D>();
                light.lightType = Light2D.LightType.Point;
                light.color = new Color(1.0f, 0.65f, 0.26f, 1f);
                light.intensity = 1.45f;
                light.pointLightInnerRadius = 0.8f;
                light.pointLightOuterRadius = 7.5f;
            }

            // 7. Gothic Banners, Chains, Barrels, Crates, Chests, and Skeletons
            float[] bannerX = new float[] { -5f, 28f, 58f, 88f, 118f, 150f };
            string[] bannerKeys = new string[] { "dc1_banner_crimson", "dc1_banner_purple", "dc1_banner_blue", "dc1_banner_crimson", "dc1_banner_tattered", "dc1_banner_crimson" };
            for (int i = 0; i < bannerX.Length; i++)
            {
                var banGO = new GameObject($"DC_Banner_{i}");
                banGO.transform.SetParent(envGO.transform, false);
                banGO.transform.position = new Vector3(bannerX[i], 2.2f, 1f);

                var sr = banGO.AddComponent<SpriteRenderer>();
                sr.sprite = s1Sprites.GetValueOrDefault(bannerKeys[i % bannerKeys.Length]);
                sr.sortingLayerName = "Ground";
                sr.sortingOrder = 0;
            }

            // Hanging Cages & Chains
            float[] cageX = new float[] { 14f, 44f, 78f, 104f, 134f };
            for (int i = 0; i < cageX.Length; i++)
            {
                var cageGO = new GameObject($"DC_HangingCage_{i}");
                cageGO.transform.SetParent(envGO.transform, false);
                cageGO.transform.position = new Vector3(cageX[i], 4.5f, 1f);

                var sr = cageGO.AddComponent<SpriteRenderer>();
                sr.sprite = s1Sprites.GetValueOrDefault("dc1_cage_hanging");
                sr.sortingLayerName = "Ground";
                sr.sortingOrder = 0;
            }

            // Wooden Crates and Barrels near corners
            float[] crateX = new float[] { -7f, 16f, 48f, 84f, 114f, 142f };
            for (int i = 0; i < crateX.Length; i++)
            {
                var crateGO = new GameObject($"DC_Crates_{i}");
                crateGO.transform.SetParent(envGO.transform, false);
                crateGO.transform.position = new Vector3(crateX[i], -1.8f, 0f);

                var sr = crateGO.AddComponent<SpriteRenderer>();
                sr.sprite = s1Sprites.GetValueOrDefault("dc1_crates_stacked");
                sr.sortingLayerName = "Ground";
                sr.sortingOrder = 1;
            }

            // Treasure Chests
            float[] chestX = new float[] { 24f, 68f, 124f };
            for (int i = 0; i < chestX.Length; i++)
            {
                var chestGO = new GameObject($"DC_Chest_{i}");
                chestGO.transform.SetParent(envGO.transform, false);
                chestGO.transform.position = new Vector3(chestX[i], -0.2f, 0f);

                var sr = chestGO.AddComponent<SpriteRenderer>();
                sr.sprite = s1Sprites.GetValueOrDefault(i == 0 ? "dc1_chest_gold_closed" : "dc1_chest_open_loot");
                sr.sortingLayerName = "Ground";
                sr.sortingOrder = 1;
            }

            // Skeletons, Bones & Shields
            float[] skelX = new float[] { 34f, 74f, 116f, 153f };
            for (int i = 0; i < skelX.Length; i++)
            {
                var skelGO = new GameObject($"DC_Skeleton_{i}");
                skelGO.transform.SetParent(envGO.transform, false);
                skelGO.transform.position = new Vector3(skelX[i], -1.8f, 0f);

                var sr = skelGO.AddComponent<SpriteRenderer>();
                sr.sprite = s1Sprites.GetValueOrDefault("dc1_skull_bones");
                sr.sortingLayerName = "Ground";
                sr.sortingOrder = 1;
            }

            // 8. Magical Crystals, Ancient Runes, and Hooded Statue from Sheet 2
            var statueGO = new GameObject("DC_GothicStatue");
            statueGO.transform.SetParent(envGO.transform, false);
            statueGO.transform.position = new Vector3(64.0f, 1.2f, 0.5f);
            var statueSr = statueGO.AddComponent<SpriteRenderer>();
            statueSr.sprite = s2Sprites.GetValueOrDefault("dc2_statue_hooded");
            statueSr.sortingLayerName = "Ground";
            statueSr.sortingOrder = 1;

            // Glowing Crystals & Runes near Angel platform & Boss gate
            float[] crystalX = new float[] { 40f, 98f, 146f, 164f };
            float[] crystalY = new float[] { 0.5f, 2.0f, 7.0f, 11.5f };
            for (int i = 0; i < crystalX.Length; i++)
            {
                var cryGO = new GameObject($"DC_MagicCrystal_{i}");
                cryGO.transform.SetParent(envGO.transform, false);
                cryGO.transform.position = new Vector3(crystalX[i], crystalY[i], 0f);

                var sr = cryGO.AddComponent<SpriteRenderer>();
                sr.sprite = s2Sprites.GetValueOrDefault(i % 2 == 0 ? "dc2_crystal_cyan" : "dc2_crystal_purple");
                sr.sortingLayerName = "Ground";
                sr.sortingOrder = 1;

                var cLight = cryGO.AddComponent<Light2D>();
                cLight.lightType = Light2D.LightType.Point;
                cLight.color = (i % 2 == 0) ? new Color(0.25f, 0.85f, 1f, 1f) : new Color(0.85f, 0.30f, 1f, 1f);
                cLight.intensity = 1.2f;
                cLight.pointLightInnerRadius = 0.5f;
                cLight.pointLightOuterRadius = 4.5f;
            }

            var runesGO = new GameObject("DC_AncientRunes");
            runesGO.transform.SetParent(envGO.transform, false);
            runesGO.transform.position = new Vector3(145.0f, 6.2f, 0f);
            var runesSr = runesGO.AddComponent<SpriteRenderer>();
            runesSr.sprite = s2Sprites.GetValueOrDefault("dc2_runes_glowing");
            runesSr.sortingLayerName = "Ground";
            runesSr.sortingOrder = 1;

            // 9. Retile Ground Tilemap with Dead Cells Terrain Tiles
            var groundGO = GameObject.Find("Grid/Ground") ?? GameObject.Find("Ground");
            if (groundGO != null)
            {
                var tm = groundGO.GetComponent<Tilemap>();
                if (tm != null)
                {
                    tm.CompressBounds();
                    BoundsInt bounds = tm.cellBounds;
                    var positionsToUpdate = new List<Vector3Int>();

                    int minX = Mathf.Min(bounds.xMin, -30);
                    int maxX = Mathf.Max(bounds.xMax, 185);
                    int minY = Mathf.Min(bounds.yMin, -25);
                    int maxY = Mathf.Max(bounds.yMax, 30);

                    for (int x = minX; x <= maxX; x++)
                    {
                        for (int y = minY; y <= maxY; y++)
                        {
                            var pos = new Vector3Int(x, y, 0);
                            if (tm.HasTile(pos))
                            {
                                positionsToUpdate.Add(pos);
                            }
                        }
                    }

                    var posArray = new List<Vector3Int>();
                    var tileArray = new List<TileBase>();

                    for (int i = 0; i < positionsToUpdate.Count; i++)
                    {
                        var pos = positionsToUpdate[i];
                        bool hasAbove = tm.HasTile(pos + Vector3Int.up);
                        bool hasBelow = tm.HasTile(pos + Vector3Int.down);
                        bool hasLeft = tm.HasTile(pos + Vector3Int.left);
                        bool hasRight = tm.HasTile(pos + Vector3Int.right);

                        Tile chosen;
                        if (!hasAbove && !hasBelow)
                        {
                            chosen = platformTile ?? surfaceTile;
                        }
                        else if (!hasAbove)
                        {
                            if (!hasLeft && wallLeftTile != null)
                                chosen = wallLeftTile;
                            else if (!hasRight && wallRightTile != null)
                                chosen = wallRightTile;
                            else
                            {
                                // Natural stone surface variations (weathered, cracked, mossy)
                                int hash = Math.Abs((pos.x * 73856093) ^ (pos.y * 19349663));
                                if (hash % 7 == 0 && mossyTile != null)
                                    chosen = mossyTile;
                                else if (hash % 11 == 0 && crackedTile != null)
                                    chosen = crackedTile;
                                else
                                    chosen = surfaceTile;
                            }
                        }
                        else if (!hasBelow)
                        {
                            chosen = ceilingTile ?? fillTile;
                        }
                        else
                        {
                            if (!hasLeft && wallLeftTile != null)
                                chosen = wallLeftTile;
                            else if (!hasRight && wallRightTile != null)
                                chosen = wallRightTile;
                            else
                            {
                                int hash = Math.Abs((pos.x * 37) ^ (pos.y * 101));
                                if (hash % 9 == 0 && crackedTile != null)
                                    chosen = crackedTile;
                                else
                                    chosen = fillTile;
                            }
                        }

                        posArray.Add(pos);
                        tileArray.Add(chosen);
                    }

                    // Seal left dungeon entrance boundary
                    for (int y = -12; y <= 8; y++)
                    {
                        posArray.Add(new Vector3Int(-15, y, 0));
                        tileArray.Add(fillTile);

                        posArray.Add(new Vector3Int(-14, y, 0));
                        tileArray.Add(wallRightTile ?? fillTile);
                    }

                    // Seal boss arena right wall
                    for (int y = 6; y <= 18; y++)
                    {
                        posArray.Add(new Vector3Int(168, y, 0));
                        tileArray.Add(wallLeftTile ?? fillTile);

                        posArray.Add(new Vector3Int(169, y, 0));
                        tileArray.Add(fillTile);
                    }

                    tm.SetTiles(posArray.ToArray(), tileArray.ToArray());
                    tm.RefreshAllTiles();
                    EditorUtility.SetDirty(tm);
                    EditorUtility.SetDirty(groundGO);
                    Debug.Log($"[DeadCellsDungeonBuilder] Retiled Ground tilemap with {posArray.Count} Dead Cells dungeon tiles");
                }
            }

            // 10. Global Lighting & Camera Clear Color
            var globalLightGO = GameObject.Find("Global Light 2D");
            if (globalLightGO != null)
            {
                var gl = globalLightGO.GetComponent<Light2D>();
                if (gl != null)
                {
                    gl.color = new Color(0.22f, 0.20f, 0.32f, 1f);
                    gl.intensity = 0.70f;
                    EditorUtility.SetDirty(gl);
                    Debug.Log("[DeadCellsDungeonBuilder] Configured Global Light 2D to Dead Cells dark fantasy violet-indigo ambience");
                }
            }

            var mainCamGO = GameObject.Find("Main Camera");
            if (mainCamGO != null)
            {
                var cam = mainCamGO.GetComponent<Camera>();
                if (cam != null)
                {
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = new Color(0.05f, 0.04f, 0.08f, 1f);
                    EditorUtility.SetDirty(cam);
                    Debug.Log("[DeadCellsDungeonBuilder] Configured Main Camera clear color to deep gothic void");
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene);
            EditorSceneManager.SaveOpenScenes();
            Debug.Log($"[DeadCellsDungeonBuilder] Successfully redesigned map 1.unity! Save result: {saved}");
        }
    }
}
#endif
