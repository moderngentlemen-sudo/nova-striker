using System.IO;
using NovaStriker.Combat;
using NovaStriker.Data;
using NovaStriker.Debugging;
using NovaStriker.Enemies;
using NovaStriker.InputSystemIntegration;
using NovaStriker.Player;
using NovaStriker.Presentation;
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

            WeaponDefinition pulse = CreateOrLoadPulseWeapon();

            Projectile2D projectilePrefab = CreateProjectilePrefab(
                projectileMaterial,
                playerProjectileLayer
            );

            GameObject playerPrefab = CreatePlayerPrefab(
                novaMaterial,
                echoMaterial,
                pulse,
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

            CreateSessionBootstrap();
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

            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(
                playerPrefab
            );

            player.name = "Nova_Player1";
            player.transform.position = new Vector3(-7.2f, -2.55f, 0f);

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

            CreateHostileEmitter(
                new Vector3(8.2f, 1.0f, 0f),
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

        private static WeaponDefinition CreateOrLoadPulseWeapon()
        {
            WeaponDefinition pulse =
                AssetDatabase.LoadAssetAtPath<WeaponDefinition>(
                    PulseWeaponPath
                );

            if (!pulse)
            {
                pulse =
                    ScriptableObject.CreateInstance<WeaponDefinition>();

                AssetDatabase.CreateAsset(
                    pulse,
                    PulseWeaponPath
                );
            }

            pulse.Id = "pulse";
            pulse.DisplayName = "Pulse";
            pulse.Behavior = WeaponBehavior.Standard;
            pulse.ProjectileSpeed = 15.6f;
            pulse.EnergyColor = new Color(0.38f, 0.86f, 1f);

            pulse.UnchargedDamage = 8f;
            pulse.Tier1Damage = 16f;
            pulse.Tier2Damage = 30f;
            pulse.Tier3Damage = 52f;

            pulse.UnchargedRadius = 0.08f;
            pulse.Tier1Radius = 0.14f;
            pulse.Tier2Radius = 0.22f;
            pulse.Tier3Radius = 0.36f;

            EditorUtility.SetDirty(pulse);
            return pulse;
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

        private static void CreateSessionBootstrap()
        {
            GameObject root =
                new("Greybox_Session");

            root.AddComponent<GreyboxSessionBootstrap>();
        }

        private static void CreateCamera()
        {
            GameObject root = new("Main Camera");
            root.tag = "MainCamera";
            root.transform.position =
                new Vector3(0f, 0f, -10f);

            Camera camera =
                root.AddComponent<Camera>();

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
