#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace Knight.Editor
{
    public static class DungeonMap1Redesign
    {
        private const string TilesetPath = "Assets/GothicVania Church/Environment/tileset.png";
        private const string TileOutputDir = "Assets/Tile/GothicDungeon";
        private const string Map1Path = "Assets/Scenes/map 1.unity";

        [MenuItem("Knight of Dungeon/Redesign Map 1 to Dungeon")]
        public static void Run()
        {
            Debug.Log("<color=cyan>=== [DungeonMap1Redesign] Starting Map 1 Dungeon Redesign ===</color>");

            try
            {
                bool tilesExist = File.Exists($"{TileOutputDir}/gothic_surface.asset") &&
                                  File.Exists($"{TileOutputDir}/gothic_fill.asset");

                if (!tilesExist)
                {
                    EnsureFolders();
                    SliceTileset();
                    ConfigureBackgroundSprites();

                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    CreateTileAssets();

                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
                else
                {
                    Debug.Log("[DungeonMap1Redesign] Tile assets already exist on disk; skipping importer mutation.");
                }

                RedesignMap1();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("<color=green>=== [DungeonMap1Redesign] SUCCESS! Map 1 is now a true Gothic Dungeon! ===</color>");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[DungeonMap1Redesign] Error during redesign: " + ex);
            }
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Tile"))
                AssetDatabase.CreateFolder("Assets", "Tile");
            if (!AssetDatabase.IsValidFolder(TileOutputDir))
                AssetDatabase.CreateFolder("Assets/Tile", "GothicDungeon");
        }

        private static void SliceTileset()
        {
            var ti = AssetImporter.GetAtPath(TilesetPath) as TextureImporter;
            if (ti == null)
            {
                Debug.LogError("Cannot find tileset at " + TilesetPath);
                return;
            }

            ti.isReadable = true;
            ti.spriteImportMode = SpriteImportMode.Multiple;
            ti.spritePixelsPerUnit = 16f;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;

            int tileW = 16;
            int tileH = 16;
            int cols = 336 / tileW; // 21
            int rows = 224 / tileH; // 14

            var metas = new List<SpriteMetaData>();
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    var meta = new SpriteMetaData
                    {
                        rect = new Rect(c * tileW, r * tileH, tileW, tileH),
                        name = $"gothic_r{r}_c{c}",
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f)
                    };
                    metas.Add(meta);
                }
            }

            ti.spritesheet = metas.ToArray();
            EditorUtility.SetDirty(ti);
            ti.SaveAndReimport();
            Debug.Log("[DungeonMap1Redesign] Sliced tileset.png into 294 16x16 tiles");
        }

        private static void ConfigureBackgroundSprites()
        {
            string[] bgs = new string[]
            {
                "Assets/GothicVania Church/Environment/bg-1.png",
                "Assets/GothicVania Church/Environment/bg-2.png",
                "Assets/GothicVania Church/Environment/bg-3.png",
                "Assets/GothicVania Church/Environment/bg-4.png",
                "Assets/GothicVania Church/Environment/bg-5.png",
                "Assets/GothicVania Church/Environment/column.png"
            };

            foreach (var p in bgs)
            {
                var ti = AssetImporter.GetAtPath(p) as TextureImporter;
                if (ti != null)
                {
                    ti.isReadable = true;
                    ti.spritePixelsPerUnit = 16f;
                    ti.filterMode = FilterMode.Point;
                    ti.textureCompression = TextureImporterCompression.Uncompressed;
                    if (p.EndsWith("bg-5.png"))
                    {
                        ti.wrapMode = TextureWrapMode.Repeat;
                    }
                    EditorUtility.SetDirty(ti);
                    ti.SaveAndReimport();
                }
            }
            Debug.Log("[DungeonMap1Redesign] Configured GothicVania background sprites with PPU=16 and Point filter");
        }

        private static void CreateTileAssets()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(TilesetPath);
            var spriteMap = new Dictionary<string, Sprite>();
            foreach (var a in assets)
            {
                if (a is Sprite s)
                {
                    spriteMap[s.name] = s;
                }
            }

            // In Unity coordinates (bottom-up):
            // Row 3 is top stone surface (c1 = center, c0 = left, c2 = right)
            // Row 2 is middle stone wall fill (c1 = solid brick, c0 = left wall, c2 = right wall)
            // Row 1 is bottom ceiling underside (c1 = ceiling)
            // Col 12, 13 = floating thin ledge
            var surfaceSprite = spriteMap.GetValueOrDefault("gothic_r3_c1");
            var fillSprite = spriteMap.GetValueOrDefault("gothic_r2_c1");
            var wallLeftSprite = spriteMap.GetValueOrDefault("gothic_r2_c0");
            var wallRightSprite = spriteMap.GetValueOrDefault("gothic_r2_c2");
            var ceilingSprite = spriteMap.GetValueOrDefault("gothic_r1_c1");
            var platformSprite = spriteMap.GetValueOrDefault("gothic_r3_c12");

            GetOrCreateTile(surfaceSprite, "gothic_surface");
            GetOrCreateTile(fillSprite, "gothic_fill");
            GetOrCreateTile(wallLeftSprite, "gothic_wall_left");
            GetOrCreateTile(wallRightSprite, "gothic_wall_right");
            GetOrCreateTile(ceilingSprite, "gothic_ceiling");
            GetOrCreateTile(platformSprite, "gothic_platform");

            Debug.Log("[DungeonMap1Redesign] Created and saved Gothic Dungeon Tile assets");
        }

        private static Tile GetOrCreateTile(Sprite sprite, string name)
        {
            if (sprite == null) return null;
            string path = $"{TileOutputDir}/{name}.asset";
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

            var surfaceTile = AssetDatabase.LoadAssetAtPath<Tile>($"{TileOutputDir}/gothic_surface.asset");
            var fillTile = AssetDatabase.LoadAssetAtPath<Tile>($"{TileOutputDir}/gothic_fill.asset");
            var wallLeftTile = AssetDatabase.LoadAssetAtPath<Tile>($"{TileOutputDir}/gothic_wall_left.asset");
            var wallRightTile = AssetDatabase.LoadAssetAtPath<Tile>($"{TileOutputDir}/gothic_wall_right.asset");
            var ceilingTile = AssetDatabase.LoadAssetAtPath<Tile>($"{TileOutputDir}/gothic_ceiling.asset");
            var platformTile = AssetDatabase.LoadAssetAtPath<Tile>($"{TileOutputDir}/gothic_platform.asset");

            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(surfaceTile, out string g, out long id);
            Debug.Log($"[DungeonMap1Redesign] In-Scene Loaded tiles: surface={surfaceTile?.name} (guid={g}, id={id}), fill={fillTile?.name}, wallL={wallLeftTile?.name}, wallR={wallRightTile?.name}");
            if (surfaceTile == null || fillTile == null)
            {
                Debug.LogError("[DungeonMap1Redesign] CRITICAL: Required gothic tile assets not found in scene! Aborting redesign.");
                return;
            }

            // 1. Remove all outdoor sea, sky, clouds, and grassy island objects (including all nested children)
            string[] outdoorKeywords = new string[] { "sky", "clouds", "sea", "far-grounds" };
            var allGOs = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var toDestroy = new HashSet<GameObject>();

            foreach (var go in allGOs)
            {
                if (go == null) continue;
                string lower = go.name.ToLower();
                foreach (var kw in outdoorKeywords)
                {
                    if (lower.StartsWith(kw) || lower.Contains(kw))
                    {
                        toDestroy.Add(go);
                        break;
                    }
                }
            }

            foreach (var d in toDestroy)
            {
                if (d != null)
                {
                    Object.DestroyImmediate(d);
                }
            }
            Debug.Log($"[DungeonMap1Redesign] Removed {toDestroy.Count} outdoor objects");

            // 2. Remove the Grass tilemap
            var gridTransform = GameObject.Find("Grid")?.transform;
            if (gridTransform != null)
            {
                var grassTransform = gridTransform.Find("Grass");
                if (grassTransform != null)
                {
                    var grassTilemap = grassTransform.GetComponent<Tilemap>();
                    if (grassTilemap != null)
                    {
                        grassTilemap.ClearAllTiles();
                    }
                    Object.DestroyImmediate(grassTransform.gameObject);
                    Debug.Log("[DungeonMap1Redesign] Removed outdoor Grass tilemap");
                }
            }

            // 3. Re-theme the Ground Tilemap with Gothic Stone Dungeon Tiles
            var groundGO = GameObject.Find("Grid/Ground") ?? GameObject.Find("Ground");
            if (groundGO != null)
            {
                var tm = groundGO.GetComponent<Tilemap>();
                if (tm != null)
                {
                    tm.CompressBounds();
                    BoundsInt bounds = tm.cellBounds;
                    Debug.Log($"[DungeonMap1Redesign] Ground tilemap initial cellBounds: {bounds}");

                    var positionsToUpdate = new List<Vector3Int>();

                    // Scan both the bounds and a wide safety window to ensure all tiles are found
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

                    Debug.Log($"[DungeonMap1Redesign] Found {positionsToUpdate.Count} occupied tiles on Ground tilemap");

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
                            // Thin floating ledge
                            chosen = platformTile ?? surfaceTile;
                        }
                        else if (!hasAbove)
                        {
                            // Ground surface
                            if (!hasLeft && wallLeftTile != null)
                                chosen = wallLeftTile;
                            else if (!hasRight && wallRightTile != null)
                                chosen = wallRightTile;
                            else
                                chosen = surfaceTile;
                        }
                        else if (!hasBelow)
                        {
                            // Ceiling underside
                            chosen = ceilingTile ?? fillTile;
                        }
                        else
                        {
                            // Solid interior stone masonry
                            if (!hasLeft && wallLeftTile != null)
                                chosen = wallLeftTile;
                            else if (!hasRight && wallRightTile != null)
                                chosen = wallRightTile;
                            else
                                chosen = fillTile;
                        }

                        posArray.Add(pos);
                        tileArray.Add(chosen);
                    }

                    // Seal left dungeon entrance boundary behind player spawn (-15)
                    for (int y = -12; y <= 8; y++)
                    {
                        posArray.Add(new Vector3Int(-15, y, 0));
                        tileArray.Add(fillTile);

                        posArray.Add(new Vector3Int(-14, y, 0));
                        tileArray.Add(wallRightTile ?? fillTile);
                    }

                    // Batch set all tiles in one atomic call
                    tm.SetTiles(posArray.ToArray(), tileArray.ToArray());
                    tm.RefreshAllTiles();
                    EditorUtility.SetDirty(tm);
                    EditorUtility.SetDirty(groundGO);
                    Debug.Log($"[DungeonMap1Redesign] Retiled Ground tilemap with {posArray.Count} gothic stone dungeon tiles via SetTiles batch");
                }
            }

            // 4. Create Dungeon Environment & Background Architecture
            var existingDungeonEnv = GameObject.Find("DungeonEnvironment");
            if (existingDungeonEnv != null)
            {
                Object.DestroyImmediate(existingDungeonEnv);
            }

            var envGO = new GameObject("DungeonEnvironment");
            envGO.transform.position = Vector3.zero;

            // 4a. Seamless Dark Gothic Stone Vault Wall (bg-5)
            var bg5Sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GothicVania Church/Environment/bg-5.png");
            if (bg5Sprite != null)
            {
                var backWallGO = new GameObject("Dungeon_BackWall");
                backWallGO.transform.SetParent(envGO.transform, false);
                backWallGO.transform.position = new Vector3(75f, 2f, 15f);

                var sr = backWallGO.AddComponent<SpriteRenderer>();
                sr.sprite = bg5Sprite;
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = new Vector2(240f, 36f);
                sr.sortingLayerName = "Background";
                sr.sortingOrder = -30;
                sr.color = new Color(0.65f, 0.65f, 0.78f, 1f);
            }

            // 4b. Gothic Arched Stained Glass Windows (bg-3) & Arches (bg-2)
            var bg3Sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GothicVania Church/Environment/bg-3.png");
            var bg2Sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GothicVania Church/Environment/bg-2.png");
            float[] archPositions = new float[] { -8f, 10f, 28f, 46f, 64f, 82f, 100f, 118f, 136f, 154f };

            for (int i = 0; i < archPositions.Length; i++)
            {
                float x = archPositions[i];
                var archSprite = (i % 2 == 0) ? bg3Sprite : bg2Sprite;
                if (archSprite != null)
                {
                    var archGO = new GameObject($"Dungeon_Arch_{i}");
                    archGO.transform.SetParent(envGO.transform, false);
                    archGO.transform.position = new Vector3(x, 1.5f, 8f);

                    var sr = archGO.AddComponent<SpriteRenderer>();
                    sr.sprite = archSprite;
                    sr.sortingLayerName = "Background";
                    sr.sortingOrder = -15;
                    sr.color = new Color(0.85f, 0.85f, 0.95f, 1f);
                }
            }

            // 4c. Grand Carved Gothic Stone Columns (column.png)
            var colSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GothicVania Church/Environment/column.png");
            float[] colPositions = new float[] { -14f, 0f, 18f, 36f, 54f, 72f, 90f, 108f, 126f, 144f, 160f };

            for (int i = 0; i < colPositions.Length; i++)
            {
                float x = colPositions[i];
                if (colSprite != null)
                {
                    var colGO = new GameObject($"Dungeon_Column_{i}");
                    colGO.transform.SetParent(envGO.transform, false);
                    colGO.transform.position = new Vector3(x, 1.0f, 2f);

                    var sr = colGO.AddComponent<SpriteRenderer>();
                    sr.sprite = colSprite;
                    sr.sortingLayerName = "Ground";
                    sr.sortingOrder = -1;
                    sr.color = new Color(0.9f, 0.9f, 0.95f, 1f);
                }
            }

            // 4d. Warm Dungeon Torches & Point Lights 2D
            var lampSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Plugins/TrapmoorDungeonTileset_v03/Sprites/lamp.png");
            float[] torchPositions = new float[] { -8f, 10f, 28f, 46f, 64f, 82f, 100f, 118f, 136f, 154f };

            for (int i = 0; i < torchPositions.Length; i++)
            {
                float x = torchPositions[i];
                var torchGO = new GameObject($"Dungeon_Torch_{i}");
                torchGO.transform.SetParent(envGO.transform, false);
                torchGO.transform.position = new Vector3(x, 1.2f, 0f);

                if (lampSprite != null)
                {
                    var sr = torchGO.AddComponent<SpriteRenderer>();
                    sr.sprite = lampSprite;
                    sr.sortingLayerName = "Player";
                    sr.sortingOrder = -1;
                }

                var light = torchGO.AddComponent<Light2D>();
                light.lightType = Light2D.LightType.Point;
                light.color = new Color(1f, 0.72f, 0.32f, 1f);
                light.intensity = 1.35f;
                light.pointLightInnerRadius = 1.2f;
                light.pointLightOuterRadius = 7.5f;
            }

            // 5. Atmospheric Dungeon Ambient Lighting & Camera Clear Color
            var globalLightGO = GameObject.Find("Global Light 2D");
            if (globalLightGO != null)
            {
                var gl = globalLightGO.GetComponent<Light2D>();
                if (gl != null)
                {
                    gl.color = new Color(0.24f, 0.26f, 0.36f, 1f);
                    gl.intensity = 0.65f;
                    EditorUtility.SetDirty(gl);
                    Debug.Log("[DungeonMap1Redesign] Configured Global Light 2D to deep gothic stone ambience");
                }
            }

            var mainCamGO = GameObject.Find("Main Camera");
            if (mainCamGO != null)
            {
                var cam = mainCamGO.GetComponent<Camera>();
                if (cam != null)
                {
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = new Color(0.04f, 0.05f, 0.08f, 1f);
                    EditorUtility.SetDirty(cam);
                    Debug.Log("[DungeonMap1Redesign] Configured Main Camera clear color to dungeon void");
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene);
            EditorSceneManager.SaveOpenScenes();
            Debug.Log($"[DungeonMap1Redesign] Successfully redesigned and saved map 1.unity! Save result: {saved}");
        }
    }
}
#endif
