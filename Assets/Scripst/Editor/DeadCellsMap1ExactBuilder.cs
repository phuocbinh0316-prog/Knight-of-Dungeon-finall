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
    public static class DeadCellsMap1ExactBuilder
    {
        private const string Map1Path = "Assets/Scenes/map 1.unity";
        private const string TileDir = "Assets/Tile/DeadCellsDungeon";
        private const string BgBaseDir = "Assets/Art/NewDungeon/Background";
        private const string Sheet1PngPath = "Assets/DeadCellsDungeon/dc_tileset_1.png";
        private const string Sheet2PngPath = "Assets/DeadCellsDungeon/dc_tileset_2.png";

        [MenuItem("Knight of Dungeon/Build Dead Cells Map 1 Exact")]
        public static void Run()
        {
            Debug.Log("<color=cyan>=== [DeadCellsMap1ExactBuilder] Building Map 1 to match Dead Cells Concept ===</color>");

            var scene = EditorSceneManager.OpenScene(Map1Path, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"Cannot open scene {Map1Path}");
                return;
            }

            // 1. Configure Main Camera Clear Flags & Deep Gothic Void
            var mainCamGO = GameObject.Find("Main Camera");
            if (mainCamGO != null)
            {
                var cam = mainCamGO.GetComponent<Camera>();
                if (cam != null)
                {
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = new Color(0.047f, 0.039f, 0.094f, 1f); // Deep midnight void
                }
            }

            // 2. Configure Global Light 2D for Moody Dead Cells Atmosphere
            var globalLightGO = GameObject.Find("Global Light 2D");
            if (globalLightGO != null)
            {
                var gl = globalLightGO.GetComponent<Light2D>();
                if (gl != null)
                {
                    gl.color = new Color(0.14f, 0.11f, 0.22f, 1f); // Twilight indigo
                    gl.intensity = 0.65f;
                }
            }

            // 3. Load Tile Assets
            var surfaceTile = AssetDatabase.LoadAssetAtPath<Tile>($"{TileDir}/dc_surface.asset");
            var fillTile = AssetDatabase.LoadAssetAtPath<Tile>($"{TileDir}/dc_fill.asset");
            var wallLeftTile = AssetDatabase.LoadAssetAtPath<Tile>($"{TileDir}/dc_wall_left.asset");
            var wallRightTile = AssetDatabase.LoadAssetAtPath<Tile>($"{TileDir}/dc_wall_right.asset");
            var ceilingTile = AssetDatabase.LoadAssetAtPath<Tile>($"{TileDir}/dc_ceiling.asset");
            var platformTile = AssetDatabase.LoadAssetAtPath<Tile>($"{TileDir}/dc_platform.asset");
            var crackedTile = AssetDatabase.LoadAssetAtPath<Tile>($"{TileDir}/dc_cracked.asset");
            var mossyTile = AssetDatabase.LoadAssetAtPath<Tile>($"{TileDir}/dc_mossy.asset");

            // 4. Rebuild Grid/Ground Multi-Tier Platform Geometry
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
                        {
                            for (int y = y0; y <= y1; y++)
                            {
                                occupied.Add(new Vector2Int(x, y));
                            }
                        }
                    }

                    void AddPlatformLedge(int x0, int x1, int y)
                    {
                        for (int x = x0; x <= x1; x++)
                        {
                            occupied.Add(new Vector2Int(x, y));
                        }
                    }

                    // --- SECTION 1: West Crypt & Entrance (x: -16 to 10) ---
                    // Main solid floor
                    AddBlock(-16, 10, -4, -2);
                    // Entrance backstop wall
                    AddBlock(-17, -16, -1, 5);
                    // Mid-tier stone balcony
                    AddPlatformLedge(-6, 2, 1);
                    // Stepped stone stair to ledge
                    occupied.Add(new Vector2Int(3, 0));
                    occupied.Add(new Vector2Int(4, 1));

                    // (Pit 1: gap at x = 11 to 13)

                    // --- SECTION 2: Great Cathedral Nave & Rose Window (x: 14 to 46) ---
                    // Lower crypt floor
                    AddBlock(14, 46, -4, -2);
                    // Mid-tier stone platform (under god rays)
                    AddPlatformLedge(19, 33, 2);
                    // Floating arch bridge
                    AddPlatformLedge(36, 44, 3);
                    // Upper cathedral high balcony
                    AddPlatformLedge(23, 40, 6);
                    // Stone staircase to balcony
                    occupied.Add(new Vector2Int(34, 3));
                    occupied.Add(new Vector2Int(35, 4));
                    occupied.Add(new Vector2Int(36, 5));

                    // (Pit 2: gap at x = 47 to 51)

                    // --- SECTION 3: Crypt Colonnade & Shrine (x: 52 to 88) ---
                    // Floor
                    AddBlock(52, 88, -4, -2);
                    // Mid platforms
                    AddPlatformLedge(57, 69, 2);
                    AddPlatformLedge(73, 85, 3);
                    // High Balcony with treasure chest
                    AddPlatformLedge(63, 79, 6);

                    // (Pit 3: gap at x = 89 to 93)

                    // --- SECTION 4: Ruined Bastion Vaults (x: 94 to 132) ---
                    // Stepped multi-level crypt floors
                    AddBlock(94, 107, -4, -2);
                    AddBlock(108, 121, -4, 0);
                    AddBlock(122, 132, -4, 2);
                    // Elevated stone ledges
                    AddPlatformLedge(101, 114, 4);
                    AddPlatformLedge(116, 128, 6);

                    // (Pit 4: gap at x = 133 to 137)

                    // --- SECTION 5: Grand Boss Antechamber & Arena (x: 138 to 170) ---
                    // Grand ceremonial stone steps
                    occupied.Add(new Vector2Int(138, 1));
                    occupied.Add(new Vector2Int(139, 2));
                    occupied.Add(new Vector2Int(140, 3));
                    occupied.Add(new Vector2Int(141, 4));
                    occupied.Add(new Vector2Int(142, 5));
                    // Boss Arena stage (elevated battle hall)
                    AddBlock(143, 170, 0, 6);
                    // Arena right boundary wall
                    AddBlock(170, 172, 7, 12);

                    // Tile Placement with Dead Cells Tile Rules
                    var positions = new List<Vector3Int>();
                    var tiles = new List<TileBase>();

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
                                if (hash % 5 == 0 && mossyTile != null)
                                    chosen = mossyTile;
                                else if (hash % 7 == 0 && crackedTile != null)
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
                                int hash = Math.Abs((pt.x * 37) ^ (pt.y * 101));
                                if (hash % 6 == 0 && crackedTile != null)
                                    chosen = crackedTile;
                                else
                                    chosen = fillTile;
                            }
                        }

                        positions.Add(pos);
                        tiles.Add(chosen);
                    }

                    tm.SetTiles(positions.ToArray(), tiles.ToArray());
                    tm.CompressBounds();

                    var compCol = groundGO.GetComponent<CompositeCollider2D>();
                    if (compCol != null)
                    {
                        compCol.GenerateGeometry();
                    }
                    Debug.Log($"[DeadCellsMap1ExactBuilder] Set {positions.Count} modular dungeon tiles in Ground tilemap.");
                }
            }

            // 5. Update CheckRoi Death Hazard Zone
            var checkRoiGO = GameObject.Find("CheckRoi");
            if (checkRoiGO != null)
            {
                checkRoiGO.transform.position = new Vector3(75f, -9f, 0f);
                var box = checkRoiGO.GetComponent<BoxCollider2D>();
                if (box != null)
                {
                    box.size = new Vector2(230f, 4f);
                }
            }

            // 6. Rebuild DeadCellsEnvironment and DC_Background_Root
            var envGO = GameObject.Find("DeadCellsEnvironment");
            if (envGO == null) envGO = new GameObject("DeadCellsEnvironment");

            // Clean previous background root
            var existingBgRoot = GameObject.Find("DC_Background_Root");
            if (existingBgRoot != null) UnityEngine.Object.DestroyImmediate(existingBgRoot);

            // Clean old prop children
            var propsToDestroy = new List<GameObject>();
            for (int i = 0; i < envGO.transform.childCount; i++)
            {
                propsToDestroy.Add(envGO.transform.GetChild(i).gameObject);
            }
            foreach (var go in propsToDestroy) UnityEngine.Object.DestroyImmediate(go);

            // Load all sprite resources
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

            // Create Master DC_Background_Root
            var bgRoot = new GameObject("DC_Background_Root");
            bgRoot.transform.SetParent(envGO.transform, false);
            bgRoot.transform.position = Vector3.zero;

            // --- LAYER 1: FAR SKY ---
            var farSkyGO = new GameObject("Layer_01_Far_Sky");
            farSkyGO.transform.SetParent(bgRoot.transform, false);
            farSkyGO.transform.position = new Vector3(0, 3f, 25f);
            var pSky = farSkyGO.AddComponent<ParallaxLayer>();
            pSky.ParallaxMultiplier = new Vector2(0.96f, 0.25f);
            pSky.TextureUnitSizeX = 16f;
            pSky.InfiniteHorizontal = true;
            for (int i = -3; i <= 3; i++)
            {
                var slice = new GameObject($"Sky_Slice_{i + 3}");
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
            farTowersGO.transform.position = new Vector3(0, 2.5f, 22f);
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

            // --- LAYER 3: MID GOTHIC ARCHES ---
            var midArchesGO = new GameObject("Layer_03_Mid_Arches");
            midArchesGO.transform.SetParent(bgRoot.transform, false);
            midArchesGO.transform.position = new Vector3(0, 1.2f, 16f);
            var pArches = midArchesGO.AddComponent<ParallaxLayer>();
            pArches.ParallaxMultiplier = new Vector2(0.70f, 0.15f);
            pArches.TextureUnitSizeX = 24f;
            pArches.InfiniteHorizontal = true;
            for (int i = -3; i <= 3; i++)
            {
                var slice = new GameObject($"Arch_Slice_{i + 3}");
                slice.transform.SetParent(midArchesGO.transform, false);
                slice.transform.localPosition = new Vector3(i * 24f, 0f, 0f);
                var sr = slice.AddComponent<SpriteRenderer>();
                sr.sprite = sArches;
                sr.sortingLayerName = "Default";
                sr.sortingOrder = -35;
            }

            // --- LAYER 4: ATMOSPHERE BACK (Purple Fog Band) ---
            var atmBackGO = new GameObject("Layer_04_Atmosphere_Back");
            atmBackGO.transform.SetParent(bgRoot.transform, false);
            atmBackGO.transform.position = new Vector3(0, 3.0f, 14f);
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

            // --- LAYER 5: DECORATION (Rose Windows, Godrays, Lancet, Columns, Banners, Cages) ---
            var decGO = new GameObject("Layer_05_Decoration");
            decGO.transform.SetParent(bgRoot.transform, false);
            decGO.transform.position = new Vector3(0, 0f, 10f);
            var pDec = decGO.AddComponent<ParallaxLayer>();
            pDec.ParallaxMultiplier = new Vector2(0.50f, 0.10f);
            pDec.InfiniteHorizontal = false;

            // Grand Rose Windows at cathedral nave centers
            float[] roseX = { 28f, 71f, 115f, 158f };
            float[] roseY = { 5.2f, 5.2f, 5.8f, 8.5f };
            for (int i = 0; i < roseX.Length; i++)
            {
                var rose = new GameObject($"Dec_RoseWindow_{i}");
                rose.transform.SetParent(decGO.transform, false);
                rose.transform.position = new Vector3(roseX[i], roseY[i], 10f);
                var sr = rose.AddComponent<SpriteRenderer>();
                sr.sprite = sRose;
                sr.sortingLayerName = "Default";
                sr.sortingOrder = -20;

                // Celestial Blue Point Light on Rose Window
                var rLight = rose.AddComponent<Light2D>();
                rLight.lightType = Light2D.LightType.Point;
                rLight.color = new Color(0.30f, 0.72f, 1.0f, 1f);
                rLight.intensity = 1.9f;
                rLight.pointLightInnerRadius = 1.2f;
                rLight.pointLightOuterRadius = 10.0f;

                // Volumetric God Rays angled down into the crypt
                var rays = new GameObject($"Dec_Godrays_{i}");
                rays.transform.SetParent(decGO.transform, false);
                rays.transform.position = new Vector3(roseX[i] + 4.5f, roseY[i] - 1.8f, 9.5f);
                var rsr = rays.AddComponent<SpriteRenderer>();
                rsr.sprite = sGodrays;
                rsr.sortingLayerName = "Default";
                rsr.sortingOrder = -18;
            }

            // Tall Lancet Windows
            float[] lancetX = { -6f, 6f, 18f, 38f, 54f, 64f, 78f, 88f, 102f, 126f, 142f, 150f, 166f };
            float[] lancetY = { 2.2f, 2.2f, 2.2f, 2.2f, 2.2f, 2.2f, 2.2f, 2.2f, 2.2f, 3.5f, 7.0f, 7.0f, 7.0f };
            for (int i = 0; i < lancetX.Length; i++)
            {
                var lancet = new GameObject($"Dec_LancetWindow_{i}");
                lancet.transform.SetParent(decGO.transform, false);
                lancet.transform.position = new Vector3(lancetX[i], lancetY[i], 10.5f);
                var sr = lancet.AddComponent<SpriteRenderer>();
                sr.sprite = (i % 2 == 0) ? sLancetPurple : sLancetCrimson;
                sr.sortingLayerName = "Default";
                sr.sortingOrder = -22;

                // Subtle window ambient light
                var lLight = lancet.AddComponent<Light2D>();
                lLight.lightType = Light2D.LightType.Point;
                lLight.color = (i % 2 == 0) ? new Color(0.75f, 0.25f, 1f, 1f) : new Color(1.0f, 0.2f, 0.4f, 1f);
                lLight.intensity = 0.85f;
                lLight.pointLightInnerRadius = 0.3f;
                lLight.pointLightOuterRadius = 4.5f;
            }

            // Gothic Cathedral Columns
            float[] colX = { -12f, 12f, 24f, 44f, 56f, 72f, 88f, 104f, 122f, 138f, 152f, 168f };
            float[] colY = { 0.2f, 0.2f, 0.2f, 0.2f, 0.2f, 0.2f, 0.2f, 0.2f, 1.5f, 3.5f, 6.2f, 6.2f };
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
            float[] bannerY = { 2.5f, 2.5f, 2.5f, 2.5f, 2.5f, 2.5f, 4.0f, 7.5f, 7.5f };
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
            float[] cageY = { 4.5f, 4.5f, 4.8f, 4.5f, 4.8f, 4.5f, 6.0f, 9.2f };
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
            for (int i = -2; i <= 2; i++)
            {
                var slice = new GameObject($"GroundMist_Slice_{i + 2}");
                slice.transform.SetParent(atmFrontGO.transform, false);
                slice.transform.localPosition = new Vector3(i * 32f, 0f, 0f);
                var sr = slice.AddComponent<SpriteRenderer>();
                sr.sprite = sGroundMist;
                sr.sortingLayerName = "Default";
                sr.sortingOrder = -5;
            }

            // --- 7. Wall Torches & Warm Orange Point Lights ---
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
                light.intensity = 1.85f;
                light.pointLightInnerRadius = 0.6f;
                light.pointLightOuterRadius = 7.0f;
            }

            // --- 8. Dungeon Doors & Gates ---
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

            // --- 9. Crates, Barrels, Treasure Chests, Statue, Crystals ---
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

            // Save Scene
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"<color=green>[DeadCellsMap1ExactBuilder] SUCCESS! map 1.unity rebuilt to match Dead Cells concept art!</color>");
        }
    }
}
#endif
