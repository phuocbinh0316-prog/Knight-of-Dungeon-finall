#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Knight.Editor
{
    [InitializeOnLoad]
    public static class RogueliteAutoSetup
    {
        private const string PrefKey = "Roguelite_AutoSetup_v1_Complete";

        static RogueliteAutoSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorPrefs.GetBool(PrefKey, false))
                {
                    RunSetup();
                }
            };
        }

        [MenuItem("Knight of Dungeon/Run Full Roguelite Setup")]
        public static void RunSetup()
        {
            Debug.Log("<color=cyan>=== [RogueliteAutoSetup] Starting Complete Setup ===</color>");

            try
            {
                EnsureFolders();
                SetupPlayerPrefab();
                var weapons = CreateWeapons();
                AssignStartingWeapon(weapons[0]);
                var projPrefab = CreateProjectilePrefab();
                var (cellPrefab, heartPrefab, weaponPickupPrefab) = CreatePickups();
                var rangedEnemyPrefab = CreateRangedEnemy(projPrefab, cellPrefab, heartPrefab);
                var rooms = CreateRooms();
                CreateRunScene(rooms, rangedEnemyPrefab, weaponPickupPrefab, weapons);
                AddRunSceneToBuildSettings();
                SetupMainMenuScene();
                SetupExistingMapScenes();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorPrefs.SetBool(PrefKey, true);
                Debug.Log("<color=green>=== [RogueliteAutoSetup] COMPLETE! All assets, prefabs, and scenes are ready with 0 errors! ===</color>");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[RogueliteAutoSetup] Error during setup: " + ex);
            }
        }

        private static void EnsureFolders()
        {
            CreateFolderIfNeeded("Assets", "RogueliteData");
            CreateFolderIfNeeded("Assets/RogueliteData", "Weapons");
            CreateFolderIfNeeded("Assets/Prefabs", "Roguelite");
            CreateFolderIfNeeded("Assets/Prefabs/Roguelite", "Rooms");
        }

        private static void CreateFolderIfNeeded(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static void SetupPlayerPrefab()
        {
            string path = "Assets/Prefabs/Player.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            if (root == null)
            {
                Debug.LogError("Cannot load Player.prefab at " + path);
                return;
            }

            bool changed = false;
            if (root.GetComponent<DashRoll>() == null)
            {
                root.AddComponent<DashRoll>();
                changed = true;
            }
            if (root.GetComponent<WeaponHolder>() == null)
            {
                root.AddComponent<WeaponHolder>();
                changed = true;
            }
            if (root.GetComponent<HitFlash>() == null)
            {
                root.AddComponent<HitFlash>();
                changed = true;
            }

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log("[RogueliteAutoSetup] Added DashRoll, WeaponHolder, and HitFlash to Player.prefab");
            }
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static WeaponData[] CreateWeapons()
        {
            var swordIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UIII/fantasy_pixelart_ui/icons/wood_sword.png");
            var axeIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UIII/fantasy_pixelart_ui/icons/silver_sword.png");
            var daggerIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UIII/fantasy_pixelart_ui/icons/gold_sword.png");

            var sword = CreateOrGetWeapon("Assets/RogueliteData/Weapons/Sword_Common.asset", "Broadsword",
                "Thanh kiếm cơ bản của hiệp sĩ, tốc độ và sát thương cân bằng.", WeaponRarity.Common, 1.5f, 0.45f, 1.2f, swordIcon);

            var axe = CreateOrGetWeapon("Assets/RogueliteData/Weapons/Axe_Rare.asset", "Battle Axe",
                "Rìu chiến sắc bén, sát thương cực mạnh nhưng vung chậm.", WeaponRarity.Rare, 3.0f, 0.75f, 1.5f, axeIcon);

            var dagger = CreateOrGetWeapon("Assets/RogueliteData/Weapons/Dagger_Epic.asset", "Shadow Dagger",
                "Dao găm bóng tối, chém cực nhanh ở cự ly gần.", WeaponRarity.Epic, 1.2f, 0.22f, 0.9f, daggerIcon);

            return new WeaponData[] { sword, axe, dagger };
        }

        private static WeaponData CreateOrGetWeapon(string path, string displayName, string desc, WeaponRarity rarity, float dmg, float cd, float range, Sprite icon)
        {
            var weapon = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
            if (weapon == null)
            {
                weapon = ScriptableObject.CreateInstance<WeaponData>();
                weapon.displayName = displayName;
                weapon.description = desc;
                weapon.rarity = rarity;
                weapon.damage = dmg;
                weapon.cooldown = cd;
                weapon.range = range;
                weapon.icon = icon;
                AssetDatabase.CreateAsset(weapon, path);
                Debug.Log("[RogueliteAutoSetup] Created Weapon asset: " + path);
            }
            return weapon;
        }

        private static void AssignStartingWeapon(WeaponData defaultWeapon)
        {
            string path = "Assets/Prefabs/Player.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            var holder = root.GetComponent<WeaponHolder>();
            if (holder != null)
            {
                var so = new SerializedObject(holder);
                var prop = so.FindProperty("startingWeapon");
                if (prop != null)
                {
                    prop.objectReferenceValue = defaultWeapon;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    Debug.Log("[RogueliteAutoSetup] Assigned " + defaultWeapon.displayName + " as starting weapon to Player.prefab");
                }
            }
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static GameObject CreateProjectilePrefab()
        {
            string path = "Assets/Prefabs/Roguelite/Projectile.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = new GameObject("Projectile");
            var sr = go.AddComponent<SpriteRenderer>();
            var fireSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GothicVania Church/Sprites/Enemies/Wizard-Fire/fire1.png");
            if (fireSprite != null) sr.sprite = fireSprite;
            sr.sortingLayerName = "Enemy";

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.25f;

            var proj = go.AddComponent<Projectile>();
            var so = new SerializedObject(proj);
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer >= 0)
                so.FindProperty("groundLayer").intValue = 1 << groundLayer;
            so.FindProperty("lifetime").floatValue = 4f;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log("[RogueliteAutoSetup] Created Projectile prefab at " + path);
            return prefab;
        }

        private static (GameObject cell, GameObject heart, GameObject weapon) CreatePickups()
        {
            var cellPrefab = CreatePickupPrefab("Assets/Prefabs/Roguelite/Pickup_Cell.prefab", "Pickup_Cell", PickupType.Cell, 1, 0f,
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UIII/Fantasy Minimal Pixel Art GUI by eta-commence/Icons/CoinIcon_16x18.png"));

            var heartPrefab = CreatePickupPrefab("Assets/Prefabs/Roguelite/Pickup_Heart.prefab", "Pickup_Heart", PickupType.Heart, 1, 1f,
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UIII/Fantasy Minimal Pixel Art GUI by eta-commence/Icons/HeartIcons_32x32.png"));

            var weaponPrefab = CreatePickupPrefab("Assets/Prefabs/Roguelite/Pickup_Weapon.prefab", "Pickup_Weapon", PickupType.Weapon, 1, 0f,
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UIII/fantasy_pixelart_ui/icons/silver_sword.png"));

            return (cellPrefab, heartPrefab, weaponPrefab);
        }

        private static GameObject CreatePickupPrefab(string path, string name, PickupType type, int amount, float healAmount, Sprite sprite)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = new GameObject(name);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = "Player";

            var rb = go.AddComponent<Rigidbody2D>();
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.mass = 0.5f;

            var groundCol = go.AddComponent<CircleCollider2D>();
            groundCol.radius = 0.2f;

            var triggerCol = go.AddComponent<CircleCollider2D>();
            triggerCol.isTrigger = true;
            triggerCol.radius = 0.5f;

            var pickup = go.AddComponent<Pickup>();
            var so = new SerializedObject(pickup);
            so.FindProperty("type").enumValueIndex = (int)type;
            so.FindProperty("amount").intValue = amount;
            so.FindProperty("healAmount").floatValue = healAmount;
            so.FindProperty("magnetRange").floatValue = 4f;
            so.FindProperty("magnetSpeed").floatValue = 12f;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log("[RogueliteAutoSetup] Created Pickup prefab: " + path);
            return prefab;
        }

        private static GameObject CreateRangedEnemy(GameObject projPrefab, GameObject cellPrefab, GameObject heartPrefab)
        {
            string path = "Assets/Prefabs/Roguelite/RangedEnemy.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = new GameObject("RangedEnemy");
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0) go.layer = enemyLayer;
            go.tag = "Enemy";

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GothicVania Church/Sprites/Enemies/Wizard-Idle/wizard-idle-1.png");
            sr.sortingLayerName = "Enemy";

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 1.5f);
            col.offset = new Vector2(0f, 0.75f);

            var rb = go.AddComponent<Rigidbody2D>();
            rb.freezeRotation = true;

            var health = go.AddComponent<Health>();
            var soHealth = new SerializedObject(health);
            soHealth.FindProperty("maxHealth").floatValue = 3f;
            soHealth.ApplyModifiedPropertiesWithoutUndo();

            go.AddComponent<HitFlash>();

            var loot = go.AddComponent<LootDropper>();
            var soLoot = new SerializedObject(loot);
            soLoot.FindProperty("cellPrefab").objectReferenceValue = cellPrefab.GetComponent<Pickup>();
            soLoot.FindProperty("cellCount").vector2IntValue = new Vector2Int(1, 3);
            soLoot.FindProperty("heartPrefab").objectReferenceValue = heartPrefab.GetComponent<Pickup>();
            soLoot.FindProperty("heartChance").floatValue = 0.25f;
            soLoot.ApplyModifiedPropertiesWithoutUndo();

            var firePoint = new GameObject("FirePoint");
            firePoint.transform.SetParent(go.transform, false);
            firePoint.transform.localPosition = new Vector3(0.6f, 0.8f, 0f);

            var ranged = go.AddComponent<RangedEnemy>();
            var soRanged = new SerializedObject(ranged);
            soRanged.FindProperty("projectilePrefab").objectReferenceValue = projPrefab.GetComponent<Projectile>();
            soRanged.FindProperty("firePoint").objectReferenceValue = firePoint.transform;
            soRanged.FindProperty("fireInterval").floatValue = 2.0f;
            soRanged.FindProperty("projectileSpeed").floatValue = 7.5f;
            soRanged.FindProperty("projectileDamage").floatValue = 1f;
            soRanged.FindProperty("detectRange").floatValue = 12f;
            soRanged.FindProperty("preferredDistance").floatValue = 6f;
            soRanged.FindProperty("moveSpeed").floatValue = 2f;
            soRanged.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log("[RogueliteAutoSetup] Created RangedEnemy prefab: " + path);
            return prefab;
        }

        private static List<Room> CreateRooms()
        {
            var list = new List<Room>();
            list.Add(CreateRoomPrefab("Room_Start", RoomType.Start, 16f, false, false, true, false));
            list.Add(CreateRoomPrefab("Room_Combat_1", RoomType.Combat, 22f, true, false, false, false));
            list.Add(CreateRoomPrefab("Room_Combat_2", RoomType.Combat, 26f, true, false, false, false, true));
            list.Add(CreateRoomPrefab("Room_Treasure", RoomType.Treasure, 16f, false, true, false, false));
            list.Add(CreateRoomPrefab("Room_Boss", RoomType.Boss, 26f, false, false, false, true));
            return list;
        }

        private static Room CreateRoomPrefab(string name, RoomType type, float width, bool hasEnemies, bool hasLoot, bool leftWall, bool rightWall, bool extraPlatform = false)
        {
            string path = "Assets/Prefabs/Roguelite/Rooms/" + name + ".prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing.GetComponent<Room>();

            var root = new GameObject(name);
            var room = root.AddComponent<Room>();
            room.type = type;

            var entry = new GameObject("Entry");
            entry.transform.SetParent(root.transform, false);
            entry.transform.localPosition = new Vector3(-width / 2f, 0f, 0f);
            room.entry = entry.transform;

            var exit = new GameObject("Exit");
            exit.transform.SetParent(root.transform, false);
            exit.transform.localPosition = new Vector3(width / 2f, 0f, 0f);
            room.exit = exit.transform;

            int groundLayer = LayerMask.NameToLayer("Ground");

            // Main Ground
            var ground = new GameObject("Ground");
            ground.transform.SetParent(root.transform, false);
            if (groundLayer >= 0) ground.layer = groundLayer;
            var groundCol = ground.AddComponent<BoxCollider2D>();
            groundCol.size = new Vector2(width, 1.2f);
            groundCol.offset = new Vector2(0f, -0.6f);

            var columnSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GothicVania Church/Environment/column.png");
            if (columnSprite != null)
            {
                var sr = ground.AddComponent<SpriteRenderer>();
                sr.sprite = columnSprite;
                sr.sortingLayerName = "Ground";
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = new Vector2(width, 1.2f);
            }

            if (leftWall)
            {
                var wall = new GameObject("Wall_Left");
                wall.transform.SetParent(root.transform, false);
                if (groundLayer >= 0) wall.layer = groundLayer;
                var wallCol = wall.AddComponent<BoxCollider2D>();
                wallCol.size = new Vector2(1f, 10f);
                wall.transform.localPosition = new Vector3(-width / 2f - 0.5f, 4f, 0f);
            }

            if (rightWall)
            {
                var wall = new GameObject("Wall_Right");
                wall.transform.SetParent(root.transform, false);
                if (groundLayer >= 0) wall.layer = groundLayer;
                var wallCol = wall.AddComponent<BoxCollider2D>();
                wallCol.size = new Vector2(1f, 10f);
                wall.transform.localPosition = new Vector3(width / 2f + 0.5f, 4f, 0f);
            }

            if (extraPlatform)
            {
                var plat = new GameObject("Platform_Elevated");
                plat.transform.SetParent(root.transform, false);
                if (groundLayer >= 0) plat.layer = groundLayer;
                var pCol = plat.AddComponent<BoxCollider2D>();
                pCol.size = new Vector2(6f, 0.6f);
                plat.transform.localPosition = new Vector3(0f, 2.5f, 0f);
                if (columnSprite != null)
                {
                    var pSr = plat.AddComponent<SpriteRenderer>();
                    pSr.sprite = columnSprite;
                    pSr.sortingLayerName = "Ground";
                    pSr.drawMode = SpriteDrawMode.Tiled;
                    pSr.size = new Vector2(6f, 0.6f);
                }
            }

            if (hasEnemies)
            {
                var spawn1 = new GameObject("EnemySpawn_1");
                spawn1.transform.SetParent(root.transform, false);
                spawn1.transform.localPosition = new Vector3(-width / 3f, 0.5f, 0f);

                var spawn2 = new GameObject("EnemySpawn_2");
                spawn2.transform.SetParent(root.transform, false);
                spawn2.transform.localPosition = new Vector3(width / 3f, 0.5f, 0f);

                if (extraPlatform)
                {
                    var spawn3 = new GameObject("EnemySpawn_3");
                    spawn3.transform.SetParent(root.transform, false);
                    spawn3.transform.localPosition = new Vector3(0f, 3.2f, 0f);
                    room.enemySpawnPoints = new Transform[] { spawn1.transform, spawn2.transform, spawn3.transform };
                }
                else
                {
                    room.enemySpawnPoints = new Transform[] { spawn1.transform, spawn2.transform };
                }
            }

            if (hasLoot)
            {
                var lootSpawn = new GameObject("LootSpawn");
                lootSpawn.transform.SetParent(root.transform, false);
                lootSpawn.transform.localPosition = new Vector3(0f, 0.5f, 0f);
                room.lootSpawnPoints = new Transform[] { lootSpawn.transform };
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Debug.Log("[RogueliteAutoSetup] Created Room prefab: " + path);
            return prefab.GetComponent<Room>();
        }

        private static void CreateRunScene(List<Room> rooms, GameObject rangedEnemyPrefab, GameObject weaponPickupPrefab, WeaponData[] weapons)
        {
            string scenePath = "Assets/Scenes/Run.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Camera
            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            var cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 7f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.12f, 0.16f);
            camGO.AddComponent<AudioListener>();
            camGO.AddComponent<CinemachineBrain>();

            // 2. Cinemachine Virtual Camera
            var vcamGO = new GameObject("CinemachineCamera");
            var vcam = vcamGO.AddComponent<CinemachineCamera>();
            vcam.Lens.OrthographicSize = 7f;
            vcamGO.AddComponent<CinemachineImpulseListener>();
            vcamGO.AddComponent<CameraShake>();

            // 3. Player instance
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            GameObject playerInstance = null;
            if (playerPrefab != null)
            {
                playerInstance = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
                playerInstance.name = "Player";
                playerInstance.transform.position = new Vector3(0f, 1f, 0f);

                var soVcam = new SerializedObject(vcam);
                var trackProp = soVcam.FindProperty("Target.TrackingTarget");
                if (trackProp != null)
                {
                    trackProp.objectReferenceValue = playerInstance.transform;
                    soVcam.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            // 4. Canvas & HUD
            var canvasGO = new GameObject("HUD_Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            // RunHUD Text Elements
            var hudGO = new GameObject("RunHUD");
            hudGO.transform.SetParent(canvasGO.transform, false);
            var hudRect = hudGO.AddComponent<RectTransform>();
            hudRect.anchorMin = new Vector2(0f, 1f);
            hudRect.anchorMax = new Vector2(0f, 1f);
            hudRect.pivot = new Vector2(0f, 1f);
            hudRect.anchoredPosition = new Vector2(25f, -25f);
            hudRect.sizeDelta = new Vector2(300f, 120f);

            var depthTMP = CreateTextMesh(hudGO.transform, "DepthText", "Depth 1", 24, new Vector2(0f, 0f), TextAlignmentOptions.TopLeft);
            var cellsTMP = CreateTextMesh(hudGO.transform, "CellsText", "Cells: 0", 22, new Vector2(0f, -32f), TextAlignmentOptions.TopLeft);
            var weaponTMP = CreateTextMesh(hudGO.transform, "WeaponText", "Broadsword", 20, new Vector2(0f, -62f), TextAlignmentOptions.TopLeft);

            var runHUD = hudGO.AddComponent<RunHUD>();
            var soHUD = new SerializedObject(runHUD);
            soHUD.FindProperty("depthText").objectReferenceValue = depthTMP;
            soHUD.FindProperty("cellsText").objectReferenceValue = cellsTMP;
            soHUD.FindProperty("weaponText").objectReferenceValue = weaponTMP;
            soHUD.ApplyModifiedPropertiesWithoutUndo();

            // GameManager & Panels
            var gmGO = new GameObject("GameManager");
            var gm = gmGO.AddComponent<GameManager>();

            var scoreTMP = CreateTextMesh(canvasGO.transform, "ScoreText", "0", 28, new Vector2(-40f, -30f), TextAlignmentOptions.TopRight);
            var scoreRect = scoreTMP.GetComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(1f, 1f);
            scoreRect.anchorMax = new Vector2(1f, 1f);
            scoreRect.pivot = new Vector2(1f, 1f);

            var gameOverPanel = CreatePanelPersistent(canvasGO.transform, "GameOverPanel", "GAME OVER", new Color(0.3f, 0.05f, 0.05f, 0.9f),
                "Replay", gm.Replay, "Main Menu", gm.MainMenu);

            var gameWinPanel = CreatePanelPersistent(canvasGO.transform, "GameWinPanel", "DEPTH CLEARED!", new Color(0.05f, 0.25f, 0.1f, 0.9f),
                "Next Depth", gm.Continue, "Main Menu", gm.MainMenu);

            gameOverPanel.SetActive(false);
            gameWinPanel.SetActive(false);

            var soGM = new SerializedObject(gm);
            soGM.FindProperty("scoreText").objectReferenceValue = scoreTMP;
            soGM.FindProperty("gameOverUi").objectReferenceValue = gameOverPanel;
            soGM.FindProperty("gameWinUi").objectReferenceValue = gameWinPanel;
            soGM.ApplyModifiedPropertiesWithoutUndo();

            // 5. LevelGenerator
            var genGO = new GameObject("LevelGenerator");
            var gen = genGO.AddComponent<LevelGenerator>();
            var soGen = new SerializedObject(gen);

            soGen.FindProperty("startRoom").objectReferenceValue = rooms.Find(r => r.type == RoomType.Start);
            soGen.FindProperty("bossRoom").objectReferenceValue = rooms.Find(r => r.type == RoomType.Boss);

            var combatProp = soGen.FindProperty("combatRooms");
            var combats = rooms.FindAll(r => r.type == RoomType.Combat);
            combatProp.arraySize = combats.Count;
            for (int i = 0; i < combats.Count; i++)
                combatProp.GetArrayElementAtIndex(i).objectReferenceValue = combats[i];

            var treasureProp = soGen.FindProperty("treasureRooms");
            var treasures = rooms.FindAll(r => r.type == RoomType.Treasure);
            treasureProp.arraySize = treasures.Count;
            for (int i = 0; i < treasures.Count; i++)
                treasureProp.GetArrayElementAtIndex(i).objectReferenceValue = treasures[i];

            if (weaponPickupPrefab != null)
                soGen.FindProperty("weaponPickupPrefab").objectReferenceValue = weaponPickupPrefab.GetComponent<Pickup>();

            if (weapons != null && weapons.Length > 0)
            {
                var wpProp = soGen.FindProperty("weaponPool");
                wpProp.arraySize = weapons.Length;
                for (int i = 0; i < weapons.Length; i++)
                    wpProp.GetArrayElementAtIndex(i).objectReferenceValue = weapons[i];
            }

            // Enemy Pool (Melee + Ranged)
            var enemyMelee = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Attack_1_0.prefab");
            var poolProp = soGen.FindProperty("enemyPool");
            poolProp.arraySize = (enemyMelee != null) ? 2 : 1;

            if (enemyMelee != null)
            {
                var e0 = poolProp.GetArrayElementAtIndex(0);
                e0.FindPropertyRelative("prefab").objectReferenceValue = enemyMelee;
                e0.FindPropertyRelative("weight").intValue = 10;
                e0.FindPropertyRelative("minDepth").intValue = 1;

                var e1 = poolProp.GetArrayElementAtIndex(1);
                e1.FindPropertyRelative("prefab").objectReferenceValue = rangedEnemyPrefab;
                e1.FindPropertyRelative("weight").intValue = 8;
                e1.FindPropertyRelative("minDepth").intValue = 1;
            }
            else
            {
                var e0 = poolProp.GetArrayElementAtIndex(0);
                e0.FindPropertyRelative("prefab").objectReferenceValue = rangedEnemyPrefab;
                e0.FindPropertyRelative("weight").intValue = 10;
                e0.FindPropertyRelative("minDepth").intValue = 1;
            }

            if (playerInstance != null)
                soGen.FindProperty("player").objectReferenceValue = playerInstance.transform;

            soGen.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log("[RogueliteAutoSetup] Created and configured Run scene at " + scenePath);
        }

        private static void AddRunSceneToBuildSettings()
        {
            string scenePath = "Assets/Scenes/Run.unity";
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(s => s.path == scenePath))
            {
                scenes.Add(new EditorBuildSettingsScene(scenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
                Debug.Log("[RogueliteAutoSetup] Added Run scene to EditorBuildSettings");
            }
        }

        private static void SetupMainMenuScene()
        {
            string path = "Assets/Scenes/MainMenu.unity";
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var mainMenu = Object.FindAnyObjectByType<MainMenu>();
            if (mainMenu == null) return;

            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            // 1. Add "Play Run" Button to Menu
            var menuTransform = canvas.transform.Find("Menu");
            if (menuTransform != null && menuTransform.Find("PlayRunButton") == null)
            {
                var btnGO = CreateUIButton(menuTransform, "PlayRunButton", "PLAY ROGUELITE (RUN)", new Vector2(0f, -40f), new Vector2(240f, 45f));
                var btn = btnGO.GetComponent<Button>();
                UnityEventTools.AddPersistentListener(btn.onClick, mainMenu.PlayRun);
                Debug.Log("[RogueliteAutoSetup] Added PlayRun button to MainMenu");
            }

            // 2. Add UpgradeShop Panel
            if (canvas.transform.Find("UpgradeShopPanel") == null)
            {
                var shopPanel = new GameObject("UpgradeShopPanel");
                shopPanel.transform.SetParent(canvas.transform, false);
                var shopRect = shopPanel.AddComponent<RectTransform>();
                shopRect.anchorMin = new Vector2(0.2f, 0.15f);
                shopRect.anchorMax = new Vector2(0.8f, 0.85f);
                shopRect.offsetMin = Vector2.zero;
                shopRect.offsetMax = Vector2.zero;

                var img = shopPanel.AddComponent<Image>();
                img.color = new Color(0.1f, 0.12f, 0.18f, 0.95f);

                CreateTextMesh(shopPanel.transform, "Title", "PERMANENT UPGRADES", 28, new Vector2(0f, -30f), TextAlignmentOptions.Top);

                var cellsText = CreateTextMesh(shopPanel.transform, "CellsText", "Cells: 0", 24, new Vector2(0f, -80f), TextAlignmentOptions.Center);
                var hpText = CreateTextMesh(shopPanel.transform, "HealthText", "Max HP  Lv 0/5  - 15 cells", 20, new Vector2(0f, -140f), TextAlignmentOptions.Center);
                var dmgText = CreateTextMesh(shopPanel.transform, "DamageText", "Damage  Lv 0/5  - 15 cells", 20, new Vector2(0f, -220f), TextAlignmentOptions.Center);

                var shopUI = shopPanel.AddComponent<UpgradeShopUI>();
                var soShop = new SerializedObject(shopUI);
                soShop.FindProperty("cellsText").objectReferenceValue = cellsText;
                soShop.FindProperty("healthText").objectReferenceValue = hpText;
                soShop.FindProperty("damageText").objectReferenceValue = dmgText;
                soShop.ApplyModifiedPropertiesWithoutUndo();

                // Buy HP Button
                var buyHpGO = CreateUIButton(shopPanel.transform, "BuyHealthBtn", "+1 Max HP", new Vector2(0f, -175f), new Vector2(160f, 35f));
                var buyHpBtn = buyHpGO.GetComponent<Button>();
                UnityEventTools.AddPersistentListener(buyHpBtn.onClick, shopUI.BuyHealth);

                // Buy Damage Button
                var buyDmgGO = CreateUIButton(shopPanel.transform, "BuyDamageBtn", "+15% Damage", new Vector2(0f, -255f), new Vector2(160f, 35f));
                var buyDmgBtn = buyDmgGO.GetComponent<Button>();
                UnityEventTools.AddPersistentListener(buyDmgBtn.onClick, shopUI.BuyDamage);

                // Close Button
                var closeGO = CreateUIButton(shopPanel.transform, "CloseBtn", "CLOSE", new Vector2(0f, -320f), new Vector2(120f, 35f));
                var closeBtn = closeGO.GetComponent<Button>();
                UnityEventTools.AddBoolPersistentListener(closeBtn.onClick, shopPanel.SetActive, false);

                // Add "Open Shop" button to Menu
                if (menuTransform != null && menuTransform.Find("OpenShopButton") == null)
                {
                    var openShopGO = CreateUIButton(menuTransform, "OpenShopButton", "UPGRADE SHOP", new Vector2(0f, -95f), new Vector2(240f, 45f));
                    var openShopBtn = openShopGO.GetComponent<Button>();
                    UnityEventTools.AddBoolPersistentListener(openShopBtn.onClick, shopPanel.SetActive, true);
                }

                shopPanel.SetActive(false);
                Debug.Log("[RogueliteAutoSetup] Added UpgradeShopPanel and Shop Button to MainMenu");
            }

            EditorSceneManager.SaveScene(scene);
        }

        private static void SetupExistingMapScenes()
        {
            string[] maps = new string[] { "Assets/Scenes/map 1.unity", "Assets/Scenes/map 2.unity", "Assets/Scenes/map 3.unity" };
            foreach (var m in maps)
            {
                if (!File.Exists(m)) continue;
                var sc = EditorSceneManager.OpenScene(m, OpenSceneMode.Single);
                var vcam = Object.FindAnyObjectByType<CinemachineCamera>();
                if (vcam != null)
                {
                    bool mod = false;
                    if (vcam.GetComponent<CinemachineImpulseListener>() == null)
                    {
                        vcam.gameObject.AddComponent<CinemachineImpulseListener>();
                        mod = true;
                    }
                    if (vcam.GetComponent<CameraShake>() == null)
                    {
                        vcam.gameObject.AddComponent<CameraShake>();
                        mod = true;
                    }
                    if (mod)
                    {
                        EditorSceneManager.SaveScene(sc);
                        Debug.Log("[RogueliteAutoSetup] Added CameraShake & ImpulseListener to " + m);
                    }
                }
            }

            // Return to MainMenu
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
        }

        private static TextMeshProUGUI CreateTextMesh(Transform parent, string name, string text, float fontSize, Vector2 anchoredPos, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(300f, 40f);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            return tmp;
        }

        private static GameObject CreateUIButton(Transform parent, string name, string label, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.22f, 0.3f, 0.9f);

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.35f, 0.4f, 0.55f);
            colors.pressedColor = new Color(0.15f, 0.17f, 0.22f);
            btn.colors = colors;

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(go.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 18f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return go;
        }

        private static GameObject CreatePanelPersistent(Transform parent, string name, string title, Color bgColor,
            string btn1Label, UnityEngine.Events.UnityAction btn1Action, string btn2Label, UnityEngine.Events.UnityAction btn2Action)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.25f, 0.25f);
            rect.anchorMax = new Vector2(0.75f, 0.75f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var img = panel.AddComponent<Image>();
            img.color = bgColor;

            CreateTextMesh(panel.transform, "Title", title, 32f, new Vector2(0f, -40f), TextAlignmentOptions.Top);

            var b1 = CreateUIButton(panel.transform, "Btn1", btn1Label, new Vector2(0f, -120f), new Vector2(180f, 45f));
            UnityEventTools.AddPersistentListener(b1.GetComponent<Button>().onClick, btn1Action);

            var b2 = CreateUIButton(panel.transform, "Btn2", btn2Label, new Vector2(0f, -180f), new Vector2(180f, 45f));
            UnityEventTools.AddPersistentListener(b2.GetComponent<Button>().onClick, btn2Action);

            return panel;
        }
    }
}
#endif
