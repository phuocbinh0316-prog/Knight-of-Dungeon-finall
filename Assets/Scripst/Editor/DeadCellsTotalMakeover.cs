#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace Knight.Editor
{
    public static class DeadCellsTotalMakeover
    {
        private const string Map1Path = "Assets/Scenes/map 1.unity";
        private const string TileDir = "Assets/Tile/DeadCellsDungeon";
        private const string BgBaseDir = "Assets/Art/NewDungeon/Background";
        private const string Sheet1PngPath = "Assets/DeadCellsDungeon/dc_tileset_1.png";
        private const string Sheet2PngPath = "Assets/DeadCellsDungeon/dc_tileset_2.png";

        [MenuItem("Knight of Dungeon/Dead Cells Total Makeover")]
        public static void Run()
        {
            Debug.Log("<color=cyan>=== [DeadCellsTotalMakeover] Transforming map 1 to 100% Dead Cells Gothic Cathedral ===</color>");

            // 1. Reimport grand full-height background assets
            ReimportGrandAssets();

            var scene = EditorSceneManager.OpenScene(Map1Path, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"Cannot open {Map1Path}");
                return;
            }

            // 2. ELIMINATE GREEN OUTDOOR TREES (Fround Tilemap)
            // Fround contains MagicCliffs outdoor oak trees that completely ruin the dark dungeon aesthetic!
            var froundGO = GameObject.Find("Fround") ?? GameObject.Find("Grid/Fround");
            if (froundGO != null)
            {
                var tmFround = froundGO.GetComponent<Tilemap>();
                if (tmFround != null) tmFround.ClearAllTiles();
                UnityEngine.Object.DestroyImmediate(froundGO);
                Debug.Log("[DeadCellsTotalMakeover] Removed old Fround tilemap (bright green outdoor trees eliminated).");
            }

            // 3. Fix Moving Platforms (Reskin Angel Wings to Dark Gothic Stone)
            var platformRoot = GameObject.Find("platform");
            if (platformRoot != null)
            {
                var stonePlatformSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{BgBaseDir}/BG_Mid/bg_mid_fortress.png");
                var srs = platformRoot.GetComponentsInChildren<SpriteRenderer>(true);
                foreach (var sr in srs)
                {
                    sr.color = new Color(0.35f, 0.28f, 0.45f, 1f); // Dark gothic purple stone tint
                }
            }

            // 4. Configure Camera & Deep Twilight Gothic Lighting
            var mainCamGO = GameObject.Find("Main Camera");
            if (mainCamGO != null)
            {
                var cam = mainCamGO.GetComponent<Camera>();
                if (cam != null)
                {
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = new Color(0.04f, 0.03f, 0.08f, 1f); // Deep gothic night void
                }
            }

            var globalLightGO = GameObject.Find("Global Light 2D");
            if (globalLightGO != null)
            {
                var gl = globalLightGO.GetComponent<Light2D>();
                if (gl != null)
                {
                    gl.color = new Color(0.13f, 0.10f, 0.24f, 1f); // Deep twilight indigo
                    gl.intensity = 0.65f;
                }
            }

            // 5. Load Dead Cells Tiles
            var surfaceTile = AssetDatabase.LoadAssetAtPath<Tile>($"{TileDir}/dc_surface.asset");
            var fillTile = AssetDatabase.LoadAssetAtPath<Tile>($"{TileDir}/dc_fill.asset");
            var wallLeftTile = AssetDatabase.LoadAssetAtPath<Tile>($"{TileDir}/dc_wall_left.asset");
            var wallRightTile = AssetDatabase.LoadAssetAtPath<Tile>($"{TileDir}/dc_wall_right.asset");
            var ceilingTile = AssetDatabase.LoadAssetAtPath<Tile>($"{TileDir}/dc_ceiling.asset");
            var platformTile = AssetDatabase.LoadAssetAtPath<Tile>($"{TileDir}/dc_platform.asset");
            var crackedTile = AssetDatabase.LoadAssetAtPath<Tile>($"{TileDir}/dc_cracked.asset");
            var mossyTile = AssetDatabase.LoadAssetAtPath<Tile>($"{TileDir}/dc_mossy.asset");

            // 6. Rebuild Ground Tilemap with Massive Deep Crypt Foundations & Multi-Tier Balconies
            var groundGO = GameObject.Find("Grid/Ground") ?? GameObject.Find("Ground");
            if (groundGO != null)
            {
                var tm = groundGO.GetComponent<Tilemap>();
                if (tm != null)
                {
                    tm.ClearAllTiles();

                    var occupied = new HashSet<Vector2Int>();

                    void AddBlock(int x0, int x1, int y0, int y1)
                    {
                        for (int x = x0; x <= x1; x++)
                            for (int y = y0; y <= y1; y++)
                                occupied.Add(new Vector2Int(x, y));
                    }

                    void AddPlatform(int x0, int x1, int y)
                    {
                        for (int x = x0; x <= x1; x++)
                            occupied.Add(new Vector2Int(x, y));
                    }

                    void AddPillarSupport(int x, int yBottom, int yTop)
                    {
                        for (int y = yBottom; y <= yTop; y++)
                            occupied.Add(new Vector2Int(x, y));
                    }

                    // --- ENCLOSED BOUNDARIES & FOUNDATIONS ---
                    // Deep foundation extends from y = -8 to y = -2 across rooms
                    // West dungeon entrance back wall (enclosed chamber feel)
                    AddBlock(-18, -16, -8, 8);

                    // Room 1: Crypt Entrance (x: -16 to 10)
                    AddBlock(-16, 10, -8, -2);
                    AddPlatform(-6, 2, 1);
                    AddPillarSupport(-4, -1, 0);
                    AddPillarSupport(0, -1, 0);
                    // Stone stairs
                    occupied.Add(new Vector2Int(3, -1));
                    occupied.Add(new Vector2Int(4, 0));
                    occupied.Add(new Vector2Int(5, 1));

                    // (Pit 1: x = 11 to 13)

                    // Room 2: Grand Cathedral Nave (x: 14 to 46)
                    AddBlock(14, 46, -8, -2);
                    // Mid-tier walkway
                    AddPlatform(19, 33, 2);
                    AddPillarSupport(21, -1, 1);
                    AddPillarSupport(31, -1, 1);
                    // Floating arch bridge
                    AddPlatform(36, 44, 3);
                    // Upper cathedral high balcony
                    AddPlatform(23, 40, 6);
                    AddPillarSupport(25, 3, 5);
                    AddPillarSupport(38, 3, 5);
                    // Ascending stone stairs
                    occupied.Add(new Vector2Int(34, 3));
                    occupied.Add(new Vector2Int(35, 4));
                    occupied.Add(new Vector2Int(36, 5));

                    // (Pit 2: x = 47 to 51)

                    // Room 3: Crypt Colonnade & High Shrine (x: 52 to 88)
                    AddBlock(52, 88, -8, -2);
                    AddPlatform(57, 69, 2);
                    AddPillarSupport(59, -1, 1);
                    AddPillarSupport(67, -1, 1);
                    AddPlatform(73, 85, 3);
                    AddPillarSupport(75, -1, 2);
                    AddPillarSupport(83, -1, 2);
                    // High Balcony with treasure chest
                    AddPlatform(63, 79, 6);
                    AddPillarSupport(65, 3, 5);
                    AddPillarSupport(77, 3, 5);

                    // (Pit 3: x = 89 to 93)

                    // Room 4: Ruined Bastion Vaults (x: 94 to 132)
                    AddBlock(94, 107, -8, -2);
                    AddBlock(108, 121, -8, 0);
                    AddBlock(122, 132, -8, 2);
                    AddPlatform(101, 114, 4);
                    AddPillarSupport(103, 1, 3);
                    AddPillarSupport(112, 1, 3);
                    AddPlatform(116, 128, 6);
                    AddPillarSupport(118, 3, 5);

                    // (Pit 4: x = 133 to 137)

                    // Room 5: Grand Boss Antechamber & Arena (x: 138 to 172)
                    // Grand ceremonial stone steps
                    occupied.Add(new Vector2Int(138, 1));
                    occupied.Add(new Vector2Int(139, 2));
                    occupied.Add(new Vector2Int(140, 3));
                    occupied.Add(new Vector2Int(141, 4));
                    occupied.Add(new Vector2Int(142, 5));
                    // Boss Arena stage with deep foundation
                    AddBlock(143, 172, -8, 6);
                    // Right boundary castle wall
                    AddBlock(171, 174, 7, 14);

                    // Placement of Dead Cells Tiles
                    var posList = new List<Vector3Int>();
                    var tileList = new List<TileBase>();

                    foreach (var pt in occupied)
                    {
                        var pos = new Vector3Int(pt.x, pt.y, 0);
                        bool hasAbove = occupied.Contains(new Vector2Int(pt.x, pt.y + 1));
                        bool hasBelow = occupied.Contains(new Vector2Int(pt.x, pt.y - 1));
                        bool hasLeft = occupied.Contains(new Vector2Int(pt.x - 1, pt.y));
                        bool hasRight = occupied.Contains(new Vector2Int(pt.x + 1, pt.y));

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
                                int hash = Math.Abs((pt.x * 73856093) ^ (pt.y * 19349663));
                                if (hash % 5 == 0 && mossyTile != null) chosen = mossyTile;
                                else if (hash % 7 == 0 && crackedTile != null) chosen = crackedTile;
                                else chosen = surfaceTile;
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
                                int hash = Math.Abs((pt.x * 37) ^ (pt.y * 101));
                                if (hash % 6 == 0 && crackedTile != null) chosen = crackedTile;
                                else chosen = fillTile;
                            }
                        }

                        posList.Add(pos);
                        tileList.Add(chosen);
                    }

                    tm.SetTiles(posList.ToArray(), tileList.ToArray());
                    tm.CompressBounds();

                    var compCol = groundGO.GetComponent<CompositeCollider2D>();
                    if (compCol != null) compCol.GenerateGeometry();
                    Debug.Log($"[DeadCellsTotalMakeover] Set {posList.Count} foundation & platform tiles in Ground tilemap.");
                }
            }

            // 7. Update CheckRoi Death Hazard Zone
            var checkRoiGO = GameObject.Find("CheckRoi");
            if (checkRoiGO != null)
            {
                checkRoiGO.transform.position = new Vector3(75f, -12f, 0f);
                var box = checkRoiGO.GetComponent<BoxCollider2D>();
                if (box != null) box.size = new Vector2(240f, 4f);
            }

            // 8. Rebuild DC_Background_Root & DeadCellsEnvironment with Full Vertical Coverage
            var envGO = GameObject.Find("DeadCellsEnvironment");
            if (envGO == null) envGO = new GameObject("DeadCellsEnvironment");

            var existingBgRoot = GameObject.Find("DC_Background_Root");
            if (existingBgRoot != null) UnityEngine.Object.DestroyImmediate(existingBgRoot);

            var oldProps = new List<GameObject>();
            for (int i = 0; i < envGO.transform.childCount; i++)
            {
                oldProps.Add(envGO.transform.GetChild(i).gameObject);
            }
            foreach (var go in oldProps) UnityEngine.Object.DestroyImmediate(go);

            // Load Sprites
            var sSky = AssetDatabase.LoadAssetAtPath<Sprite>($"{BgBaseDir}/BG_Far/bg_far_sky.png");
            var sTowers = AssetDatabase.LoadAssetAtPath<Sprite>($"{BgBaseDir}/BG_Far/bg_far_towers.png");
            var sArches = AssetDatabase.LoadAssetAtPath<Sprite>($"{BgBaseDir}/BG_Mid/bg_mid_arches.png");
            var sRose = AssetDatabase.LoadAssetAtPath<Sprite>($"{BgBaseDir}/BG_Decoration/bg_dec_rose_window.png");
            var sLancetPurple = AssetDatabase.LoadAssetAtPath<Sprite>($"{BgBaseDir}/BG_Decoration/bg_dec_lancet_purple.png");
            var sLancetCrimson = AssetDatabase.LoadAssetAtPath<Sprite>($"{BgBaseDir}/BG_Decoration/bg_dec_lancet_crimson.png");
            var sColumn = AssetDatabase.LoadAssetAtPath<Sprite>($"{BgBaseDir}/BG_Decoration/bg_dec_columns.png");
            var sBanner = AssetDatabase.LoadAssetAtPath<Sprite>($"{BgBaseDir}/BG_Decoration/bg_dec_banners.png");
            var sCage = AssetDatabase.LoadAssetAtPath<Sprite>($"{BgBaseDir}/BG_Decoration/bg_dec_cages.png");
            var sFog = AssetDatabase.LoadAssetAtPath<Sprite>($"{BgBaseDir}/BG_Atmosphere/bg_atm_fog_layer.png");
            var sGodrays = AssetDatabase.LoadAssetAtPath<Sprite>($"{BgBaseDir}/BG_Atmosphere/bg_atm_godrays.png");
            var sGroundMist = AssetDatabase.LoadAssetAtPath<Sprite>($"{BgBaseDir}/BG_Atmosphere/bg_atm_ground_mist.png");

            var s1Sprites = new Dictionary<string, Sprite>();
            foreach (var a in AssetDatabase.LoadAllAssetsAtPath(Sheet1PngPath))
                if (a is Sprite s) s1Sprites[s.name] = s;

            var s2Sprites = new Dictionary<string, Sprite>();
            foreach (var a in AssetDatabase.LoadAllAssetsAtPath(Sheet2PngPath))
                if (a is Sprite s) s2Sprites[s.name] = s;

            var bgRoot = new GameObject("DC_Background_Root");
            bgRoot.transform.SetParent(envGO.transform, false);
            bgRoot.transform.position = Vector3.zero;

            // --- LAYER 1: FAR SKY (Full Height from y = -6 to +18) ---
            var farSkyGO = new GameObject("Layer_01_Far_Sky");
            farSkyGO.transform.SetParent(bgRoot.transform, false);
            farSkyGO.transform.position = new Vector3(0, 6f, 25f);
            var pSky = farSkyGO.AddComponent<ParallaxLayer>();
            pSky.ParallaxMultiplier = new Vector2(0.96f, 0.25f);
            pSky.TextureUnitSizeX = 16f;
            pSky.InfiniteHorizontal = true;
            for (int i = -4; i <= 4; i++)
            {
                var slice = new GameObject($"Sky_Slice_{i + 4}");
                slice.transform.SetParent(farSkyGO.transform, false);
                slice.transform.localPosition = new Vector3(i * 16f, 0f, 0f);
                var sr = slice.AddComponent<SpriteRenderer>();
                sr.sprite = sSky;
                sr.sortingLayerName = "Default";
                sr.sortingOrder = -60;
            }

            // --- LAYER 2: FAR TOWERS & CHAINS ---
            var farTowersGO = new GameObject("Layer_02_Far_Towers");
            farTowersGO.transform.SetParent(bgRoot.transform, false);
            farTowersGO.transform.position = new Vector3(0, 5f, 22f);
            var pTowers = farTowersGO.AddComponent<ParallaxLayer>();
            pTowers.ParallaxMultiplier = new Vector2(0.90f, 0.20f);
            pTowers.TextureUnitSizeX = 32f;
            pTowers.InfiniteHorizontal = true;
            for (int i = -2; i <= 2; i++)
            {
                var slice = new GameObject($"Towers_Slice_{i + 2}");
                slice.transform.SetParent(farTowersGO.transform, false);
                slice.transform.localPosition = new Vector3(i * 32f, 0f, 0f);
                var sr = slice.AddComponent<SpriteRenderer>();
                sr.sprite = sTowers;
                sr.sortingLayerName = "Default";
                sr.sortingOrder = -50;
            }

            // --- LAYER 3: GRAND CATHEDRAL ARCHES & VAULTED CEILING (Full Height y = -6 to +18) ---
            var midArchesGO = new GameObject("Layer_03_Mid_Arches");
            midArchesGO.transform.SetParent(bgRoot.transform, false);
            midArchesGO.transform.position = new Vector3(0, 6f, 16f);
            var pArches = midArchesGO.AddComponent<ParallaxLayer>();
            pArches.ParallaxMultiplier = new Vector2(0.70f, 0.15f);
            pArches.TextureUnitSizeX = 32f; // 1024 / 32
            pArches.InfiniteHorizontal = true;
            for (int i = -3; i <= 3; i++)
            {
                var slice = new GameObject($"Arch_Slice_{i + 3}");
                slice.transform.SetParent(midArchesGO.transform, false);
                slice.transform.localPosition = new Vector3(i * 32f, 0f, 0f);
                var sr = slice.AddComponent<SpriteRenderer>();
                sr.sprite = sArches;
                sr.sortingLayerName = "Default";
                sr.sortingOrder = -35;
            }

            // --- LAYER 4: ATMOSPHERE BACK (Purple Fog Band) ---
            var atmBackGO = new GameObject("Layer_04_Atmosphere_Back");
            atmBackGO.transform.SetParent(bgRoot.transform, false);
            atmBackGO.transform.position = new Vector3(0, 5.0f, 14f);
            var pAtmBack = atmBackGO.AddComponent<ParallaxLayer>();
            pAtmBack.ParallaxMultiplier = new Vector2(0.80f, 0.15f);
            pAtmBack.TextureUnitSizeX = 32f;
            pAtmBack.InfiniteHorizontal = true;
            for (int i = -2; i <= 2; i++)
            {
                var slice = new GameObject($"Fog_Slice_{i + 2}");
                slice.transform.SetParent(atmBackGO.transform, false);
                slice.transform.localPosition = new Vector3(i * 32f, 0f, 0f);
                var sr = slice.AddComponent<SpriteRenderer>();
                sr.sprite = sFog;
                sr.sortingLayerName = "Default";
                sr.sortingOrder = -28;
            }

            // --- LAYER 5: DECORATION (Rose Windows, Sweeping God Rays, Lancets, Columns, Banners, Cages) ---
            var decGO = new GameObject("Layer_05_Decoration");
            decGO.transform.SetParent(bgRoot.transform, false);
            decGO.transform.position = new Vector3(0, 0f, 10f);
            var pDec = decGO.AddComponent<ParallaxLayer>();
            pDec.ParallaxMultiplier = new Vector2(0.50f, 0.10f);
            pDec.InfiniteHorizontal = false;

            // Grand Rose Windows positioned prominently in cathedral naves
            float[] roseX = { 28f, 71f, 115f, 156f };
            float[] roseY = { 7.5f, 7.5f, 7.5f, 10.0f };
            for (int i = 0; i < roseX.Length; i++)
            {
                var rose = new GameObject($"Dec_RoseWindow_{i}");
                rose.transform.SetParent(decGO.transform, false);
                rose.transform.position = new Vector3(roseX[i], roseY[i], 10f);
                var sr = rose.AddComponent<SpriteRenderer>();
                sr.sprite = sRose;
                sr.sortingLayerName = "Default";
                sr.sortingOrder = -20;

                // Celestial Blue Point Light
                var rLight = rose.AddComponent<Light2D>();
                rLight.lightType = Light2D.LightType.Point;
                rLight.color = new Color(0.30f, 0.72f, 1.0f, 1f);
                rLight.intensity = 2.2f;
                rLight.pointLightInnerRadius = 1.5f;
                rLight.pointLightOuterRadius = 12.0f;

                // Sweeping 24-Unit Full-Height God Rays streaming down to the crypt floor!
                var rays = new GameObject($"Dec_Godrays_{i}");
                rays.transform.SetParent(decGO.transform, false);
                rays.transform.position = new Vector3(roseX[i] + 7.0f, roseY[i] - 7.0f, 9.5f);
                var rsr = rays.AddComponent<SpriteRenderer>();
                rsr.sprite = sGodrays;
                rsr.sortingLayerName = "Default";
                rsr.sortingOrder = -18;
            }

            // Tall Lancet Windows
            float[] lancetX = { -6f, 6f, 18f, 38f, 54f, 64f, 78f, 88f, 102f, 126f, 142f, 150f, 166f };
            float[] lancetY = { 2.5f, 2.5f, 2.5f, 2.5f, 2.5f, 2.5f, 2.5f, 2.5f, 3.5f, 4.5f, 8.0f, 8.0f, 8.0f };
            for (int i = 0; i < lancetX.Length; i++)
            {
                var lancet = new GameObject($"Dec_LancetWindow_{i}");
                lancet.transform.SetParent(decGO.transform, false);
                lancet.transform.position = new Vector3(lancetX[i], lancetY[i], 10.5f);
                var sr = lancet.AddComponent<SpriteRenderer>();
                sr.sprite = (i % 2 == 0) ? sLancetPurple : sLancetCrimson;
                sr.sortingLayerName = "Default";
                sr.sortingOrder = -22;

                var lLight = lancet.AddComponent<Light2D>();
                lLight.lightType = Light2D.LightType.Point;
                lLight.color = (i % 2 == 0) ? new Color(0.75f, 0.25f, 1f, 1f) : new Color(1.0f, 0.2f, 0.4f, 1f);
                lLight.intensity = 0.95f;
                lLight.pointLightInnerRadius = 0.4f;
                lLight.pointLightOuterRadius = 5.0f;
            }

            // Gothic Cathedral Columns
            float[] colX = { -12f, 12f, 24f, 44f, 56f, 72f, 88f, 104f, 122f, 138f, 152f, 168f };
            float[] colY = { 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 2.0f, 4.0f, 7.0f, 7.0f };
            for (int i = 0; i < colX.Length; i++)
            {
                var col = new GameObject($"Dec_Column_{i}");
                col.transform.SetParent(decGO.transform, false);
                col.transform.position = new Vector3(colX[i], colY[i], 8f);
                var sr = col.AddComponent<SpriteRenderer>();
                sr.sprite = sColumn;
                sr.sortingLayerName = "Default";
                sr.sortingOrder = -16;
            }

            // Hanging Banners
            float[] bannerX = { -2f, 20f, 36f, 58f, 80f, 98f, 124f, 146f, 162f };
            float[] bannerY = { 2.5f, 2.5f, 2.5f, 2.5f, 2.5f, 2.5f, 4.0f, 8.0f, 8.0f };
            for (int i = 0; i < bannerX.Length; i++)
            {
                var banner = new GameObject($"Dec_Banner_{i}");
                banner.transform.SetParent(decGO.transform, false);
                banner.transform.position = new Vector3(bannerX[i], bannerY[i], 7.5f);
                var sr = banner.AddComponent<SpriteRenderer>();
                sr.sprite = sBanner;
                sr.sortingLayerName = "Default";
                sr.sortingOrder = -14;
            }

            // Hanging Gibbet Cages and Chains
            float[] cageX = { 8f, 32f, 42f, 68f, 86f, 110f, 130f, 154f };
            float[] cageY = { 5.5f, 5.5f, 5.8f, 5.5f, 5.8f, 5.5f, 7.0f, 10.5f };
            for (int i = 0; i < cageX.Length; i++)
            {
                var cage = new GameObject($"Dec_Cage_{i}");
                cage.transform.SetParent(decGO.transform, false);
                cage.transform.position = new Vector3(cageX[i], cageY[i], 7.5f);
                var sr = cage.AddComponent<SpriteRenderer>();
                sr.sprite = sCage;
                sr.sortingLayerName = "Default";
                sr.sortingOrder = -14;
            }

            // --- LAYER 6: ATMOSPHERE FRONT (Ground Mist) ---
            var atmFrontGO = new GameObject("Layer_06_Atmosphere_Front");
            atmFrontGO.transform.SetParent(bgRoot.transform, false);
            atmFrontGO.transform.position = new Vector3(0, -2.5f, 5f);
            var pAtmFront = atmFrontGO.AddComponent<ParallaxLayer>();
            pAtmFront.ParallaxMultiplier = new Vector2(0.30f, 0.05f);
            pAtmFront.TextureUnitSizeX = 32f;
            pAtmFront.InfiniteHorizontal = true;
            for (int i = -3; i <= 3; i++)
            {
                var slice = new GameObject($"GroundMist_Slice_{i + 3}");
                slice.transform.SetParent(atmFrontGO.transform, false);
                slice.transform.localPosition = new Vector3(i * 32f, 0f, 0f);
                var sr = slice.AddComponent<SpriteRenderer>();
                sr.sprite = sGroundMist;
                sr.sortingLayerName = "Default";
                sr.sortingOrder = -5;
            }

            // --- 9. Wall Torches & Intense Warm Flame Point Lights ---
            var torchSprite = s1Sprites.GetValueOrDefault("dc1_torch_wall");
            float[] torchX = { -9f, 4f, 18f, 30f, 44f, 56f, 68f, 82f, 98f, 112f, 126f, 140f, 148f, 158f, 168f };
            float[] torchY = { 0.2f, 0.2f, -0.5f, 3.5f, -0.5f, 1.5f, 3.5f, 0.5f, 1.5f, 2.5f, 4.0f, 6.5f, 8.2f, 8.2f, 8.2f };

            for (int i = 0; i < torchX.Length; i++)
            {
                var torchGO = new GameObject($"DC_WallTorch_{i}");
                torchGO.transform.SetParent(envGO.transform, false);
                torchGO.transform.position = new Vector3(torchX[i], torchY[i], 0f);

                if (torchSprite != null)
                {
                    var sr = torchGO.AddComponent<SpriteRenderer>();
                    sr.sprite = torchSprite;
                    sr.sortingLayerName = "Player";
                    sr.sortingOrder = -1;
                }

                var light = torchGO.AddComponent<Light2D>();
                light.lightType = Light2D.LightType.Point;
                light.color = new Color(1.0f, 0.58f, 0.18f, 1f); // Warm flame orange
                light.intensity = 2.0f;
                light.pointLightInnerRadius = 0.6f;
                light.pointLightOuterRadius = 7.5f;
            }

            // --- 10. Doors, Crates, Chests, Statue, Crystals ---
            var doorGO = new GameObject("DC_EntranceDoor");
            doorGO.transform.SetParent(envGO.transform, false);
            doorGO.transform.position = new Vector3(-13.0f, -1.8f, 0.5f);
            var doorSr = doorGO.AddComponent<SpriteRenderer>();
            doorSr.sprite = s1Sprites.GetValueOrDefault("dc1_wooden_door");
            doorSr.sortingLayerName = "Ground";
            doorSr.sortingOrder = 0;

            var gateGO = new GameObject("DC_BossGate");
            gateGO.transform.SetParent(envGO.transform, false);
            gateGO.transform.position = new Vector3(147.5f, 7.2f, 0.5f);
            var gateSr = gateGO.AddComponent<SpriteRenderer>();
            gateSr.sprite = s1Sprites.GetValueOrDefault("dc1_iron_gate");
            gateSr.sortingLayerName = "Ground";
            gateSr.sortingOrder = 0;

            float[] crateX = { -7f, 16f, 45f, 84f, 110f, 144f };
            float[] crateY = { -1.8f, -1.8f, -1.8f, -1.8f, 0.2f, 6.2f };
            for (int i = 0; i < crateX.Length; i++)
            {
                var crateGO = new GameObject($"DC_Crates_{i}");
                crateGO.transform.SetParent(envGO.transform, false);
                crateGO.transform.position = new Vector3(crateX[i], crateY[i], 0f);
                var sr = crateGO.AddComponent<SpriteRenderer>();
                sr.sprite = s1Sprites.GetValueOrDefault("dc1_crates_stacked");
                sr.sortingLayerName = "Ground";
                sr.sortingOrder = 1;
            }

            float[] chestX = { 24f, 78f, 126f };
            float[] chestY = { 2.2f, 3.2f, 6.2f };
            for (int i = 0; i < chestX.Length; i++)
            {
                var chestGO = new GameObject($"DC_Chest_{i}");
                chestGO.transform.SetParent(envGO.transform, false);
                chestGO.transform.position = new Vector3(chestX[i], chestY[i], 0f);
                var sr = chestGO.AddComponent<SpriteRenderer>();
                sr.sprite = s1Sprites.GetValueOrDefault(i == 0 ? "dc1_chest_ornate" : "dc1_chest_wood");
                sr.sortingLayerName = "Ground";
                sr.sortingOrder = 1;
            }

            var statueGO = new GameObject("DC_GothicStatue");
            statueGO.transform.SetParent(envGO.transform, false);
            statueGO.transform.position = new Vector3(64.0f, 1.2f, 0.5f);
            var statueSr = statueGO.AddComponent<SpriteRenderer>();
            statueSr.sprite = s2Sprites.GetValueOrDefault("dc2_statue_hooded");
            statueSr.sortingLayerName = "Ground";
            statueSr.sortingOrder = 1;

            float[] crystalX = { 40f, 98f, 146f, 164f };
            float[] crystalY = { 3.2f, 1.5f, 6.8f, 6.8f };
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
                cLight.intensity = 1.3f;
                cLight.pointLightInnerRadius = 0.5f;
                cLight.pointLightOuterRadius = 4.5f;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("<color=green>[DeadCellsTotalMakeover] COMPLETED SUCCESSFULLY! No green trees, full vertical gothic arches, and sweeping god rays!</color>");
        }

        private static void ReimportGrandAssets()
        {
            string[] paths = {
                $"{BgBaseDir}/BG_Far/bg_far_sky.png",
                $"{BgBaseDir}/BG_Mid/bg_mid_arches.png",
                $"{BgBaseDir}/BG_Atmosphere/bg_atm_godrays.png"
            };

            foreach (var p in paths)
            {
                AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate);
                var ti = AssetImporter.GetAtPath(p) as TextureImporter;
                if (ti != null)
                {
                    ti.textureType = TextureImporterType.Sprite;
                    ti.spriteImportMode = SpriteImportMode.Single;
                    ti.spritePixelsPerUnit = 32f;
                    ti.filterMode = FilterMode.Point;
                    ti.textureCompression = TextureImporterCompression.Uncompressed;
                    ti.alphaIsTransparency = true;
                    ti.wrapMode = TextureWrapMode.Repeat;
                    ti.SaveAndReimport();
                }
            }
        }
    }
}
#endif
