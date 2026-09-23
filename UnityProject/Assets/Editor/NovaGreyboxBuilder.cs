using System.IO;
using NovaStriker.CameraSystem;
using NovaStriker.Combat;
using NovaStriker.Commerce;
using NovaStriker.Data;
using NovaStriker.Debugging;
using NovaStriker.Enemies;
using NovaStriker.InputSystemIntegration;
using NovaStriker.Player;
using NovaStriker.Platform;
using NovaStriker.Presentation;
using NovaStriker.Progression;
using NovaStriker.Session;
using NovaStriker.Traversal;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NovaStriker.EditorTools
{
    /// <summary>
    /// Builds the first Unity mechanics lab entirely from repository code.
    ///
    /// Menu:
    /// Nova Striker > Greybox > Build / Refresh Mechanics Lab
    ///
    /// Generated assets live under Assets/Greybox/Generated and can be
    /// replaced freely while the gameplay architecture remains stable.
    /// </summary>
    public static class NovaGreyboxBuilder
    {
        private const string GreyboxRoot = "Assets/Greybox";
        private const string GeneratedRoot = GreyboxRoot + "/Generated";
        private const string SceneRoot = GreyboxRoot + "/Scenes";

        private const string ScenePath =
            SceneRoot + "/NovaMechanicsGreybox.unity";

        private const string PlayerPrefabPath =
            GeneratedRoot + "/NovaGreybox.prefab";

        private const string ProjectilePrefabPath =
            GeneratedRoot + "/ProjectileGreybox.prefab";

        private const string PulseWeaponPath =
            GeneratedRoot + "/Weapon_Pulse.asset";

        private const string NovaMaterialPath =
            GeneratedRoot + "/MAT_Greybox_Nova.mat";

        private const string EchoMaterialPath =
            GeneratedRoot + "/MAT_Greybox_Echo.mat";

        private const string WorldMaterialPath =
            GeneratedRoot + "/MAT_Greybox_World.mat";

        private const string OneWayMaterialPath =
            GeneratedRoot + "/MAT_Greybox_OneWay.mat";

        private const string EnemyMaterialPath =
            GeneratedRoot + "/MAT_Greybox_Enemy.mat";

        private const string ProjectileMaterialPath =
            GeneratedRoot + "/MAT_Greybox_Projectile.mat";

        private const string CommerceCatalogPath =
            GeneratedRoot + "/CommerceCatalog.asset";

        [MenuItem(
            "Nova Striker/Greybox/Build / Refresh Mechanics Lab",
            priority = 1)]
        public static void BuildGreybox()
        {
            EnsureFolders();

            int worldLayer = EnsureLayer("World");
            int oneWayLayer = EnsureLayer("OneWay");
            int playerLayer = EnsureLayer("Player");
            int enemyLayer = EnsureLayer("Enemy");
            int playerProjectileLayer = EnsureLayer("PlayerProjectile");
            int enemyProjectileLayer = EnsureLayer("EnemyProjectile");
            int grapplePointLayer = EnsureLayer("GrapplePoint");

            Material novaMaterial = CreateOrLoadMaterial(
                NovaMaterialPath,
                new Color(0.12f, 0.58f, 0.95f)
            );

            Material echoMaterial = CreateOrLoadMaterial(
                EchoMaterialPath,
                new Color(0.96f, 0.66f, 0.12f)
            );

            Material worldMaterial = CreateOrLoadMaterial(
                WorldMaterialPath,
                new Color(0.12f, 0.16f, 0.22f)
            );

            Material oneWayMaterial = CreateOrLoadMaterial(
                OneWayMaterialPath,
                new Color(0.18f, 0.75f, 0.88f)
            );

            Material enemyMaterial = CreateOrLoadMaterial(
                EnemyMaterialPath,
                new Color(0.82f, 0.22f, 0.28f)
            );

            Material projectileMaterial = CreateOrLoadMaterial(
                ProjectileMaterialPath,
                new Color(0.85f, 0.96f, 1f)
            );

            CommerceCatalog commerceCatalog =
                CreateOrLoadCommerceCatalog();

            WeaponDefinition[] weapons =
                CreateOrLoadWeapons();

            GuardianDefinition[] guardians =
                CreateOrLoadGuardians();

            WeaponDefinition pulse = weapons[0];

            Projectile2D projectilePrefab = CreateProjectilePrefab(
                projectileMaterial,
                playerProjectileLayer
            );

            GameObject playerPrefab = CreatePlayerPrefab(
                novaMaterial,
                echoMaterial,
                pulse,
                weapons,
                guardians,
                projectilePrefab,
                worldLayer,
                oneWayLayer,
                playerLayer,
                enemyLayer,
                enemyProjectileLayer,
                grapplePointLayer
            );

            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single
            );

            CreatePersistentServices(
                commerceCatalog
            );

            GameObject sessionRoot =
                CreateSessionBootstrap(
                    enemyLayer
                );

            StrikeTeamSession teamSession =
                sessionRoot.GetComponent<StrikeTeamSession>();

            LocalStrikeTeamInputCoordinator inputCoordinator =
                sessionRoot.GetComponent<LocalStrikeTeamInputCoordinator>();

            CreateCamera();
            CreateLight();

            CreateWorldBox(
                "Floor",
                new Vector3(0f, -4f, 0f),
                new Vector3(21f, 1f, 1f),
                worldMaterial,
                worldLayer
            );

            CreateWorldBox(
                "LeftWall",
                new Vector3(-10.5f, 0f, 0f),
                new Vector3(1f, 9f, 1f),
                worldMaterial,
                worldLayer
            );

            CreateWorldBox(
                "RightWall",
                new Vector3(10.5f, 0f, 0f),
                new Vector3(1f, 9f, 1f),
                worldMaterial,
                worldLayer
            );

            CreateOneWayPlatform(
                "OneWay_Low",
                new Vector3(-4.3f, -1.6f, 0f),
                new Vector3(3.6f, 0.3f, 1f),
                oneWayMaterial,
                oneWayLayer
            );

            CreateOneWayPlatform(
                "OneWay_Mid",
                new Vector3(0.6f, 0.15f, 0f),
                new Vector3(4.2f, 0.3f, 1f),
                oneWayMaterial,
                oneWayLayer
            );

            CreateOneWayPlatform(
                "OneWay_High",
                new Vector3(5.1f, 2.05f, 0f),
                new Vector3(3.4f, 0.3f, 1f),
                oneWayMaterial,
                oneWayLayer
            );

            // Echo traversal test geometry. These are normal World solids,
            // not designated grapple markers: any eligible solid surface above
            // Echo can be acquired by the grapple system.
            CreateWorldBox(
                "TraversalBeam_Left",
                new Vector3(-4.8f, 2.9f, 0f),
                new Vector3(3.0f, 0.35f, 1f),
                worldMaterial,
                worldLayer
            );

            CreateWorldBox(
                "TraversalBeam_Center",
                new Vector3(0.3f, 4.0f, 0f),
                new Vector3(3.2f, 0.35f, 1f),
                worldMaterial,
                worldLayer
            );

            CreateWorldBox(
                "TraversalBeam_Right",
                new Vector3(6.4f, 3.45f, 0f),
                new Vector3(3.2f, 0.35f, 1f),
                worldMaterial,
                worldLayer
            );

            GameObject[] players = new GameObject[4];
            NovaInputSystemAdapter[] playerInputs =
                new NovaInputSystemAdapter[4];

            Vector3[] playerPositions =
            {
                new(-7.4f, -2.55f, 0f),
                new(-6.5f, -2.55f, 0f),
                new(-5.6f, -2.55f, 0f),
                new(-4.7f, -2.55f, 0f)
            };

            StrikeTeamRole[] labRoles =
            {
                StrikeTeamRole.Striker1,
                StrikeTeamRole.Striker0,
                StrikeTeamRole.Tank,
                StrikeTeamRole.Support
            };

            StrikerCharacter[] labCharacters =
            {
                StrikerCharacter.Nova,
                StrikerCharacter.Echo,
                StrikerCharacter.Nova,
                StrikerCharacter.Echo
            };

            for (int i = 0; i < players.Length; i++)
            {
                GameObject player =
                    (GameObject)PrefabUtility.InstantiatePrefab(
                        playerPrefab
                    );

                players[i] = player;
                player.name = $"Striker_Player{i + 1}";
                player.transform.position =
                    playerPositions[i];

                Damageable2D health =
                    player.GetComponent<Damageable2D>();

                NovaMotor2D motor =
                    player.GetComponent<NovaMotor2D>();

                NovaCombatController combat =
                    player.GetComponent<NovaCombatController>();

                StrikerPlayerIdentity identity =
                    player.GetComponent<StrikerPlayerIdentity>();

                NovaInputSystemAdapter input =
                    player.GetComponent<NovaInputSystemAdapter>();

                SetInt(health, "actorId", i);
                SetInt(motor, "playerId", i);
                SetInt(combat, "playerId", i);
                SetEnum(
                    combat,
                    "character",
                    (int)labCharacters[i]
                );

                identity.Configure(
                    i,
                    labRoles[i],
                    teamSession
                );

                player
                    .GetComponent<CosmeticLoadoutController>()
                    ?.ConfigurePlayerSlot(i);

                input.ConfigureSlot(
                    i,
                    i == 0
                );

                playerInputs[i] = input;
            }

            inputCoordinator.Configure(playerInputs);

            GameObject player = players[0];

            CreateDamageDummy(
                "Target_Light",
                101,
                new Vector3(2.9f, -2.8f, 0f),
                100f,
                enemyMaterial,
                enemyLayer
            );

            CreateDamageDummy(
                "Target_Heavy",
                102,
                new Vector3(6.1f, -2.65f, 0f),
                240f,
                enemyMaterial,
                enemyLayer,
                new Vector3(1.15f, 1.7f, 0.9f)
            );

            CreateSkirmisherEnemy(
                "Enemy_Skirmisher",
                110,
                new Vector3(-0.8f, -2.75f, 0f),
                player.transform,
                projectilePrefab,
                enemyMaterial,
                enemyLayer,
                worldLayer,
                oneWayLayer
            );

            CreateFlankerEnemy(
                "Enemy_Flanker",
                111,
                new Vector3(-3.8f, -2.80f, 0f),
                player.transform,
                projectilePrefab,
                enemyMaterial,
                enemyLayer,
                worldLayer,
                oneWayLayer
            );

            CreateArtilleryEnemy(
                "Enemy_Artillery",
                112,
                new Vector3(7.4f, -2.72f, 0f),
                player.transform,
                projectilePrefab,
                enemyMaterial,
                enemyLayer,
                worldLayer,
                oneWayLayer
            );

            CreateAerialEnemy(
                "Enemy_Aerial_HoverTest",
                113,
                new Vector3(-1.7f, 2.0f, 0f),
                AerialMotionPattern.HoverBob,
                player.transform,
                projectilePrefab,
                enemyMaterial,
                enemyLayer,
                worldLayer,
                oneWayLayer
            );

            CreateAerialEnemy(
                "Enemy_Aerial_OrbitTest",
                114,
                new Vector3(4.4f, 3.0f, 0f),
                AerialMotionPattern.Orbit,
                player.transform,
                projectilePrefab,
                enemyMaterial,
                enemyLayer,
                worldLayer,
                oneWayLayer
            );

            CreateHostileEmitter(
                new Vector3(9.2f, 1.0f, 0f),
                player.transform,
                projectilePrefab,
                enemyMaterial,
                enemyLayer
            );

            NovaMotor2D motor =
                player.GetComponent<NovaMotor2D>();

            NovaCombatController combat =
                player.GetComponent<NovaCombatController>();

            Damageable2D playerHealth =
                player.GetComponent<Damageable2D>();

            GameObject hudObject = new("Greybox_HUD");
            GreyboxHUD hud = hudObject.AddComponent<GreyboxHUD>();
            hud.Configure(motor, combat, playerHealth);

            hudObject.AddComponent<StrikeTeamDebugHUD>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = player;

            Debug.Log(
                "Nova Striker greybox created: " +
                ScenePath +
                "\nPress Play to test keyboard or connected gamepad input."
            );

#if !ENABLE_INPUT_SYSTEM
            Debug.LogWarning(
                "Unity Input System package is installed, but the new input " +
                "backend is not active. In Project Settings > Player, set " +
                "Active Input Handling to Input System Package (New) or Both, " +
                "then restart the Editor."
            );

            SettingsService.OpenProjectSettings("Project/Player");
#endif
        }

        [MenuItem(
            "Nova Striker/Greybox/Open Mechanics Lab",
            priority = 2)]
        public static void OpenGreybox()
        {
            if (
                !AssetDatabase.LoadAssetAtPath<SceneAsset>(
                    ScenePath
                )
            )
            {
                BuildGreybox();
                return;
            }

            EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Single
            );
        }

        private static void EnsureFolders()
        {
            string projectRoot =
                Path.GetDirectoryName(
                    Application.dataPath
                ) ?? Application.dataPath;

            Directory.CreateDirectory(
                Path.Combine(projectRoot, GreyboxRoot)
            );

            Directory.CreateDirectory(
                Path.Combine(projectRoot, GeneratedRoot)
            );

            Directory.CreateDirectory(
                Path.Combine(projectRoot, SceneRoot)
            );

            AssetDatabase.Refresh();
        }

        private static int EnsureLayer(string layerName)
        {
            int existing = LayerMask.NameToLayer(layerName);

            if (existing >= 0)
                return existing;

            Object tagManagerAsset =
                AssetDatabase.LoadAllAssetsAtPath(
                    "ProjectSettings/TagManager.asset"
                )[0];

            SerializedObject tagManager =
                new(tagManagerAsset);

            SerializedProperty layers =
                tagManager.FindProperty("layers");

            for (int i = 8; i < 32; i++)
            {
                SerializedProperty layer =
                    layers.GetArrayElementAtIndex(i);

                if (!string.IsNullOrEmpty(layer.stringValue))
                    continue;

                layer.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                return i;
            }

            throw new System.InvalidOperationException(
                "No free user layer is available for " +
                layerName
            );
        }

        private static CommerceCatalog CreateOrLoadCommerceCatalog()
        {
            CommerceCatalog catalog =
                AssetDatabase.LoadAssetAtPath<CommerceCatalog>(
                    CommerceCatalogPath
                );

            if (!catalog)
            {
                catalog =
                    ScriptableObject.CreateInstance<CommerceCatalog>();

                AssetDatabase.CreateAsset(
                    catalog,
                    CommerceCatalogPath
                );
            }

            catalog.Products.Clear();

            catalog.Products.Add(
                new CommerceProductDefinition
                {
                    ProductId = "cosmetic.nova.sentinel-obsidian",
                    EntitlementId = "cosmetic.suit.nova.sentinel-obsidian",
                    DisplayName = "Sentinel Obsidian Suit",
                    Type = CommerceProductType.Cosmetic,
                    Restorable = true,
                    GameplayStatPurchase = false
                }
            );

            catalog.Products.Add(
                new CommerceProductDefinition
                {
                    ProductId = "cosmetic.echo.pursuit-amberglass",
                    EntitlementId = "cosmetic.suit.echo.pursuit-amberglass",
                    DisplayName = "Pursuit Amberglass Suit",
                    Type = CommerceProductType.Cosmetic,
                    Restorable = true,
                    GameplayStatPurchase = false
                }
            );

            catalog.Products.Add(
                new CommerceProductDefinition
                {
                    ProductId = "cosmetic.team.prismatic-trail",
                    EntitlementId = "cosmetic.trail.team.prismatic",
                    DisplayName = "Prismatic Strike Trail",
                    Type = CommerceProductType.Cosmetic,
                    Restorable = true,
                    GameplayStatPurchase = false
                }
            );

            catalog.Products.Add(
                new CommerceProductDefinition
                {
                    ProductId = "cosmetic.weapon.energy-chrome",
                    EntitlementId = "cosmetic.weapon.energy-chrome",
                    DisplayName = "Energy Chrome Weapon Finish",
                    Type = CommerceProductType.Cosmetic,
                    Restorable = true,
                    GameplayStatPurchase = false
                }
            );

            catalog.Products.Add(
                new CommerceProductDefinition
                {
                    ProductId = "dlc.expansion.01",
                    EntitlementId = "dlc.expansion.01",
                    DisplayName = "Expansion Slot 01",
                    Type = CommerceProductType.Dlc,
                    Restorable = true,
                    GameplayStatPurchase = false
                }
            );

            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static Material CreateOrLoadMaterial(
            string path,
            Color color)
        {
            Material existing =
                AssetDatabase.LoadAssetAtPath<Material>(
                    path
                );

            if (existing)
            {
                existing.color = color;
                EditorUtility.SetDirty(existing);
                return existing;
            }

            Shader shader =
                Shader.Find("Standard") ??
                Shader.Find("Universal Render Pipeline/Lit") ??
                Shader.Find("Unlit/Color");

            Material material = new(shader)
            {
                color = color
            };

            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static WeaponDefinition[] CreateOrLoadWeapons()
        {
            return new[]
            {
                CreateOrLoadWeapon(
                    "pulse", "Pulse", WeaponBehavior.Standard,
                    780f, new[] { 8f, 16f, 30f, 52f },
                    new[] { 4f, 7f, 11f, 18f }, "#61dcff"
                ),
                CreateOrLoadWeapon(
                    "arc", "Arc Fan", WeaponBehavior.Spread,
                    700f, new[] { 6f, 11f, 20f, 35f },
                    new[] { 4f, 5f, 7f, 11f }, "#b9f46f"
                ),
                CreateOrLoadWeapon(
                    "rail", "Rail Lance", WeaponBehavior.Pierce,
                    1120f, new[] { 12f, 23f, 42f, 72f },
                    new[] { 3f, 4f, 6f, 9f }, "#e7fbff"
                ),
                CreateOrLoadWeapon(
                    "volt", "Volt Disc", WeaponBehavior.Boomerang,
                    590f, new[] { 8f, 16f, 29f, 49f },
                    new[] { 7f, 10f, 14f, 20f }, "#ffe66f"
                ),
                CreateOrLoadWeapon(
                    "cryo", "Cryo Burst", WeaponBehavior.Cryo,
                    560f, new[] { 7f, 13f, 23f, 39f },
                    new[] { 7f, 11f, 17f, 26f }, "#91ddff"
                ),
                CreateOrLoadWeapon(
                    "nova", "Nova Beam", WeaponBehavior.Beam,
                    1350f, new[] { 10f, 21f, 40f, 76f },
                    new[] { 4f, 6f, 10f, 15f }, "#fff29a"
                ),
                CreateOrLoadWeapon(
                    "spear", "Photon Spear", WeaponBehavior.Spear,
                    970f, new[] { 13f, 24f, 43f, 70f },
                    new[] { 3f, 5f, 8f, 12f }, "#dcf8ff"
                ),
                CreateOrLoadWeapon(
                    "gravity", "Gravity Well", WeaponBehavior.Gravity,
                    460f, new[] { 6f, 12f, 20f, 34f },
                    new[] { 9f, 14f, 22f, 34f }, "#b477ff"
                ),
                CreateOrLoadWeapon(
                    "magma", "Magma Talon", WeaponBehavior.Magma,
                    620f, new[] { 9f, 18f, 32f, 57f },
                    new[] { 6f, 9f, 14f, 21f }, "#ff7b43"
                ),
                CreateOrLoadWeapon(
                    "cyclone", "Arc Cyclone", WeaponBehavior.Cyclone,
                    640f, new[] { 7f, 14f, 25f, 44f },
                    new[] { 6f, 9f, 14f, 23f }, "#a9fff2"
                ),
                CreateOrLoadWeapon(
                    "mines", "Echo Mines", WeaponBehavior.Mine,
                    410f, new[] { 11f, 21f, 37f, 65f },
                    new[] { 8f, 12f, 18f, 28f }, "#ff9fe5"
                ),
                CreateOrLoadWeapon(
                    "null", "Null Cannon", WeaponBehavior.Null,
                    820f, new[] { 15f, 29f, 52f, 92f },
                    new[] { 6f, 10f, 16f, 25f }, "#d1a0ff"
                )
            };
        }

        private static WeaponDefinition CreateOrLoadWeapon(
            string id,
            string displayName,
            WeaponBehavior behavior,
            float browserSpeed,
            float[] damage,
            float[] browserRadius,
            string colorHex)
        {
            string path =
                GeneratedRoot +
                "/Weapon_" +
                displayName.Replace(" ", string.Empty) +
                ".asset";

            WeaponDefinition weapon =
                AssetDatabase.LoadAssetAtPath<WeaponDefinition>(
                    path
                );

            if (!weapon)
            {
                weapon =
                    ScriptableObject.CreateInstance<WeaponDefinition>();

                AssetDatabase.CreateAsset(
                    weapon,
                    path
                );
            }

            weapon.Id = id;
            weapon.DisplayName = displayName;
            weapon.Behavior = behavior;

            // Browser gameplay authored speed/radius in canvas units.
            // The established Unity conversion is 50 reference units = 1 m.
            weapon.ProjectileSpeed = browserSpeed / 50f;
            weapon.EnergyColor = ColorFromHex(colorHex);

            weapon.UnchargedDamage = damage[0];
            weapon.Tier1Damage = damage[1];
            weapon.Tier2Damage = damage[2];
            weapon.Tier3Damage = damage[3];

            weapon.UnchargedRadius = browserRadius[0] / 50f;
            weapon.Tier1Radius = browserRadius[1] / 50f;
            weapon.Tier2Radius = browserRadius[2] / 50f;
            weapon.Tier3Radius = browserRadius[3] / 50f;

            EditorUtility.SetDirty(weapon);
            return weapon;
        }

        private static GuardianDefinition[] CreateOrLoadGuardians()
        {
            return new[]
            {
                CreateOrLoadGuardian(
                    GuardianId.Aegis,
                    "Aegis Guard",
                    8f,
                    "Defensive Guardian support."
                ),
                CreateOrLoadGuardian(
                    GuardianId.Cinder,
                    "Cinder Drive",
                    7f,
                    "Heat-driven offensive Guardian support."
                ),
                CreateOrLoadGuardian(
                    GuardianId.Mycel,
                    "Mycel Bloom",
                    10f,
                    "Regenerative and growth-oriented Guardian support."
                ),
                CreateOrLoadGuardian(
                    GuardianId.Rime,
                    "Rime Field",
                    9f,
                    "Cold-field control Guardian support."
                ),
                CreateOrLoadGuardian(
                    GuardianId.Tempest,
                    "Tempest Lift",
                    7f,
                    "Aerial and electrical Guardian support."
                ),
                CreateOrLoadGuardian(
                    GuardianId.Null,
                    "Null Overdrive",
                    12f,
                    "Spatial/gravity Guardian support."
                )
            };
        }

        private static GuardianDefinition CreateOrLoadGuardian(
            GuardianId id,
            string displayName,
            float cooldown,
            string description)
        {
            string path =
                GeneratedRoot +
                "/Guardian_" +
                id +
                ".asset";

            GuardianDefinition guardian =
                AssetDatabase.LoadAssetAtPath<GuardianDefinition>(
                    path
                );

            if (!guardian)
            {
                guardian =
                    ScriptableObject.CreateInstance<GuardianDefinition>();

                AssetDatabase.CreateAsset(
                    guardian,
                    path
                );
            }

            guardian.Id = id;
            guardian.DisplayName = displayName;
            guardian.CooldownSeconds = cooldown;
            guardian.GameplayDescription = description;

            EditorUtility.SetDirty(guardian);
            return guardian;
        }

        private static Color ColorFromHex(string value)
        {
            if (
                ColorUtility.TryParseHtmlString(
                    value,
                    out Color color
                )
            )
            {
                return color;
            }

            return Color.white;
        }

        private static Projectile2D CreateProjectilePrefab(
            Material material,
            int playerProjectileLayer)
        {
            GameObject root = new("Projectile_Greybox");
            root.layer = playerProjectileLayer;

            Rigidbody2D body =
                root.AddComponent<Rigidbody2D>();

            body.gravityScale = 0f;
            body.collisionDetectionMode =
                CollisionDetectionMode2D.Continuous;

            CircleCollider2D collider =
                root.AddComponent<CircleCollider2D>();

            collider.isTrigger = true;
            collider.radius = 0.5f;

            Projectile2D projectile =
                root.AddComponent<Projectile2D>();

            SetObjectReference(
                projectile,
                "body",
                body
            );

            SetObjectReference(
                projectile,
                "hitCollider",
                collider
            );

            GameObject visual =
                GameObject.CreatePrimitive(
                    PrimitiveType.Sphere
                );

            visual.name = "Visual";
            visual.transform.SetParent(
                root.transform,
                false
            );

            Object.DestroyImmediate(
                visual.GetComponent<Collider>()
            );

            visual.GetComponent<MeshRenderer>().
                sharedMaterial = material;

            PrefabUtility.SaveAsPrefabAsset(
                root,
                ProjectilePrefabPath
            );

            Object.DestroyImmediate(root);

            GameObject asset =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    ProjectilePrefabPath
                );

            return asset.GetComponent<Projectile2D>();
        }

        private static GameObject CreatePlayerPrefab(
            Material novaMaterial,
            Material echoMaterial,
            WeaponDefinition pulse,
            WeaponDefinition[] weapons,
            GuardianDefinition[] guardians,
            Projectile2D projectilePrefab,
            int worldLayer,
            int oneWayLayer,
            int playerLayer,
            int enemyLayer,
            int enemyProjectileLayer,
            int grapplePointLayer)
        {
            GameObject root = new("Nova_Greybox");
            root.layer = playerLayer;

            Rigidbody2D body =
                root.AddComponent<Rigidbody2D>();

            body.gravityScale = 3.57f;
            body.freezeRotation = true;
            body.interpolation =
                RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode =
                CollisionDetectionMode2D.Continuous;

            CapsuleCollider2D capsule =
                root.AddComponent<CapsuleCollider2D>();

            capsule.direction =
                CapsuleDirection2D.Vertical;

            capsule.size =
                new Vector2(0.72f, 1.75f);

            Damageable2D health =
                root.AddComponent<Damageable2D>();

            SetInt(health, "actorId", 0);
            SetEnum(
                health,
                "faction",
                (int)CombatFaction.Player
            );
            SetFloat(health, "maxHealth", 100f);
            SetObjectReference(health, "body", body);

            AddCombatState(
                root,
                health,
                0f,
                0f,
                100f
            );

            NovaMotor2D motor =
                root.AddComponent<NovaMotor2D>();

            SetInt(motor, "playerId", 0);
            SetObjectReference(motor, "body", body);
            SetObjectReference(motor, "capsule", capsule);
            SetLayerMask(
                motor,
                "worldMask",
                1 << worldLayer
            );
            SetLayerMask(
                motor,
                "oneWayMask",
                1 << oneWayLayer
            );

            NovaCombatController combat =
                root.AddComponent<NovaCombatController>();

            NovaTraversalDamage traversal =
                root.AddComponent<NovaTraversalDamage>();

            NovaPlayerGameplay gameplay =
                root.AddComponent<NovaPlayerGameplay>();

            StrikerPlayerIdentity identity =
                root.AddComponent<StrikerPlayerIdentity>();

            PlayerStyleMeter styleMeter =
                root.AddComponent<PlayerStyleMeter>();

            StrikerDownedState downed =
                root.AddComponent<StrikerDownedState>();

            StrikerReviveInteractor reviveInteractor =
                root.AddComponent<StrikerReviveInteractor>();

            StrikeSuitAbilityController suitAbilities =
                root.AddComponent<StrikeSuitAbilityController>();

            StrikerLoadoutController loadout =
                root.AddComponent<StrikerLoadoutController>();

            CosmeticLoadoutController cosmeticLoadout =
                root.AddComponent<CosmeticLoadoutController>();

            GuardianAbilityController guardianAbilities =
                root.AddComponent<GuardianAbilityController>();

            NovaInputSystemAdapter input =
                root.AddComponent<NovaInputSystemAdapter>();

            GreyboxMechanicsVisualizer visualizer =
                root.AddComponent<GreyboxMechanicsVisualizer>();

            StrikerPresentationBridge presentation =
                root.AddComponent<StrikerPresentationBridge>();

            GameObject novaVisualRoot =
                new("NovaVisualRoot");
            novaVisualRoot.transform.SetParent(
                root.transform,
                false
            );

            GameObject novaVisual =
                GameObject.CreatePrimitive(
                    PrimitiveType.Capsule
                );

            novaVisual.name = "Nova_Placeholder";
            novaVisual.transform.SetParent(
                novaVisualRoot.transform,
                false
            );
            novaVisual.transform.localScale =
                new Vector3(0.55f, 0.9f, 0.55f);

            Object.DestroyImmediate(
                novaVisual.GetComponent<Collider>()
            );

            novaVisual.GetComponent<MeshRenderer>().
                sharedMaterial = novaMaterial;

            GameObject echoVisualRoot =
                new("EchoVisualRoot");
            echoVisualRoot.transform.SetParent(
                root.transform,
                false
            );

            GameObject echoVisual =
                GameObject.CreatePrimitive(
                    PrimitiveType.Capsule
                );

            echoVisual.name = "Echo_Placeholder";
            echoVisual.transform.SetParent(
                echoVisualRoot.transform,
                false
            );
            echoVisual.transform.localScale =
                new Vector3(0.52f, 0.88f, 0.55f);

            Object.DestroyImmediate(
                echoVisual.GetComponent<Collider>()
            );

            echoVisual.GetComponent<MeshRenderer>().
                sharedMaterial = echoMaterial;

            echoVisualRoot.SetActive(false);

            GameObject muzzle =
                new("MuzzleSocket");

            muzzle.transform.SetParent(
                root.transform,
                false
            );

            muzzle.transform.localPosition =
                new Vector3(0.62f, 0.22f, 0f);

            SetInt(combat, "playerId", 0);
            SetObjectReference(
                combat,
                "motor",
                motor
            );
            SetObjectReference(
                combat,
                "suitAbilities",
                suitAbilities
            );
            SetObjectReference(
                combat,
                "muzzleSocket",
                muzzle.transform
            );
            SetObjectReference(
                combat,
                "projectilePrefab",
                projectilePrefab
            );
            SetObjectReference(
                combat,
                "equippedWeapon",
                pulse
            );
            SetLayerMask(
                combat,
                "damageableMask",
                1 << enemyLayer
            );
            SetLayerMask(
                combat,
                "projectileMask",
                1 << enemyProjectileLayer
            );
            SetLayerMask(
                combat,
                "grapplePointMask",
                1 << grapplePointLayer
            );
            SetLayerMask(
                combat,
                "grappleSurfaceMask",
                (1 << worldLayer) |
                (1 << oneWayLayer)
            );

            SetObjectReference(
                traversal,
                "motor",
                motor
            );
            SetObjectReference(
                traversal,
                "bodyCollider",
                capsule
            );
            SetLayerMask(
                traversal,
                "damageableMask",
                1 << enemyLayer
            );

            SetObjectReference(
                gameplay,
                "motor",
                motor
            );
            SetObjectReference(
                gameplay,
                "combat",
                combat
            );
            SetObjectReference(
                gameplay,
                "identity",
                identity
            );
            SetObjectReference(
                gameplay,
                "reviveInteractor",
                reviveInteractor
            );
            SetObjectReference(
                gameplay,
                "suitAbilities",
                suitAbilities
            );
            SetObjectReference(
                gameplay,
                "loadout",
                loadout
            );
            SetObjectReference(
                gameplay,
                "guardianAbilities",
                guardianAbilities
            );

            SetObjectReference(
                identity,
                "damageable",
                health
            );
            SetObjectReference(
                identity,
                "combat",
                combat
            );

            SetObjectReference(
                styleMeter,
                "identity",
                identity
            );

            SetObjectReference(
                downed,
                "damageable",
                health
            );
            SetObjectReference(
                downed,
                "motor",
                motor
            );
            SetObjectReference(
                downed,
                "combat",
                combat
            );
            SetObjectReference(
                downed,
                "body",
                body
            );

            SetObjectReference(
                reviveInteractor,
                "identity",
                identity
            );

            SetObjectReference(
                suitAbilities,
                "identity",
                identity
            );
            SetObjectReference(
                suitAbilities,
                "motor",
                motor
            );
            SetObjectReference(
                suitAbilities,
                "combat",
                combat
            );
            SetObjectReference(
                suitAbilities,
                "damageable",
                health
            );
            SetObjectReference(
                suitAbilities,
                "reviveInteractor",
                reviveInteractor
            );
            SetLayerMask(
                suitAbilities,
                "damageableMask",
                1 << enemyLayer
            );
            SetLayerMask(
                suitAbilities,
                "projectileMask",
                1 << enemyProjectileLayer
            );
            SetLayerMask(
                suitAbilities,
                "worldMask",
                (1 << worldLayer) |
                (1 << oneWayLayer)
            );

            SetObjectReference(
                loadout,
                "combat",
                combat
            );
            loadout.Configure(
                weapons,
                guardians
            );

            SetObjectReference(
                guardianAbilities,
                "identity",
                identity
            );
            SetObjectReference(
                guardianAbilities,
                "loadout",
                loadout
            );
            SetObjectReference(
                guardianAbilities,
                "damageable",
                health
            );
            SetLayerMask(
                guardianAbilities,
                "damageableMask",
                1 << enemyLayer
            );

            SetObjectReference(
                input,
                "player",
                gameplay
            );

            SetObjectReference(
                visualizer,
                "motor",
                motor
            );
            SetObjectReference(
                visualizer,
                "combat",
                combat
            );

            SetObjectReference(
                presentation,
                "motor",
                motor
            );
            SetObjectReference(
                presentation,
                "combat",
                combat
            );
            SetObjectReference(
                presentation,
                "novaVisualRoot",
                novaVisualRoot
            );
            SetObjectReference(
                presentation,
                "echoVisualRoot",
                echoVisualRoot
            );

            PrefabUtility.SaveAsPrefabAsset(
                root,
                PlayerPrefabPath
            );

            Object.DestroyImmediate(root);

            return AssetDatabase.LoadAssetAtPath<GameObject>(
                PlayerPrefabPath
            );
        }

        private static void CreatePersistentServices(
            CommerceCatalog commerceCatalog)
        {
            GameObject root =
                new("Greybox_PersistentServices");

            root.AddComponent<RuntimeScalabilityManager>();

            SaveGameService save =
                root.AddComponent<SaveGameService>();

            CampaignProgressionController progression =
                root.AddComponent<CampaignProgressionController>();

            EntitlementService entitlements =
                root.AddComponent<EntitlementService>();

            SetObjectReference(
                progression,
                "saveService",
                save
            );

            SetObjectReference(
                entitlements,
                "catalog",
                commerceCatalog
            );

            SetObjectReference(
                entitlements,
                "saveService",
                save
            );
        }

        private static GameObject CreateSessionBootstrap(
            int enemyLayer)
        {
            GameObject root =
                new("Greybox_Session");

            root.AddComponent<GreyboxSessionBootstrap>();

            StrikeTeamSession session =
                root.AddComponent<StrikeTeamSession>();

            root.AddComponent<LocalStrikeTeamInputCoordinator>();

            TeamSyncResolver syncResolver =
                root.AddComponent<TeamSyncResolver>();

            WeaponMasteryService mastery =
                root.AddComponent<WeaponMasteryService>();

            SetObjectReference(
                syncResolver,
                "session",
                session
            );

            SetObjectReference(
                mastery,
                "session",
                session
            );
            SetLayerMask(
                syncResolver,
                "damageableMask",
                1 << enemyLayer
            );

            return root;
        }

        private static void CreateCamera()
        {
            GameObject root = new("Main Camera");
            root.tag = "MainCamera";
            root.transform.position =
                new Vector3(0f, 0f, -10f);

            Camera camera =
                root.AddComponent<Camera>();

            root.AddComponent<StrikeTeamCamera2D>();

            camera.orthographic = true;
            camera.orthographicSize = 5.8f;
            camera.clearFlags =
                CameraClearFlags.SolidColor;
            camera.backgroundColor =
                new Color(0.025f, 0.035f, 0.055f);
        }

        private static void CreateLight()
        {
            GameObject root =
                new("Greybox_DirectionalLight");

            root.transform.rotation =
                Quaternion.Euler(28f, -32f, 0f);

            Light light = root.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
        }

        private static GameObject CreateWorldBox(
            string name,
            Vector3 position,
            Vector3 scale,
            Material material,
            int layer)
        {
            GameObject root =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube
                );

            root.name = name;
            root.layer = layer;
            root.transform.position = position;
            root.transform.localScale = scale;

            Object.DestroyImmediate(
                root.GetComponent<BoxCollider>()
            );

            root.AddComponent<BoxCollider2D>();

            root.GetComponent<MeshRenderer>().
                sharedMaterial = material;

            return root;
        }

        private static void CreateOneWayPlatform(
            string name,
            Vector3 position,
            Vector3 scale,
            Material material,
            int layer)
        {
            GameObject root = CreateWorldBox(
                name,
                position,
                scale,
                material,
                layer
            );

            BoxCollider2D collider =
                root.GetComponent<BoxCollider2D>();

            PlatformEffector2D effector =
                root.AddComponent<PlatformEffector2D>();

            effector.useOneWay = true;
            effector.surfaceArc = 180f;

            collider.usedByEffector = true;
        }

        private static void CreateGrapplePoint(
            string name,
            string grappleId,
            Vector3 position,
            Material material,
            int layer)
        {
            GameObject root =
                GameObject.CreatePrimitive(
                    PrimitiveType.Sphere
                );

            root.name = name;
            root.layer = layer;
            root.transform.position = position;
            root.transform.localScale =
                Vector3.one * 0.46f;

            Object.DestroyImmediate(
                root.GetComponent<Collider>()
            );

            CircleCollider2D collider =
                root.AddComponent<CircleCollider2D>();

            collider.isTrigger = true;
            collider.radius = 0.8f;

            GrapplePoint2D point =
                root.AddComponent<GrapplePoint2D>();

            SetString(
                point,
                "grappleId",
                grappleId
            );

            root.GetComponent<MeshRenderer>().
                sharedMaterial = material;
        }

        private static void CreateDamageDummy(
            string name,
            int actorId,
            Vector3 position,
            float health,
            Material material,
            int enemyLayer,
            Vector3? scaleOverride = null)
        {
            GameObject root =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube
                );

            root.name = name;
            root.layer = enemyLayer;
            root.transform.position = position;
            root.transform.localScale =
                scaleOverride ??
                new Vector3(0.82f, 1.35f, 0.82f);

            Object.DestroyImmediate(
                root.GetComponent<BoxCollider>()
            );

            root.AddComponent<BoxCollider2D>();

            Rigidbody2D body =
                root.AddComponent<Rigidbody2D>();

            body.gravityScale = 3.57f;
            body.freezeRotation = true;
            body.collisionDetectionMode =
                CollisionDetectionMode2D.Continuous;

            Damageable2D damageable =
                root.AddComponent<Damageable2D>();

            SetInt(
                damageable,
                "actorId",
                actorId
            );
            SetEnum(
                damageable,
                "faction",
                (int)CombatFaction.Enemy
            );
            SetFloat(
                damageable,
                "maxHealth",
                health
            );
            SetObjectReference(
                damageable,
                "body",
                body
            );

            AddCombatState(
                root,
                damageable,
                0f,
                0f,
                70f
            );

            root.GetComponent<MeshRenderer>().
                sharedMaterial = material;
        }

        private static void CreateSkirmisherEnemy(
            string name,
            int actorId,
            Vector3 position,
            Transform target,
            Projectile2D projectilePrefab,
            Material material,
            int enemyLayer,
            int worldLayer,
            int oneWayLayer)
        {
            GameObject root =
                GameObject.CreatePrimitive(
                    PrimitiveType.Capsule
                );

            root.name = name;
            root.layer = enemyLayer;
            root.transform.position = position;
            root.transform.localScale =
                new Vector3(0.72f, 0.92f, 0.72f);

            Object.DestroyImmediate(
                root.GetComponent<CapsuleCollider>()
            );

            CapsuleCollider2D collider =
                root.AddComponent<CapsuleCollider2D>();

            collider.direction =
                CapsuleDirection2D.Vertical;
            collider.size =
                new Vector2(0.82f, 1.45f);

            Rigidbody2D body =
                root.AddComponent<Rigidbody2D>();

            body.gravityScale = 3.57f;
            body.freezeRotation = true;
            body.interpolation =
                RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode =
                CollisionDetectionMode2D.Continuous;

            Damageable2D damageable =
                root.AddComponent<Damageable2D>();

            SetInt(
                damageable,
                "actorId",
                actorId
            );
            SetEnum(
                damageable,
                "faction",
                (int)CombatFaction.Enemy
            );
            SetFloat(
                damageable,
                "maxHealth",
                86f
            );
            SetObjectReference(
                damageable,
                "body",
                body
            );

            AddCombatState(
                root,
                damageable,
                0f,
                0f,
                70f
            );

            EnemyBrain2D brain =
                root.AddComponent<EnemyBrain2D>();

            SetInt(
                brain,
                "actorId",
                actorId
            );
            SetEnum(
                brain,
                "role",
                (int)EnemyRole.Skirmisher
            );
            SetObjectReference(
                brain,
                "body",
                body
            );
            SetObjectReference(
                brain,
                "damageable",
                damageable
            );
            SetObjectReference(
                brain,
                "target",
                target
            );
            SetLayerMask(
                brain,
                "lineOfSightMask",
                (1 << worldLayer) |
                (1 << oneWayLayer)
            );

            EnemySkirmisherModule2D skirmisher =
                root.AddComponent<EnemySkirmisherModule2D>();

            SetObjectReference(
                skirmisher,
                "projectilePrefab",
                projectilePrefab
            );

            root.GetComponent<MeshRenderer>().
                sharedMaterial = material;
        }

        private static void CreateFlankerEnemy(
            string name,
            int actorId,
            Vector3 position,
            Transform target,
            Projectile2D projectilePrefab,
            Material material,
            int enemyLayer,
            int worldLayer,
            int oneWayLayer)
        {
            GameObject root =
                GameObject.CreatePrimitive(
                    PrimitiveType.Capsule
                );

            root.name = name;
            root.layer = enemyLayer;
            root.transform.position = position;
            root.transform.localScale =
                new Vector3(0.62f, 0.82f, 0.62f);

            Object.DestroyImmediate(
                root.GetComponent<CapsuleCollider>()
            );

            CapsuleCollider2D collider =
                root.AddComponent<CapsuleCollider2D>();

            collider.direction =
                CapsuleDirection2D.Vertical;
            collider.size =
                new Vector2(0.76f, 1.30f);

            Rigidbody2D body =
                root.AddComponent<Rigidbody2D>();

            body.gravityScale = 3.57f;
            body.freezeRotation = true;
            body.interpolation =
                RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode =
                CollisionDetectionMode2D.Continuous;

            Damageable2D damageable =
                root.AddComponent<Damageable2D>();

            SetInt(damageable, "actorId", actorId);
            SetEnum(
                damageable,
                "faction",
                (int)CombatFaction.Enemy
            );
            SetFloat(damageable, "maxHealth", 72f);
            SetObjectReference(
                damageable,
                "body",
                body
            );

            AddCombatState(
                root,
                damageable,
                0f,
                0f,
                70f
            );

            EnemyBrain2D brain =
                root.AddComponent<EnemyBrain2D>();

            SetInt(brain, "actorId", actorId);
            SetEnum(
                brain,
                "role",
                (int)EnemyRole.Flanker
            );
            SetFloat(brain, "detectionRange", 12f);
            SetObjectReference(brain, "body", body);
            SetObjectReference(
                brain,
                "damageable",
                damageable
            );
            SetObjectReference(
                brain,
                "target",
                target
            );
            SetLayerMask(
                brain,
                "lineOfSightMask",
                (1 << worldLayer) |
                (1 << oneWayLayer)
            );

            EnemyFlankerModule2D flanker =
                root.AddComponent<EnemyFlankerModule2D>();

            SetObjectReference(
                flanker,
                "projectilePrefab",
                projectilePrefab
            );

            root.GetComponent<MeshRenderer>().
                sharedMaterial = material;
        }

        private static void CreateArtilleryEnemy(
            string name,
            int actorId,
            Vector3 position,
            Transform target,
            Projectile2D projectilePrefab,
            Material material,
            int enemyLayer,
            int worldLayer,
            int oneWayLayer)
        {
            GameObject root =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube
                );

            root.name = name;
            root.layer = enemyLayer;
            root.transform.position = position;
            root.transform.localScale =
                new Vector3(1.05f, 1.10f, 0.90f);

            Object.DestroyImmediate(
                root.GetComponent<BoxCollider>()
            );

            BoxCollider2D collider =
                root.AddComponent<BoxCollider2D>();

            collider.size =
                new Vector2(0.94f, 1.08f);

            Rigidbody2D body =
                root.AddComponent<Rigidbody2D>();

            body.gravityScale = 3.57f;
            body.freezeRotation = true;
            body.interpolation =
                RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode =
                CollisionDetectionMode2D.Continuous;

            Damageable2D damageable =
                root.AddComponent<Damageable2D>();

            SetInt(damageable, "actorId", actorId);
            SetEnum(
                damageable,
                "faction",
                (int)CombatFaction.Enemy
            );
            SetFloat(damageable, "maxHealth", 120f);
            SetObjectReference(
                damageable,
                "body",
                body
            );

            AddCombatState(
                root,
                damageable,
                0f,
                0f,
                70f
            );

            EnemyBrain2D brain =
                root.AddComponent<EnemyBrain2D>();

            SetInt(brain, "actorId", actorId);
            SetEnum(
                brain,
                "role",
                (int)EnemyRole.Artillery
            );
            SetFloat(brain, "detectionRange", 18f);
            SetObjectReference(brain, "body", body);
            SetObjectReference(
                brain,
                "damageable",
                damageable
            );
            SetObjectReference(
                brain,
                "target",
                target
            );
            SetLayerMask(
                brain,
                "lineOfSightMask",
                (1 << worldLayer) |
                (1 << oneWayLayer)
            );

            EnemyArtilleryModule2D artillery =
                root.AddComponent<EnemyArtilleryModule2D>();

            SetObjectReference(
                artillery,
                "projectilePrefab",
                projectilePrefab
            );

            root.GetComponent<MeshRenderer>().
                sharedMaterial = material;
        }

        private static void CreateAerialEnemy(
            string name,
            int actorId,
            Vector3 position,
            AerialMotionPattern motionPattern,
            Transform target,
            Projectile2D projectilePrefab,
            Material material,
            int enemyLayer,
            int worldLayer,
            int oneWayLayer)
        {
            GameObject root =
                GameObject.CreatePrimitive(
                    PrimitiveType.Sphere
                );

            root.name = name;
            root.layer = enemyLayer;
            root.transform.position = position;
            root.transform.localScale =
                new Vector3(0.82f, 0.72f, 0.82f);

            Object.DestroyImmediate(
                root.GetComponent<SphereCollider>()
            );

            CircleCollider2D collider =
                root.AddComponent<CircleCollider2D>();

            collider.radius = 0.52f;

            Rigidbody2D body =
                root.AddComponent<Rigidbody2D>();

            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation =
                RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode =
                CollisionDetectionMode2D.Continuous;

            Damageable2D damageable =
                root.AddComponent<Damageable2D>();

            SetInt(damageable, "actorId", actorId);
            SetEnum(
                damageable,
                "faction",
                (int)CombatFaction.Enemy
            );
            SetFloat(damageable, "maxHealth", 65f);
            SetObjectReference(
                damageable,
                "body",
                body
            );

            AddCombatState(
                root,
                damageable,
                0f,
                0f,
                70f
            );

            EnemyBrain2D brain =
                root.AddComponent<EnemyBrain2D>();

            SetInt(brain, "actorId", actorId);
            SetEnum(
                brain,
                "role",
                (int)EnemyRole.Aerial
            );
            SetFloat(brain, "detectionRange", 14f);
            SetObjectReference(brain, "body", body);
            SetObjectReference(
                brain,
                "damageable",
                damageable
            );
            SetObjectReference(
                brain,
                "target",
                target
            );
            SetLayerMask(
                brain,
                "lineOfSightMask",
                (1 << worldLayer) |
                (1 << oneWayLayer)
            );

            EnemyAerialModule2D aerial =
                root.AddComponent<EnemyAerialModule2D>();

            SetEnum(
                aerial,
                "motionPattern",
                (int)motionPattern
            );
            SetObjectReference(
                aerial,
                "projectilePrefab",
                projectilePrefab
            );

            root.GetComponent<MeshRenderer>().
                sharedMaterial = material;
        }

        private static CombatState2D AddCombatState(
            GameObject root,
            Damageable2D damageable,
            float maxShield,
            float maxArmor,
            float breakMax,
            float armorReduction = 0.24f)
        {
            CombatState2D state =
                root.AddComponent<CombatState2D>();

            SetObjectReference(
                state,
                "damageable",
                damageable
            );
            SetObjectReference(
                damageable,
                "combatState",
                state
            );

            SetFloat(state, "maxShield", maxShield);
            SetFloat(state, "maxArmor", maxArmor);
            SetFloat(state, "breakMax", breakMax);
            SetFloat(
                state,
                "armorDamageReduction",
                armorReduction
            );

            return state;
        }

        private static void CreateHostileEmitter(
            Vector3 position,
            Transform target,
            Projectile2D projectilePrefab,
            Material material,
            int enemyLayer)
        {
            GameObject root =
                GameObject.CreatePrimitive(
                    PrimitiveType.Sphere
                );

            root.name = "Parry_Test_Emitter";
            root.layer = enemyLayer;
            root.transform.position = position;
            root.transform.localScale =
                Vector3.one * 0.55f;

            Object.DestroyImmediate(
                root.GetComponent<Collider>()
            );

            root.GetComponent<MeshRenderer>().
                sharedMaterial = material;

            GreyboxHostileProjectileEmitter emitter =
                root.AddComponent<
                    GreyboxHostileProjectileEmitter
                >();

            SetObjectReference(
                emitter,
                "projectilePrefab",
                projectilePrefab
            );

            SetObjectReference(
                emitter,
                "target",
                target
            );

            SetFloat(
                emitter,
                "interval",
                1.4f
            );

            SetFloat(
                emitter,
                "projectileSpeed",
                6.6f
            );

            SetFloat(
                emitter,
                "damage",
                11f
            );

            SetInt(
                emitter,
                "perfectOpportunityEvery",
                4
            );
        }

        private static void SetObjectReference(
            Object target,
            string propertyName,
            Object value)
        {
            SerializedObject serialized =
                new(target);

            SerializedProperty property =
                serialized.FindProperty(propertyName);

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetLayerMask(
            Object target,
            string propertyName,
            int value)
        {
            SerializedObject serialized =
                new(target);

            SerializedProperty property =
                serialized.FindProperty(propertyName);

            property.intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(
            Object target,
            string propertyName,
            float value)
        {
            SerializedObject serialized =
                new(target);

            SerializedProperty property =
                serialized.FindProperty(propertyName);

            property.floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInt(
            Object target,
            string propertyName,
            int value)
        {
            SerializedObject serialized =
                new(target);

            SerializedProperty property =
                serialized.FindProperty(propertyName);

            property.intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetString(
            Object target,
            string propertyName,
            string value)
        {
            SerializedObject serialized =
                new(target);

            SerializedProperty property =
                serialized.FindProperty(propertyName);

            property.stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetEnum(
            Object target,
            string propertyName,
            int enumIndex)
        {
            SerializedObject serialized =
                new(target);

            SerializedProperty property =
                serialized.FindProperty(propertyName);

            property.enumValueIndex = enumIndex;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
