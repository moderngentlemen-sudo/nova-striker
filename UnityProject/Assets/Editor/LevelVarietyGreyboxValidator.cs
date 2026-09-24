using System;
using NovaStriker.Campaign;
using NovaStriker.Combat;
using NovaStriker.Debugging;
using NovaStriker.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NovaStriker.EditorTools
{
    /// <summary>
    /// Interactive structural validation for the eight generated level-variety
    /// labs. This verifies generated scene composition; it does not replace
    /// Play Mode traversal, camera, combat, or performance validation.
    /// </summary>
    public static class LevelVarietyGreyboxValidator
    {
        private static readonly string[] ScenePaths =
        {
            "Assets/Greybox/LevelVariety/Scenes/LV_01_LinearRun.unity",
            "Assets/Greybox/LevelVariety/Scenes/LV_02_MovingConvoy.unity",
            "Assets/Greybox/LevelVariety/Scenes/LV_03_VerticalAscent.unity",
            "Assets/Greybox/LevelVariety/Scenes/LV_04_VerticalDescent.unity",
            "Assets/Greybox/LevelVariety/Scenes/LV_05_SplitRoute.unity",
            "Assets/Greybox/LevelVariety/Scenes/LV_06_LayeredArena.unity",
            "Assets/Greybox/LevelVariety/Scenes/LV_07_LoopingArena.unity",
            "Assets/Greybox/LevelVariety/Scenes/LV_08_ReconfiguringSpace.unity"
        };

        [MenuItem(
            "Nova Striker/Validation/Validate Generated Level Variety Labs",
            priority = 24)]
        public static void ValidateInteractive()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Scene original =
                SceneManager.GetActiveScene();

            string originalPath =
                original.IsValid()
                    ? original.path
                    : string.Empty;

            int errors = 0;
            int warnings = 0;

            try
            {
                for (int i = 0; i < ScenePaths.Length; i++)
                {
                    ValidateScene(
                        ScenePaths[i],
                        ref errors,
                        ref warnings
                    );
                }
            }
            finally
            {
                if (
                    !string.IsNullOrWhiteSpace(originalPath) &&
                    AssetDatabase.LoadAssetAtPath<SceneAsset>(
                        originalPath
                    )
                )
                {
                    EditorSceneManager.OpenScene(
                        originalPath,
                        OpenSceneMode.Single
                    );
                }
            }

            string summary =
                "Nova Striker level-variety lab validation: " +
                errors + " error(s), " +
                warnings + " warning(s).";

            if (errors > 0)
                Debug.LogError(summary);
            else if (warnings > 0)
                Debug.LogWarning(summary);
            else
                Debug.Log(summary);
        }

        private static void ValidateScene(
            string path,
            ref int errors,
            ref int warnings)
        {
            SceneAsset asset =
                AssetDatabase.LoadAssetAtPath<SceneAsset>(
                    path
                );

            if (!asset)
            {
                Error(
                    ref errors,
                    "Missing generated topology lab: " +
                    path
                );
                return;
            }

            EditorSceneManager.OpenScene(
                path,
                OpenSceneMode.Single
            );

            GreyboxLevelVarietyBootstrap[] bootstraps =
                UnityEngine.Object.FindObjectsByType<
                    GreyboxLevelVarietyBootstrap
                >(FindObjectsInactive.Include);

            ActObjectiveController2D[] objectives =
                UnityEngine.Object.FindObjectsByType<
                    ActObjectiveController2D
                >(FindObjectsInactive.Include);

            GreyboxLevelVarietyHUD[] huds =
                UnityEngine.Object.FindObjectsByType<
                    GreyboxLevelVarietyHUD
                >(FindObjectsInactive.Include);

            StrikerPlayerIdentity[] players =
                UnityEngine.Object.FindObjectsByType<
                    StrikerPlayerIdentity
                >(FindObjectsInactive.Include);

            SectorHazard2D[] hazards =
                UnityEngine.Object.FindObjectsByType<
                    SectorHazard2D
                >(FindObjectsInactive.Include);

            if (bootstraps.Length != 1)
            {
                Error(
                    ref errors,
                    path +
                    " must contain exactly one level-variety bootstrap."
                );
                return;
            }

            if (objectives.Length != 1)
            {
                Error(
                    ref errors,
                    path +
                    " must contain exactly one act-objective controller."
                );
            }

            if (huds.Length != 1)
            {
                Error(
                    ref errors,
                    path +
                    " must contain exactly one level-variety HUD."
                );
            }

            if (players.Length != 4)
            {
                Error(
                    ref errors,
                    path +
                    " expected four Strike Team player objects; found " +
                    players.Length + "."
                );
            }

            if (
                Camera.main == null ||
                Camera.main.GetComponent<
                    NovaStriker.CameraSystem.StrikeTeamCamera2D
                >() == null
            )
            {
                Error(
                    ref errors,
                    path +
                    " is missing the shared Strike Team camera."
                );
            }

            if (hazards.Length == 0)
            {
                Warning(
                    ref warnings,
                    path +
                    " contains no sector-hazard volume."
                );
            }

            GreyboxLevelVarietyBootstrap bootstrap =
                bootstraps[0];

            ActLevelVarietyReference plan =
                bootstrap.CurrentPlan;

            ValidateObjectiveMarkers(
                path,
                plan,
                ref errors
            );

            ValidateTopologySpecificRuntime(
                path,
                plan.Topology,
                ref errors
            );

            Damageable2D[] damageables =
                UnityEngine.Object.FindObjectsByType<
                    Damageable2D
                >(FindObjectsInactive.Include);

            int enemyCount = 0;

            for (int i = 0; i < damageables.Length; i++)
            {
                if (
                    damageables[i] &&
                    damageables[i].Faction ==
                    CombatFaction.Enemy
                )
                {
                    enemyCount++;
                }
            }

            if (enemyCount < 3)
            {
                Warning(
                    ref warnings,
                    path +
                    " has fewer than three representative combat targets."
                );
            }
        }

        private static void ValidateObjectiveMarkers(
            string path,
            ActLevelVarietyReference plan,
            ref int errors)
        {
            ActObjectiveTrigger2D[] triggers =
                UnityEngine.Object.FindObjectsByType<
                    ActObjectiveTrigger2D
                >(FindObjectsInactive.Include);

            int required =
                plan.Objective switch
                {
                    EncounterObjectiveKind.DisableNodes => 2,
                    EncounterObjectiveKind.MultiFront => 2,
                    EncounterObjectiveKind.Advance => 1,
                    EncounterObjectiveKind.Pursuit => 1,
                    EncounterObjectiveKind.HoldZone => 1,
                    _ => 0
                };

            if (triggers.Length < required)
            {
                Error(
                    ref errors,
                    path +
                    " objective " +
                    plan.Objective +
                    " requires at least " +
                    required +
                    " objective trigger(s); found " +
                    triggers.Length + "."
                );
            }
        }

        private static void ValidateTopologySpecificRuntime(
            string path,
            LevelTopologyKind topology,
            ref int errors)
        {
            if (
                topology == LevelTopologyKind.MovingConvoy ||
                topology == LevelTopologyKind.LoopingArena
            )
            {
                GreyboxMovingPlatform2D[] movers =
                    UnityEngine.Object.FindObjectsByType<
                        GreyboxMovingPlatform2D
                    >(FindObjectsInactive.Include);

                if (movers.Length == 0)
                {
                    Error(
                        ref errors,
                        path +
                        " requires moving-platform runtime for " +
                        topology + "."
                    );
                }
            }

            if (topology == LevelTopologyKind.ReconfiguringSpace)
            {
                GreyboxPhaseToggle2D[] toggles =
                    UnityEngine.Object.FindObjectsByType<
                        GreyboxPhaseToggle2D
                    >(FindObjectsInactive.Include);

                if (toggles.Length == 0)
                {
                    Error(
                        ref errors,
                        path +
                        " requires phase-toggle runtime for reconfiguring space."
                    );
                }
            }
        }

        private static void Error(
            ref int count,
            string message)
        {
            count++;
            Debug.LogError(
                "[Level Variety Lab Validation] " +
                message
            );
        }

        private static void Warning(
            ref int count,
            string message)
        {
            count++;
            Debug.LogWarning(
                "[Level Variety Lab Validation] " +
                message
            );
        }
    }
}
