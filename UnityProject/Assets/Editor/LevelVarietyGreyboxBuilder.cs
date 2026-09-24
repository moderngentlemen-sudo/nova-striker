using System;
using System.Collections.Generic;
using NovaStriker.Campaign;
using NovaStriker.Debugging;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NovaStriker.EditorTools
{
    /// <summary>
    /// Generates representative, playable pre-Blender greybox scenes for the
    /// eight campaign topology families. The labs clone the validated Mechanics
    /// Lab runtime shell, then replace its test-room geometry with topology-
    /// specific primitives, hazards, objective triggers, and moving/reconfiguring
    /// pieces. Nothing generated here is production environment art.
    /// </summary>
    public static class LevelVarietyGreyboxBuilder
    {
        private const string BaseScenePath =
            "Assets/Greybox/Scenes/NovaMechanicsGreybox.unity";

        private const string RootPath =
            "Assets/Greybox/LevelVariety";

        private const string SceneRoot =
            RootPath + "/Scenes";

        private const string GeneratedRoot =
            "Assets/Greybox/Generated";

        private const string WorldMaterialPath =
            GeneratedRoot + "/MAT_Greybox_World.mat";

        private const string OneWayMaterialPath =
            GeneratedRoot + "/MAT_Greybox_OneWay.mat";

        private const string EnemyMaterialPath =
            GeneratedRoot + "/MAT_Greybox_Enemy.mat";

        private sealed class LabSpec
        {
            public LabSpec(
                string id,
                string displayName,
                SectorId sector,
                int actIndex,
                Vector3 startPosition)
            {
                Id = id;
                DisplayName = displayName;
                Sector = sector;
                ActIndex = actIndex;
                StartPosition = startPosition;
            }

            public string Id { get; }
            public string DisplayName { get; }
            public SectorId Sector { get; }
            public int ActIndex { get; }
            public Vector3 StartPosition { get; }

            public string ScenePath =>
                SceneRoot + "/" + Id + ".unity";
        }

        private readonly struct TopologyAnchors
        {
            public TopologyAnchors(
                Vector3 goal,
                Vector3 nodeA,
                Vector3 nodeB,
                Vector3 holdCenter)
            {
                Goal = goal;
                NodeA = nodeA;
                NodeB = nodeB;
                HoldCenter = holdCenter;
            }

            public Vector3 Goal { get; }
            public Vector3 NodeA { get; }
            public Vector3 NodeB { get; }
            public Vector3 HoldCenter { get; }
        }

        private static readonly LabSpec[] Labs =
        {
            new(
                "LV_01_LinearRun",
                "Linear Run — Frozen Array",
                SectorId.CryoRelay,
                0,
                new Vector3(-13f, -2.55f, 0f)
            ),
            new(
                "LV_02_MovingConvoy",
                "Moving Convoy — Transit Spine",
                SectorId.Skyport,
                0,
                new Vector3(-13f, -2.55f, 0f)
            ),
            new(
                "LV_03_VerticalAscent",
                "Vertical Ascent — Upper Skyline",
                SectorId.Skyport,
                2,
                new Vector3(-5.8f, -2.55f, 0f)
            ),
            new(
                "LV_04_VerticalDescent",
                "Vertical Descent — Foundry Shaft",
                SectorId.EmberWorks,
                1,
                new Vector3(-5.4f, 10.2f, 0f)
            ),
            new(
                "LV_05_SplitRoute",
                "Split Route — Maintenance Interior",
                SectorId.Skyport,
                1,
                new Vector3(-13f, -2.55f, 0f)
            ),
            new(
                "LV_06_LayeredArena",
                "Layered Arena — Canopy Engine",
                SectorId.VerdantVault,
                1,
                new Vector3(-1.4f, -2.55f, 0f)
            ),
            new(
                "LV_07_LoopingArena",
                "Looping Arena — Core Forge",
                SectorId.EmberWorks,
                2,
                new Vector3(0f, -2.55f, 0f)
            ),
            new(
                "LV_08_ReconfiguringSpace",
                "Reconfiguring Space — Memory Lattice",
                SectorId.EclipseCore,
                1,
                new Vector3(-12.5f, -2.55f, 0f)
            )
        };

        [MenuItem(
            "Nova Striker/Greybox/Level Variety/Build All Topology Labs",
            priority = 30)]
        public static void BuildAll()
        {
            BuildAllInternal(
                promptToSave: true,
                openFirstLabWhenDone: true
            );
        }

        public static void BuildAllForValidation()
        {
            BuildAllInternal(
                promptToSave: false,
                openFirstLabWhenDone: false
            );
        }

        private static void BuildAllInternal(
            bool promptToSave,
            bool openFirstLabWhenDone)
        {
            if (
                promptToSave &&
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()
            )
            {
                return;
            }

            EnsureFolders();
            EnsureMechanicsLab();

            Material world =
                AssetDatabase.LoadAssetAtPath<Material>(
                    WorldMaterialPath
                );

            Material oneWay =
                AssetDatabase.LoadAssetAtPath<Material>(
                    OneWayMaterialPath
                );

            Material accent =
                AssetDatabase.LoadAssetAtPath<Material>(
                    EnemyMaterialPath
                );

            if (!world || !oneWay || !accent)
            {
                Debug.LogError(
                    "[Level Variety Greybox] Required generated materials " +
                    "are missing. Rebuild the Mechanics Lab first."
                );
                return;
            }

            for (int i = 0; i < Labs.Length; i++)
            {
                BuildLab(
                    Labs[i],
                    i,
                    world,
                    oneWay,
                    accent
                );
            }

            ConfigureBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorSceneManager.OpenScene(
                openFirstLabWhenDone
                    ? Labs[0].ScenePath
                    : BaseScenePath,
                OpenSceneMode.Single
            );

            Debug.Log(
                "[Level Variety Greybox] Built " +
                Labs.Length +
                " playable topology labs under " +
                SceneRoot +
                ". Use Nova Striker > Greybox > Level Variety to open them."
            );
        }

        [MenuItem(
            "Nova Striker/Greybox/Level Variety/Open 01 Linear Run",
            priority = 40)]
        public static void OpenLinearRun()
        {
            OpenLab(0);
        }

        [MenuItem(
            "Nova Striker/Greybox/Level Variety/Open 02 Moving Convoy",
            priority = 41)]
        public static void OpenMovingConvoy()
        {
            OpenLab(1);
        }

        [MenuItem(
            "Nova Striker/Greybox/Level Variety/Open 03 Vertical Ascent",
            priority = 42)]
        public static void OpenVerticalAscent()
        {
            OpenLab(2);
        }

        [MenuItem(
            "Nova Striker/Greybox/Level Variety/Open 04 Vertical Descent",
            priority = 43)]
        public static void OpenVerticalDescent()
        {
            OpenLab(3);
        }

        [MenuItem(
            "Nova Striker/Greybox/Level Variety/Open 05 Split Route",
            priority = 44)]
        public static void OpenSplitRoute()
        {
            OpenLab(4);
        }

        [MenuItem(
            "Nova Striker/Greybox/Level Variety/Open 06 Layered Arena",
            priority = 45)]
        public static void OpenLayeredArena()
        {
            OpenLab(5);
        }

        [MenuItem(
            "Nova Striker/Greybox/Level Variety/Open 07 Looping Arena",
            priority = 46)]
        public static void OpenLoopingArena()
        {
            OpenLab(6);
        }

        [MenuItem(
            "Nova Striker/Greybox/Level Variety/Open 08 Reconfiguring Space",
            priority = 47)]
        public static void OpenReconfiguringSpace()
        {
            OpenLab(7);
        }

        private static void OpenLab(int index)
        {
            if (index < 0 || index >= Labs.Length)
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EnsureFolders();

            if (
                !AssetDatabase.LoadAssetAtPath<SceneAsset>(
                    Labs[index].ScenePath
                )
            )
            {
                BuildAll();
            }

            EditorSceneManager.OpenScene(
                Labs[index].ScenePath,
                OpenSceneMode.Single
            );
        }

        private static void BuildLab(
            LabSpec spec,
            int labIndex,
            Material world,
            Material oneWay,
            Material accent)
        {
            Scene scene =
                EditorSceneManager.OpenScene(
                    BaseScenePath,
                    OpenSceneMode.Single
                );

            ClearMechanicsLabContent(scene);
            PositionPlayers(spec.StartPosition);

            GameObject root =
                new("LevelVariety_" + spec.Id);

            ActLevelVarietyReference plan =
                ActLevelVarietyCatalog.Get(
                    spec.Sector,
                    spec.ActIndex
                );

            GameObject geometryRoot =
                new("Geometry");

            geometryRoot.transform.SetParent(
                root.transform,
                false
            );

            List<SectorHazard2D> hazards =
                new();

            TopologyAnchors anchors =
                BuildTopology(
                    plan.Topology,
                    geometryRoot.transform,
                    world,
                    oneWay,
                    accent,
                    hazards
                );

            PositionRepresentativeEnemies(
                anchors,
                plan
            );

            ActObjectiveController2D objective =
                root.AddComponent<ActObjectiveController2D>();

            CreateObjectiveMarkers(
                plan,
                objective,
                geometryRoot.transform,
                accent,
                anchors
            );

            ActLevelVariationController2D variation =
                root.AddComponent<ActLevelVariationController2D>();

            variation.ConfigureBindings(
                Array.Empty<LevelVariationModuleBinding>(),
                hazards.ToArray()
            );

            GreyboxLevelVarietyBootstrap bootstrap =
                root.AddComponent<GreyboxLevelVarietyBootstrap>();

            bootstrap.Configure(
                (int)spec.Sector,
                spec.ActIndex,
                objective,
                variation
            );

            GreyboxLevelVarietyNavigator navigator =
                root.AddComponent<GreyboxLevelVarietyNavigator>();

            navigator.Configure(
                labIndex,
                GetLabScenePaths()
            );

            GreyboxLevelVarietyTelemetry telemetry =
                root.AddComponent<GreyboxLevelVarietyTelemetry>();

            telemetry.Configure(
                navigator,
                objective
            );

            CreateLabel(
                spec,
                plan,
                root.transform
            );

            GameObject hudRoot =
                new("LevelVariety_HUD");

            GreyboxLevelVarietyHUD hud =
                hudRoot.AddComponent<GreyboxLevelVarietyHUD>();

            hud.Configure(
                bootstrap,
                objective,
                navigator,
                telemetry
            );

            EditorUtility.SetDirty(hud);
            EditorUtility.SetDirty(navigator);
            EditorUtility.SetDirty(telemetry);
            EditorUtility.SetDirty(objective);
            EditorUtility.SetDirty(variation);
            EditorUtility.SetDirty(bootstrap);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(
                scene,
                spec.ScenePath
            );
        }

        private static TopologyAnchors BuildTopology(
            LevelTopologyKind topology,
            Transform parent,
            Material world,
            Material oneWay,
            Material accent,
            List<SectorHazard2D> hazards)
        {
            int worldLayer =
                Mathf.Max(
                    0,
                    LayerMask.NameToLayer("World")
                );

            int oneWayLayer =
                Mathf.Max(
                    0,
                    LayerMask.NameToLayer("OneWay")
                );

            return topology switch
            {
                LevelTopologyKind.LinearRun =>
                    BuildLinearRun(
                        parent,
                        world,
                        oneWay,
                        accent,
                        worldLayer,
                        oneWayLayer,
                        hazards
                    ),

                LevelTopologyKind.MovingConvoy =>
                    BuildMovingConvoy(
                        parent,
                        world,
                        oneWay,
                        accent,
                        worldLayer,
                        oneWayLayer,
                        hazards
                    ),

                LevelTopologyKind.VerticalAscent =>
                    BuildVerticalAscent(
                        parent,
                        world,
                        oneWay,
                        accent,
                        worldLayer,
                        oneWayLayer,
                        hazards
                    ),

                LevelTopologyKind.VerticalDescent =>
                    BuildVerticalDescent(
                        parent,
                        world,
                        oneWay,
                        accent,
                        worldLayer,
                        oneWayLayer,
                        hazards
                    ),

                LevelTopologyKind.SplitRoute =>
                    BuildSplitRoute(
                        parent,
                        world,
                        oneWay,
                        accent,
                        worldLayer,
                        oneWayLayer,
                        hazards
                    ),

                LevelTopologyKind.LayeredArena =>
                    BuildLayeredArena(
                        parent,
                        world,
                        oneWay,
                        accent,
                        worldLayer,
                        oneWayLayer,
                        hazards
                    ),

                LevelTopologyKind.LoopingArena =>
                    BuildLoopingArena(
                        parent,
                        world,
                        oneWay,
                        accent,
                        worldLayer,
                        oneWayLayer,
                        hazards
                    ),

                _ =>
                    BuildReconfiguringSpace(
                        parent,
                        world,
                        oneWay,
                        accent,
                        worldLayer,
                        oneWayLayer,
                        hazards
                    )
            };
        }

        private static TopologyAnchors BuildLinearRun(
            Transform parent,
            Material world,
            Material oneWay,
            Material accent,
            int worldLayer,
            int oneWayLayer,
            List<SectorHazard2D> hazards)
        {
            CreateWorldBox(
                "Floor",
                new Vector3(0f, -4f, 0f),
                new Vector3(32f, 1f, 1f),
                world,
                worldLayer,
                parent
            );

            CreateWorldBox(
                "LeftWall",
                new Vector3(-16f, 0f, 0f),
                new Vector3(1f, 9f, 1f),
                world,
                worldLayer,
                parent
            );

            CreateWorldBox(
                "RightWall",
                new Vector3(16f, 0f, 0f),
                new Vector3(1f, 9f, 1f),
                world,
                worldLayer,
                parent
            );

            CreateOneWay(
                "FastRoute_A",
                new Vector3(-6f, -0.8f, 0f),
                new Vector3(5f, 0.35f, 1f),
                oneWay,
                oneWayLayer,
                parent
            );

            CreateOneWay(
                "FastRoute_B",
                new Vector3(1f, 1.1f, 0f),
                new Vector3(4.5f, 0.35f, 1f),
                oneWay,
                oneWayLayer,
                parent
            );

            CreateOneWay(
                "FastRoute_C",
                new Vector3(7.5f, 0.2f, 0f),
                new Vector3(4.2f, 0.35f, 1f),
                oneWay,
                oneWayLayer,
                parent
            );

            hazards.Add(
                CreateHazard(
                    "Hazard_Lane_A",
                    new Vector3(-2.5f, -3.35f, 0f),
                    new Vector3(2.5f, 0.3f, 1f),
                    HazardId.Ice,
                    accent,
                    parent
                )
            );

            hazards.Add(
                CreateHazard(
                    "Hazard_Lane_B",
                    new Vector3(5f, -3.35f, 0f),
                    new Vector3(2f, 0.3f, 1f),
                    HazardId.Ice,
                    accent,
                    parent
                )
            );

            return new TopologyAnchors(
                new Vector3(13.5f, -2.5f, 0f),
                new Vector3(-3f, -2.5f, 0f),
                new Vector3(7.5f, 0.8f, 0f),
                new Vector3(2f, -2.6f, 0f)
            );
        }

        private static TopologyAnchors BuildMovingConvoy(
            Transform parent,
            Material world,
            Material oneWay,
            Material accent,
            int worldLayer,
            int oneWayLayer,
            List<SectorHazard2D> hazards)
        {
            CreateWorldBox(
                "CatchFloor",
                new Vector3(0f, -8f, 0f),
                new Vector3(34f, 1f, 1f),
                world,
                worldLayer,
                parent
            );

            CreateWorldBox(
                "DeparturePlatform",
                new Vector3(-12.5f, -4f, 0f),
                new Vector3(7f, 1f, 1f),
                world,
                worldLayer,
                parent
            );

            CreateWorldBox(
                "ArrivalPlatform",
                new Vector3(12.5f, -4f, 0f),
                new Vector3(7f, 1f, 1f),
                world,
                worldLayer,
                parent
            );

            GameObject movingA =
                CreateMovingPlatform(
                    "Convoy_A",
                    new Vector3(-7.5f, -1.7f, 0f),
                    new Vector3(4f, 0.45f, 1f),
                    oneWay,
                    worldLayer,
                    parent,
                    new Vector2(4f, 0f),
                    2.5f,
                    0f
                );

            CreateMovingPlatform(
                "Convoy_B",
                new Vector3(-1.5f, 0.2f, 0f),
                new Vector3(4f, 0.45f, 1f),
                oneWay,
                worldLayer,
                parent,
                new Vector2(4f, 0.8f),
                2.8f,
                0.35f
            );

            CreateMovingPlatform(
                "Convoy_C",
                new Vector3(5f, -1.2f, 0f),
                new Vector3(4f, 0.45f, 1f),
                oneWay,
                worldLayer,
                parent,
                new Vector2(3.5f, 0f),
                2.3f,
                0.65f
            );

            CreateOneWay(
                "UpperServiceRoute",
                new Vector3(1.5f, 3.1f, 0f),
                new Vector3(8f, 0.35f, 1f),
                oneWay,
                oneWayLayer,
                parent
            );

            hazards.Add(
                CreateHazard(
                    "ConvoyLaser",
                    new Vector3(0f, -7.3f, 0f),
                    new Vector3(7f, 0.35f, 1f),
                    HazardId.Laser,
                    accent,
                    parent
                )
            );

            _ = movingA;

            return new TopologyAnchors(
                new Vector3(13f, -2.5f, 0f),
                new Vector3(-6f, -1f, 0f),
                new Vector3(3f, 3.6f, 0f),
                new Vector3(10.5f, -2.5f, 0f)
            );
        }

        private static TopologyAnchors BuildVerticalAscent(
            Transform parent,
            Material world,
            Material oneWay,
            Material accent,
            int worldLayer,
            int oneWayLayer,
            List<SectorHazard2D> hazards)
        {
            CreateWorldBox(
                "ShaftFloor",
                new Vector3(0f, -4f, 0f),
                new Vector3(16f, 1f, 1f),
                world,
                worldLayer,
                parent
            );

            CreateWorldBox(
                "ShaftLeft",
                new Vector3(-8f, 4.5f, 0f),
                new Vector3(1f, 19f, 1f),
                world,
                worldLayer,
                parent
            );

            CreateWorldBox(
                "ShaftRight",
                new Vector3(8f, 4.5f, 0f),
                new Vector3(1f, 19f, 1f),
                world,
                worldLayer,
                parent
            );

            Vector3[] positions =
            {
                new(-4.7f, -1.2f, 0f),
                new(-1.2f, 1.0f, 0f),
                new(3.0f, 3.0f, 0f),
                new(5.0f, 5.4f, 0f),
                new(1.5f, 7.5f, 0f),
                new(-2.5f, 9.2f, 0f),
                new(2.8f, 11.0f, 0f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                CreateOneWay(
                    "Ascent_" + i,
                    positions[i],
                    new Vector3(
                        i % 2 == 0 ? 3.2f : 2.7f,
                        0.35f,
                        1f
                    ),
                    oneWay,
                    oneWayLayer,
                    parent
                );
            }

            hazards.Add(
                CreateHazard(
                    "AscentLaser_A",
                    new Vector3(-0.5f, 2.3f, 0f),
                    new Vector3(3f, 0.25f, 1f),
                    HazardId.Laser,
                    accent,
                    parent
                )
            );

            hazards.Add(
                CreateHazard(
                    "AscentLaser_B",
                    new Vector3(1f, 8.5f, 0f),
                    new Vector3(3f, 0.25f, 1f),
                    HazardId.Laser,
                    accent,
                    parent
                )
            );

            return new TopologyAnchors(
                new Vector3(2.8f, 11.8f, 0f),
                new Vector3(-2.5f, 9.8f, 0f),
                new Vector3(4.7f, 5.9f, 0f),
                new Vector3(1.5f, 7.9f, 0f)
            );
        }

        private static TopologyAnchors BuildVerticalDescent(
            Transform parent,
            Material world,
            Material oneWay,
            Material accent,
            int worldLayer,
            int oneWayLayer,
            List<SectorHazard2D> hazards)
        {
            CreateWorldBox(
                "ShaftFloor",
                new Vector3(0f, -4f, 0f),
                new Vector3(16f, 1f, 1f),
                world,
                worldLayer,
                parent
            );

            CreateWorldBox(
                "ShaftLeft",
                new Vector3(-8f, 4f, 0f),
                new Vector3(1f, 20f, 1f),
                world,
                worldLayer,
                parent
            );

            CreateWorldBox(
                "ShaftRight",
                new Vector3(8f, 4f, 0f),
                new Vector3(1f, 20f, 1f),
                world,
                worldLayer,
                parent
            );

            Vector3[] positions =
            {
                new(-5.2f, 8.8f, 0f),
                new(-1.5f, 6.8f, 0f),
                new(3.6f, 5.0f, 0f),
                new(5.3f, 2.5f, 0f),
                new(1.0f, 0.5f, 0f),
                new(-3.5f, -1.3f, 0f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                CreateOneWay(
                    "Descent_" + i,
                    positions[i],
                    new Vector3(3.1f, 0.35f, 1f),
                    oneWay,
                    oneWayLayer,
                    parent
                );
            }

            hazards.Add(
                CreateHazard(
                    "FoundryVent_A",
                    new Vector3(3.8f, 4.0f, 0f),
                    new Vector3(2f, 0.3f, 1f),
                    HazardId.Vent,
                    accent,
                    parent
                )
            );

            hazards.Add(
                CreateHazard(
                    "FoundryVent_B",
                    new Vector3(-1.5f, -3.35f, 0f),
                    new Vector3(2.5f, 0.3f, 1f),
                    HazardId.Vent,
                    accent,
                    parent
                )
            );

            return new TopologyAnchors(
                new Vector3(5.5f, -2.5f, 0f),
                new Vector3(-1.5f, 7.3f, 0f),
                new Vector3(1f, 1f, 0f),
                new Vector3(-3.5f, -0.8f, 0f)
            );
        }

        private static TopologyAnchors BuildSplitRoute(
            Transform parent,
            Material world,
            Material oneWay,
            Material accent,
            int worldLayer,
            int oneWayLayer,
            List<SectorHazard2D> hazards)
        {
            CreateWorldBox(
                "Floor",
                new Vector3(0f, -4f, 0f),
                new Vector3(32f, 1f, 1f),
                world,
                worldLayer,
                parent
            );

            CreateWorldBox(
                "RouteDivider",
                new Vector3(0f, -0.2f, 0f),
                new Vector3(12f, 0.65f, 1f),
                world,
                worldLayer,
                parent
            );

            CreateOneWay(
                "UpperEntry",
                new Vector3(-8.5f, -0.4f, 0f),
                new Vector3(3.2f, 0.35f, 1f),
                oneWay,
                oneWayLayer,
                parent
            );

            CreateOneWay(
                "UpperMiddle",
                new Vector3(-3.5f, 2.2f, 0f),
                new Vector3(4f, 0.35f, 1f),
                oneWay,
                oneWayLayer,
                parent
            );

            CreateOneWay(
                "UpperExit",
                new Vector3(5.0f, 2.2f, 0f),
                new Vector3(4.2f, 0.35f, 1f),
                oneWay,
                oneWayLayer,
                parent
            );

            hazards.Add(
                CreateHazard(
                    "LowerLaser_A",
                    new Vector3(-4f, -3.35f, 0f),
                    new Vector3(2.3f, 0.3f, 1f),
                    HazardId.Laser,
                    accent,
                    parent
                )
            );

            hazards.Add(
                CreateHazard(
                    "LowerLaser_B",
                    new Vector3(4f, -3.35f, 0f),
                    new Vector3(2.3f, 0.3f, 1f),
                    HazardId.Laser,
                    accent,
                    parent
                )
            );

            return new TopologyAnchors(
                new Vector3(13.2f, -2.5f, 0f),
                new Vector3(-2.5f, 2.7f, 0f),
                new Vector3(3.5f, -2.5f, 0f),
                new Vector3(8.5f, -2.5f, 0f)
            );
        }

        private static TopologyAnchors BuildLayeredArena(
            Transform parent,
            Material world,
            Material oneWay,
            Material accent,
            int worldLayer,
            int oneWayLayer,
            List<SectorHazard2D> hazards)
        {
            CreateWorldBox(
                "ArenaFloor",
                new Vector3(0f, -4f, 0f),
                new Vector3(30f, 1f, 1f),
                world,
                worldLayer,
                parent
            );

            CreateOneWay(
                "Layer_Left",
                new Vector3(-7f, -0.8f, 0f),
                new Vector3(6f, 0.35f, 1f),
                oneWay,
                oneWayLayer,
                parent
            );

            CreateOneWay(
                "Layer_Center",
                new Vector3(0f, 1.6f, 0f),
                new Vector3(7f, 0.35f, 1f),
                oneWay,
                oneWayLayer,
                parent
            );

            CreateOneWay(
                "Layer_Right",
                new Vector3(7f, -0.8f, 0f),
                new Vector3(6f, 0.35f, 1f),
                oneWay,
                oneWayLayer,
                parent
            );

            CreateOneWay(
                "Layer_Top",
                new Vector3(0f, 4.4f, 0f),
                new Vector3(5f, 0.35f, 1f),
                oneWay,
                oneWayLayer,
                parent
            );

            hazards.Add(
                CreateHazard(
                    "Spore_Left",
                    new Vector3(-8f, -3.35f, 0f),
                    new Vector3(2.5f, 0.3f, 1f),
                    HazardId.Spore,
                    accent,
                    parent
                )
            );

            hazards.Add(
                CreateHazard(
                    "Spore_Right",
                    new Vector3(8f, -3.35f, 0f),
                    new Vector3(2.5f, 0.3f, 1f),
                    HazardId.Spore,
                    accent,
                    parent
                )
            );

            return new TopologyAnchors(
                new Vector3(0f, 5.1f, 0f),
                new Vector3(-7f, -0.2f, 0f),
                new Vector3(7f, -0.2f, 0f),
                new Vector3(0f, -2.5f, 0f)
            );
        }

        private static TopologyAnchors BuildLoopingArena(
            Transform parent,
            Material world,
            Material oneWay,
            Material accent,
            int worldLayer,
            int oneWayLayer,
            List<SectorHazard2D> hazards)
        {
            CreateWorldBox(
                "ArenaFloor",
                new Vector3(0f, -4f, 0f),
                new Vector3(30f, 1f, 1f),
                world,
                worldLayer,
                parent
            );

            Vector3[] ring =
            {
                new(-9f, -1.0f, 0f),
                new(-6f, 1.5f, 0f),
                new(-2.5f, 3.5f, 0f),
                new(2.5f, 3.5f, 0f),
                new(6f, 1.5f, 0f),
                new(9f, -1.0f, 0f)
            };

            for (int i = 0; i < ring.Length; i++)
            {
                CreateOneWay(
                    "Loop_" + i,
                    ring[i],
                    new Vector3(3f, 0.35f, 1f),
                    oneWay,
                    oneWayLayer,
                    parent
                );
            }

            hazards.Add(
                CreateHazard(
                    "ForgeCore",
                    new Vector3(0f, -3.35f, 0f),
                    new Vector3(4f, 0.35f, 1f),
                    HazardId.Vent,
                    accent,
                    parent
                )
            );

            CreateMovingPlatform(
                "LoopBridge",
                new Vector3(-1.5f, 0.4f, 0f),
                new Vector3(3f, 0.4f, 1f),
                oneWay,
                worldLayer,
                parent,
                new Vector2(3f, 0f),
                2.2f,
                0.25f
            );

            return new TopologyAnchors(
                new Vector3(0f, 4.2f, 0f),
                new Vector3(-8.5f, -0.3f, 0f),
                new Vector3(8.5f, -0.3f, 0f),
                new Vector3(0f, -2.5f, 0f)
            );
        }

        private static TopologyAnchors BuildReconfiguringSpace(
            Transform parent,
            Material world,
            Material oneWay,
            Material accent,
            int worldLayer,
            int oneWayLayer,
            List<SectorHazard2D> hazards)
        {
            CreateWorldBox(
                "CatchFloor",
                new Vector3(0f, -7f, 0f),
                new Vector3(32f, 1f, 1f),
                world,
                worldLayer,
                parent
            );

            CreateWorldBox(
                "StartPlatform",
                new Vector3(-12.5f, -4f, 0f),
                new Vector3(7f, 1f, 1f),
                world,
                worldLayer,
                parent
            );

            CreateWorldBox(
                "EndPlatform",
                new Vector3(12.5f, -4f, 0f),
                new Vector3(7f, 1f, 1f),
                world,
                worldLayer,
                parent
            );

            GameObject[] phaseA =
            {
                CreateOneWay(
                    "PhaseA_1",
                    new Vector3(-7f, -1.2f, 0f),
                    new Vector3(4f, 0.35f, 1f),
                    oneWay,
                    oneWayLayer,
                    parent
                ),
                CreateOneWay(
                    "PhaseA_2",
                    new Vector3(0f, 2f, 0f),
                    new Vector3(4f, 0.35f, 1f),
                    oneWay,
                    oneWayLayer,
                    parent
                ),
                CreateOneWay(
                    "PhaseA_3",
                    new Vector3(7f, -1.2f, 0f),
                    new Vector3(4f, 0.35f, 1f),
                    oneWay,
                    oneWayLayer,
                    parent
                )
            };

            GameObject[] phaseB =
            {
                CreateOneWay(
                    "PhaseB_1",
                    new Vector3(-4f, 1.3f, 0f),
                    new Vector3(4f, 0.35f, 1f),
                    oneWay,
                    oneWayLayer,
                    parent
                ),
                CreateOneWay(
                    "PhaseB_2",
                    new Vector3(4f, 1.3f, 0f),
                    new Vector3(4f, 0.35f, 1f),
                    oneWay,
                    oneWayLayer,
                    parent
                )
            };

            GreyboxPhaseToggle2D toggle =
                parent.gameObject.AddComponent<GreyboxPhaseToggle2D>();

            toggle.Configure(
                phaseA,
                phaseB,
                2.75f,
                true
            );

            EditorUtility.SetDirty(toggle);

            hazards.Add(
                CreateHazard(
                    "NullGrid_A",
                    new Vector3(-2.5f, -6.35f, 0f),
                    new Vector3(3f, 0.3f, 1f),
                    HazardId.NullGrid,
                    accent,
                    parent
                )
            );

            hazards.Add(
                CreateHazard(
                    "NullGrid_B",
                    new Vector3(3.5f, -6.35f, 0f),
                    new Vector3(3f, 0.3f, 1f),
                    HazardId.NullGrid,
                    accent,
                    parent
                )
            );

            return new TopologyAnchors(
                new Vector3(13f, -2.5f, 0f),
                new Vector3(-4f, 2f, 0f),
                new Vector3(4f, 2f, 0f),
                new Vector3(0f, -5.5f, 0f)
            );
        }

        private static void CreateObjectiveMarkers(
            ActLevelVarietyReference plan,
            ActObjectiveController2D objective,
            Transform parent,
            Material material,
            TopologyAnchors anchors)
        {
            switch (plan.Objective)
            {
                case EncounterObjectiveKind.Advance:
                case EncounterObjectiveKind.Pursuit:
                    CreateObjectiveTrigger(
                        "Goal",
                        anchors.Goal,
                        new Vector3(1.1f, 2.5f, 1f),
                        material,
                        parent,
                        objective,
                        ActObjectiveTriggerRole.Goal,
                        "goal"
                    );
                    break;

                case EncounterObjectiveKind.HoldZone:
                    CreateObjectiveTrigger(
                        "HoldZone",
                        anchors.HoldCenter,
                        new Vector3(3.5f, 2.2f, 1f),
                        material,
                        parent,
                        objective,
                        ActObjectiveTriggerRole.Zone,
                        "hold-zone"
                    );
                    break;

                case EncounterObjectiveKind.DisableNodes:
                case EncounterObjectiveKind.MultiFront:
                    CreateObjectiveTrigger(
                        "Node_A",
                        anchors.NodeA,
                        new Vector3(1.1f, 1.8f, 1f),
                        material,
                        parent,
                        objective,
                        ActObjectiveTriggerRole.Node,
                        "node-a"
                    );

                    CreateObjectiveTrigger(
                        "Node_B",
                        anchors.NodeB,
                        new Vector3(1.1f, 1.8f, 1f),
                        material,
                        parent,
                        objective,
                        ActObjectiveTriggerRole.Node,
                        "node-b"
                    );
                    break;
            }
        }

        private static GameObject CreateObjectiveTrigger(
            string name,
            Vector3 position,
            Vector3 scale,
            Material material,
            Transform parent,
            ActObjectiveController2D objective,
            ActObjectiveTriggerRole role,
            string nodeId)
        {
            GameObject root =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube
                );

            root.name = name;
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            root.transform.localScale = scale;

            UnityEngine.Object.DestroyImmediate(
                root.GetComponent<BoxCollider>()
            );

            BoxCollider2D collider =
                root.AddComponent<BoxCollider2D>();

            collider.isTrigger = true;

            root.GetComponent<MeshRenderer>().
                sharedMaterial = material;

            ActObjectiveTrigger2D trigger =
                root.AddComponent<ActObjectiveTrigger2D>();

            trigger.Configure(
                objective,
                role,
                nodeId,
                true
            );

            EditorUtility.SetDirty(trigger);
            return root;
        }

        private static SectorHazard2D CreateHazard(
            string name,
            Vector3 position,
            Vector3 scale,
            HazardId hazard,
            Material material,
            Transform parent)
        {
            GameObject root =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube
                );

            root.name = name;
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            root.transform.localScale = scale;

            UnityEngine.Object.DestroyImmediate(
                root.GetComponent<BoxCollider>()
            );

            BoxCollider2D collider =
                root.AddComponent<BoxCollider2D>();

            collider.isTrigger = true;

            root.GetComponent<MeshRenderer>().
                sharedMaterial = material;

            SectorHazard2D component =
                root.AddComponent<SectorHazard2D>();

            component.ConfigureHazard(hazard);
            EditorUtility.SetDirty(component);
            return component;
        }

        private static GameObject CreateMovingPlatform(
            string name,
            Vector3 position,
            Vector3 scale,
            Material material,
            int layer,
            Transform parent,
            Vector2 travel,
            float seconds,
            float phase)
        {
            GameObject root =
                CreateWorldBox(
                    name,
                    position,
                    scale,
                    material,
                    layer,
                    parent
                );

            Rigidbody2D body =
                root.AddComponent<Rigidbody2D>();

            body.bodyType =
                RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;

            GreyboxMovingPlatform2D motion =
                root.AddComponent<GreyboxMovingPlatform2D>();

            motion.Configure(
                travel,
                seconds,
                phase
            );

            EditorUtility.SetDirty(motion);
            return root;
        }

        private static GameObject CreateWorldBox(
            string name,
            Vector3 position,
            Vector3 scale,
            Material material,
            int layer,
            Transform parent)
        {
            GameObject root =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube
                );

            root.name = name;
            root.layer = layer;
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            root.transform.localScale = scale;

            UnityEngine.Object.DestroyImmediate(
                root.GetComponent<BoxCollider>()
            );

            root.AddComponent<BoxCollider2D>();

            root.GetComponent<MeshRenderer>().
                sharedMaterial = material;

            return root;
        }

        private static GameObject CreateOneWay(
            string name,
            Vector3 position,
            Vector3 scale,
            Material material,
            int layer,
            Transform parent)
        {
            GameObject root =
                CreateWorldBox(
                    name,
                    position,
                    scale,
                    material,
                    layer,
                    parent
                );

            BoxCollider2D collider =
                root.GetComponent<BoxCollider2D>();

            PlatformEffector2D effector =
                root.AddComponent<PlatformEffector2D>();

            effector.useOneWay = true;
            effector.surfaceArc = 180f;
            collider.usedByEffector = true;

            return root;
        }

        private static void CreateLabel(
            LabSpec spec,
            ActLevelVarietyReference plan,
            Transform parent)
        {
            GameObject label =
                new("LabLabel");

            label.transform.SetParent(
                parent,
                false
            );

            label.transform.position =
                spec.StartPosition +
                new Vector3(0f, 4.5f, 0f);

            TextMesh text =
                label.AddComponent<TextMesh>();

            text.text =
                spec.DisplayName +
                "\n" +
                plan.Topology +
                " | " +
                plan.Traversal +
                " | " +
                plan.Objective;

            text.anchor = TextAnchor.UpperLeft;
            text.alignment = TextAlignment.Left;
            text.characterSize = 0.22f;
            text.fontSize = 48;
            text.color = Color.white;
        }

        private static void PositionRepresentativeEnemies(
            TopologyAnchors anchors,
            ActLevelVarietyReference plan)
        {
            SetSceneObjectPosition(
                "Enemy_Skirmisher",
                anchors.HoldCenter +
                new Vector3(2.4f, 0.4f, 0f)
            );

            SetSceneObjectPosition(
                "Enemy_Flanker",
                anchors.NodeA +
                new Vector3(-1.4f, 0.8f, 0f)
            );

            SetSceneObjectPosition(
                "Enemy_Artillery",
                anchors.NodeB +
                new Vector3(1.8f, 0.8f, 0f)
            );

            SetSceneObjectPosition(
                "Enemy_Aerial_HoverTest",
                anchors.Goal +
                new Vector3(-2.0f, 2.4f, 0f)
            );

            SetSceneObjectPosition(
                "Enemy_Aerial_OrbitTest",
                plan.Topology == LevelTopologyKind.VerticalAscent ||
                plan.Topology == LevelTopologyKind.VerticalDescent
                    ? anchors.HoldCenter +
                      new Vector3(2.2f, 3.0f, 0f)
                    : anchors.Goal +
                      new Vector3(-4.0f, 3.2f, 0f)
            );
        }

        private static void SetSceneObjectPosition(
            string objectName,
            Vector3 position)
        {
            GameObject target =
                GameObject.Find(objectName);

            if (!target)
                return;

            target.transform.position = position;
            target.SetActive(true);
        }

        private static void PositionPlayers(
            Vector3 start)
        {
            for (int i = 0; i < 4; i++)
            {
                GameObject player =
                    GameObject.Find(
                        "Striker_Player" +
                        (i + 1)
                    );

                if (!player)
                    continue;

                player.transform.position =
                    start +
                    Vector3.right * i * 0.85f;
            }
        }

        private static void ClearMechanicsLabContent(
            Scene scene)
        {
            HashSet<string> keep =
                new(StringComparer.Ordinal)
                {
                    "Greybox_PersistentServices",
                    "Greybox_Session",
                    "Main Camera",
                    "Greybox_DirectionalLight",
                    "Striker_Player1",
                    "Striker_Player2",
                    "Striker_Player3",
                    "Striker_Player4",
                    "Greybox_HUD",
                    "Enemy_Skirmisher",
                    "Enemy_Flanker",
                    "Enemy_Artillery",
                    "Enemy_Aerial_HoverTest",
                    "Enemy_Aerial_OrbitTest"
                };

            GameObject[] roots =
                scene.GetRootGameObjects();

            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root =
                    roots[i];

                if (
                    root &&
                    !keep.Contains(root.name)
                )
                {
                    UnityEngine.Object.DestroyImmediate(
                        root
                    );
                }
            }
        }

        private static void EnsureMechanicsLab()
        {
            if (
                AssetDatabase.LoadAssetAtPath<SceneAsset>(
                    BaseScenePath
                )
            )
            {
                return;
            }

            NovaGreyboxBuilder.BuildGreybox();
        }

        private static string[] GetLabScenePaths()
        {
            string[] paths =
                new string[Labs.Length];

            for (int i = 0; i < Labs.Length; i++)
                paths[i] = Labs[i].ScenePath;

            return paths;
        }

        private static void ConfigureBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes =
                new(EditorBuildSettings.scenes);

            AddBuildSceneIfMissing(
                scenes,
                BaseScenePath
            );

            for (int i = 0; i < Labs.Length; i++)
            {
                AddBuildSceneIfMissing(
                    scenes,
                    Labs[i].ScenePath
                );
            }

            EditorBuildSettings.scenes =
                scenes.ToArray();
        }

        private static void AddBuildSceneIfMissing(
            List<EditorBuildSettingsScene> scenes,
            string path)
        {
            for (int i = 0; i < scenes.Count; i++)
            {
                if (
                    string.Equals(
                        scenes[i].path,
                        path,
                        StringComparison.Ordinal
                    )
                )
                {
                    if (!scenes[i].enabled)
                    {
                        scenes[i] =
                            new EditorBuildSettingsScene(
                                path,
                                true
                            );
                    }

                    return;
                }
            }

            scenes.Add(
                new EditorBuildSettingsScene(
                    path,
                    true
                )
            );
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Greybox"))
            {
                AssetDatabase.CreateFolder(
                    "Assets",
                    "Greybox"
                );
            }

            if (!AssetDatabase.IsValidFolder(RootPath))
            {
                AssetDatabase.CreateFolder(
                    "Assets/Greybox",
                    "LevelVariety"
                );
            }

            if (!AssetDatabase.IsValidFolder(SceneRoot))
            {
                AssetDatabase.CreateFolder(
                    RootPath,
                    "Scenes"
                );
            }
        }
    }
}
