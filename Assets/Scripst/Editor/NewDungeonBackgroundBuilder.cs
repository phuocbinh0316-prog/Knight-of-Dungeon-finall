#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Knight.Editor
{
    public static class NewDungeonBackgroundBuilder
    {
        private const string BgBaseDir = "Assets/Art/NewDungeon/Background";
        private const string Map1Path = "Assets/Scenes/map 1.unity";

        private static readonly string[] FarTextures = {
            $"{BgBaseDir}/BG_Far/bg_far_sky.png",
            $"{BgBaseDir}/BG_Far/bg_far_towers.png"
        };

        private static readonly string[] MidTextures = {
            $"{BgBaseDir}/BG_Mid/bg_mid_arches.png",
            $"{BgBaseDir}/BG_Mid/bg_mid_fortress.png"
        };

        private static readonly string[] DecTextures = {
            $"{BgBaseDir}/BG_Decoration/bg_dec_rose_window.png",
            $"{BgBaseDir}/BG_Decoration/bg_dec_lancet_purple.png",
            $"{BgBaseDir}/BG_Decoration/bg_dec_lancet_crimson.png",
            $"{BgBaseDir}/BG_Decoration/bg_dec_columns.png",
            $"{BgBaseDir}/BG_Decoration/bg_dec_banners.png",
            $"{BgBaseDir}/BG_Decoration/bg_dec_cages.png"
        };

        private static readonly string[] AtmTextures = {
            $"{BgBaseDir}/BG_Atmosphere/bg_atm_fog_layer.png",
            $"{BgBaseDir}/BG_Atmosphere/bg_atm_godrays.png",
            $"{BgBaseDir}/BG_Atmosphere/bg_atm_ground_mist.png"
        };

        [MenuItem("Knight of Dungeon/Build Dead Cells 4-Layer Background")]
        public static void Run()
        {
            Debug.Log("<color=cyan>=== [NewDungeonBackgroundBuilder] Starting 4-Layer Background Integration ===</color>");

            ConfigureImportSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            IntegrateIntoScene();

            Debug.Log("<color=green>=== [NewDungeonBackgroundBuilder] Completed Successfully! ===</color>");
        }

        public static void ConfigureImportSettings()
        {
            var allTextures = new System.Collections.Generic.List<string>();
            allTextures.AddRange(FarTextures);
            allTextures.AddRange(MidTextures);
            allTextures.AddRange(DecTextures);
            allTextures.AddRange(AtmTextures);

            foreach (var path in allTextures)
            {
                if (!File.Exists(path))
                {
                    Debug.LogWarning($"[NewDungeonBackgroundBuilder] Missing asset at {path}");
                    continue;
                }

                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) continue;

                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = 32f;
                ti.filterMode = FilterMode.Point;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.alphaIsTransparency = true;

                // Tiling textures repeat horizontally
                if (path.Contains("sky") || path.Contains("towers") || path.Contains("arches") ||
                    path.Contains("fortress") || path.Contains("fog") || path.Contains("ground_mist"))
                {
                    ti.wrapMode = TextureWrapMode.Repeat;
                }
                else
                {
                    ti.wrapMode = TextureWrapMode.Clamp;
                }

                ti.SaveAndReimport();
                Debug.Log($"[NewDungeonBackgroundBuilder] Configured TextureImporter for {path}");
            }
        }

        public static void IntegrateIntoScene()
        {
            var scene = EditorSceneManager.OpenScene(Map1Path, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[NewDungeonBackgroundBuilder] Failed to open scene at {Map1Path}");
                return;
            }

            // Find or create DeadCellsEnvironment
            var envGO = GameObject.Find("DeadCellsEnvironment");
            if (envGO == null)
            {
                envGO = new GameObject("DeadCellsEnvironment");
            }

            // Remove previous background roots if any
            var existingBgRoot = GameObject.Find("DC_Background_Root");
            if (existingBgRoot != null)
            {
                Object.DestroyImmediate(existingBgRoot);
            }

            // Clean up old problematic background objects under DeadCellsEnvironment
            var toDestroy = new System.Collections.Generic.List<GameObject>();
            for (int i = 0; i < envGO.transform.childCount; i++)
            {
                var child = envGO.transform.GetChild(i).gameObject;
                string n = child.name;
                if (n.StartsWith("DC_CathedralBackdrop_") ||
                    n.StartsWith("DC_BgCathedral_") ||
                    n.StartsWith("DC_BgFortress_") ||
                    n.StartsWith("DC_GothicArch_") ||
                    n.StartsWith("DC_StainedWindow_") ||
                    n.StartsWith("DC_Column_"))
                {
                    toDestroy.Add(child);
                }
            }
            foreach (var go in toDestroy)
            {
                Object.DestroyImmediate(go);
            }
            Debug.Log($"[NewDungeonBackgroundBuilder] Cleaned up {toDestroy.Count} old stretched/un-parallaxed background objects.");

            // Create Master DC_Background_Root
            var bgRoot = new GameObject("DC_Background_Root");
            bgRoot.transform.SetParent(envGO.transform, false);
            bgRoot.transform.position = Vector3.zero;

            // Load Sprites
            var sSky = AssetDatabase.LoadAssetAtPath<Sprite>($"{BgBaseDir}/BG_Far/bg_far_sky.png");
            var sTowers = AssetDatabase.LoadAssetAtPath<Sprite>($"{BgBaseDir}/BG_Far/bg_far_towers.png");
            var sArches = AssetDatabase.LoadAssetAtPath<Sprite>($"{BgBaseDir}/BG_Mid/bg_mid_arches.png");
            var sFortress = AssetDatabase.LoadAssetAtPath<Sprite>($"{BgBaseDir}/BG_Mid/bg_mid_fortress.png");
            var sRose = AssetDatabase.LoadAssetAtPath<Sprite>($"{BgBaseDir}/BG_Decoration/bg_dec_rose_window.png");
            var sLancetPurple = AssetDatabase.LoadAssetAtPath<Sprite>($"{BgBaseDir}/BG_Decoration/bg_dec_lancet_purple.png");
            var sLancetCrimson = AssetDatabase.LoadAssetAtPath<Sprite>($"{BgBaseDir}/BG_Decoration/bg_dec_lancet_crimson.png");
            var sColumn = AssetDatabase.LoadAssetAtPath<Sprite>($"{BgBaseDir}/BG_Decoration/bg_dec_columns.png");
            var sBanner = AssetDatabase.LoadAssetAtPath<Sprite>($"{BgBaseDir}/BG_Decoration/bg_dec_banners.png");
            var sCage = AssetDatabase.LoadAssetAtPath<Sprite>($"{BgBaseDir}/BG_Decoration/bg_dec_cages.png");
            var sFog = AssetDatabase.LoadAssetAtPath<Sprite>($"{BgBaseDir}/BG_Atmosphere/bg_atm_fog_layer.png");
            var sGodrays = AssetDatabase.LoadAssetAtPath<Sprite>($"{BgBaseDir}/BG_Atmosphere/bg_atm_godrays.png");
            var sGroundMist = AssetDatabase.LoadAssetAtPath<Sprite>($"{BgBaseDir}/BG_Atmosphere/bg_atm_ground_mist.png");

            // -------------------------------------------------------------
            // LAYER 1: FAR BACKGROUND (Sky & Distant Spires)
            // -------------------------------------------------------------
            // Far Sky (Moves at 4% relative speed -> parallax 0.96)
            var farSkyGO = new GameObject("Layer_01_Far_Sky");
            farSkyGO.transform.SetParent(bgRoot.transform, false);
            farSkyGO.transform.position = new Vector3(0, 3f, 25f);
            var pSky = farSkyGO.AddComponent<ParallaxLayer>();
            pSky.ParallaxMultiplier = new Vector2(0.96f, 0.25f);
            pSky.TextureUnitSizeX = 16f; // 512 / 32
            pSky.InfiniteHorizontal = true;

            // 5 slices of 16 units span [-32..+32]
            for (int i = -2; i <= 2; i++)
            {
                var slice = new GameObject($"Sky_Slice_{i + 2}");
                slice.transform.SetParent(farSkyGO.transform, false);
                slice.transform.localPosition = new Vector3(i * 16f, 0f, 0f);
                slice.transform.localScale = Vector3.one;
                var sr = slice.AddComponent<SpriteRenderer>();
                sr.sprite = sSky;
                sr.sortingLayerName = "Default";
                sr.sortingOrder = -60;
            }

            // Distant Towers (Moves at 10% relative speed -> parallax 0.90)
            var farTowersGO = new GameObject("Layer_02_Far_Towers");
            farTowersGO.transform.SetParent(bgRoot.transform, false);
            farTowersGO.transform.position = new Vector3(0, 2.5f, 22f);
            var pTowers = farTowersGO.AddComponent<ParallaxLayer>();
            pTowers.ParallaxMultiplier = new Vector2(0.90f, 0.20f);
            pTowers.TextureUnitSizeX = 32f; // 1024 / 32
            pTowers.InfiniteHorizontal = true;

            // 3 slices of 32 units span [-32..+32]
            for (int i = -1; i <= 1; i++)
            {
                var slice = new GameObject($"Towers_Slice_{i + 1}");
                slice.transform.SetParent(farTowersGO.transform, false);
                slice.transform.localPosition = new Vector3(i * 32f, 0f, 0f);
                slice.transform.localScale = Vector3.one;
                var sr = slice.AddComponent<SpriteRenderer>();
                sr.sprite = sTowers;
                sr.sortingLayerName = "Default";
                sr.sortingOrder = -50;
            }

            // -------------------------------------------------------------
            // LAYER 2: MID BACKGROUND (Gothic Arches & Fortress Colonnades)
            // -------------------------------------------------------------
            var midArchesGO = new GameObject("Layer_03_Mid_Arches");
            midArchesGO.transform.SetParent(bgRoot.transform, false);
            midArchesGO.transform.position = new Vector3(0, 1.2f, 16f);
            var pArches = midArchesGO.AddComponent<ParallaxLayer>();
            pArches.ParallaxMultiplier = new Vector2(0.70f, 0.15f);
            pArches.TextureUnitSizeX = 24f; // 768 / 32
            pArches.InfiniteHorizontal = true;

            // 5 slices of 24 units span [-48..+48]
            for (int i = -2; i <= 2; i++)
            {
                var slice = new GameObject($"Arch_Slice_{i + 2}");
                slice.transform.SetParent(midArchesGO.transform, false);
                slice.transform.localPosition = new Vector3(i * 24f, 0f, 0f);
                slice.transform.localScale = Vector3.one;
                var sr = slice.AddComponent<SpriteRenderer>();
                sr.sprite = sArches;
                sr.sortingLayerName = "Default";
                sr.sortingOrder = -35;
            }

            // -------------------------------------------------------------
            // LAYER 3: ATMOSPHERE BACK (Purple Volumetric Fog)
            // -------------------------------------------------------------
            var atmBackGO = new GameObject("Layer_04_Atmosphere_Back");
            atmBackGO.transform.SetParent(bgRoot.transform, false);
            atmBackGO.transform.position = new Vector3(0, 3.0f, 14f);
            var pAtmBack = atmBackGO.AddComponent<ParallaxLayer>();
            pAtmBack.ParallaxMultiplier = new Vector2(0.80f, 0.15f);
            pAtmBack.TextureUnitSizeX = 32f;
            pAtmBack.InfiniteHorizontal = true;

            for (int i = -1; i <= 1; i++)
            {
                var slice = new GameObject($"Fog_Slice_{i + 1}");
                slice.transform.SetParent(atmBackGO.transform, false);
                slice.transform.localPosition = new Vector3(i * 32f, 0f, 0f);
                slice.transform.localScale = Vector3.one;
                var sr = slice.AddComponent<SpriteRenderer>();
                sr.sprite = sFog;
                sr.sortingLayerName = "Default";
                sr.sortingOrder = -28;
            }

            // -------------------------------------------------------------
            // LAYER 4: DECORATION (Rose Windows, Lancets, Columns, Banners, Cages, Godrays)
            // -------------------------------------------------------------
            var decGO = new GameObject("Layer_05_Decoration");
            decGO.transform.SetParent(bgRoot.transform, false);
            decGO.transform.position = new Vector3(0, 0f, 10f);
            var pDec = decGO.AddComponent<ParallaxLayer>();
            pDec.ParallaxMultiplier = new Vector2(0.50f, 0.10f);
            pDec.InfiniteHorizontal = false;

            // Rose Windows (focal centerpieces in grand halls)
            float[] roseX = { 18f, 54f, 90f, 126f, 154f };
            for (int i = 0; i < roseX.Length; i++)
            {
                var rose = new GameObject($"Dec_RoseWindow_{i}");
                rose.transform.SetParent(decGO.transform, false);
                rose.transform.position = new Vector3(roseX[i], 4.2f, 10f);
                rose.transform.localScale = Vector3.one;
                var sr = rose.AddComponent<SpriteRenderer>();
                sr.sprite = sRose;
                sr.sortingLayerName = "Default";
                sr.sortingOrder = -20;

                // Volumetric God Rays angled beneath each Rose Window
                var rays = new GameObject($"Dec_Godrays_{i}");
                rays.transform.SetParent(decGO.transform, false);
                rays.transform.position = new Vector3(roseX[i] + 4.5f, 2.5f, 9.5f);
                rays.transform.localScale = Vector3.one;
                var rsr = rays.AddComponent<SpriteRenderer>();
                rsr.sprite = sGodrays;
                rsr.sortingLayerName = "Default";
                rsr.sortingOrder = -18;
            }

            // Tall Lancet Windows (purple & crimson alternating between pillars)
            float[] lancetX = { -6f, 6f, 30f, 42f, 66f, 78f, 102f, 114f, 138f, 146f, 162f };
            for (int i = 0; i < lancetX.Length; i++)
            {
                var lancet = new GameObject($"Dec_LancetWindow_{i}");
                lancet.transform.SetParent(decGO.transform, false);
                lancet.transform.position = new Vector3(lancetX[i], 2.2f, 10.5f);
                lancet.transform.localScale = Vector3.one;
                var sr = lancet.AddComponent<SpriteRenderer>();
                sr.sprite = (i % 2 == 0) ? sLancetPurple : sLancetCrimson;
                sr.sortingLayerName = "Default";
                sr.sortingOrder = -22;
            }

            // Gothic Cathedral Columns
            float[] colX = { -12f, 12f, 36f, 60f, 84f, 108f, 132f, 156f };
            for (int i = 0; i < colX.Length; i++)
            {
                var col = new GameObject($"Dec_Column_{i}");
                col.transform.SetParent(decGO.transform, false);
                col.transform.position = new Vector3(colX[i], 0.2f, 8f);
                col.transform.localScale = Vector3.one;
                var sr = col.AddComponent<SpriteRenderer>();
                sr.sprite = sColumn;
                sr.sortingLayerName = "Default";
                sr.sortingOrder = -16;
            }

            // Hanging Banners
            float[] bannerX = { 0f, 24f, 48f, 72f, 96f, 120f, 144f };
            for (int i = 0; i < bannerX.Length; i++)
            {
                var banner = new GameObject($"Dec_Banner_{i}");
                banner.transform.SetParent(decGO.transform, false);
                banner.transform.position = new Vector3(bannerX[i], 2.5f, 7.5f);
                banner.transform.localScale = Vector3.one;
                var sr = banner.AddComponent<SpriteRenderer>();
                sr.sprite = sBanner;
                sr.sortingLayerName = "Default";
                sr.sortingOrder = -14;
            }

            // Hanging Gibbet Cages
            float[] cageX = { 8f, 32f, 64f, 80f, 112f, 140f };
            for (int i = 0; i < cageX.Length; i++)
            {
                var cage = new GameObject($"Dec_Cage_{i}");
                cage.transform.SetParent(decGO.transform, false);
                cage.transform.position = new Vector3(cageX[i], 4.2f, 7.5f);
                cage.transform.localScale = Vector3.one;
                var sr = cage.AddComponent<SpriteRenderer>();
                sr.sprite = sCage;
                sr.sortingLayerName = "Default";
                sr.sortingOrder = -14;
            }

            // -------------------------------------------------------------
            // LAYER 5: ATMOSPHERE FRONT (Ground Mist along dungeon crypt floor)
            // -------------------------------------------------------------
            var atmFrontGO = new GameObject("Layer_06_Atmosphere_Front");
            atmFrontGO.transform.SetParent(bgRoot.transform, false);
            atmFrontGO.transform.position = new Vector3(0, -2.5f, 5f);
            var pAtmFront = atmFrontGO.AddComponent<ParallaxLayer>();
            pAtmFront.ParallaxMultiplier = new Vector2(0.30f, 0.05f);
            pAtmFront.TextureUnitSizeX = 32f;
            pAtmFront.InfiniteHorizontal = true;

            for (int i = -1; i <= 1; i++)
            {
                var slice = new GameObject($"GroundMist_Slice_{i + 1}");
                slice.transform.SetParent(atmFrontGO.transform, false);
                slice.transform.localPosition = new Vector3(i * 32f, 0f, 0f);
                slice.transform.localScale = Vector3.one;
                var sr = slice.AddComponent<SpriteRenderer>();
                sr.sprite = sGroundMist;
                sr.sortingLayerName = "Default";
                sr.sortingOrder = -5;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[NewDungeonBackgroundBuilder] Successfully saved scene {Map1Path} with new 4-layer parallax background!");
        }
    }
}
#endif
