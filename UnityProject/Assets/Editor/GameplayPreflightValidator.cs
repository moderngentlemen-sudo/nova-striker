using System.Collections.Generic;
using NovaStriker.Campaign;
using NovaStriker.Combat;
using NovaStriker.Debugging;
using NovaStriker.Enemies;
using NovaStriker.Platform;
using NovaStriker.Player;
using NovaStriker.Progression;
using NovaStriker.Presentation;
using NovaStriker.Session;
using UnityEditor;
using UnityEngine;

namespace NovaStriker.EditorTools
{
    /// <summary>
    /// Fast structural validation before entering Play Mode or handing the
    /// project to production asset work. It does not replace a Unity compile,
    /// Play Mode pass, profiler capture, or platform certification run.
    /// </summary>
    public static class GameplayPreflightValidator
    {
        private static readonly string[] RequiredLayers =
        {
            "World",
            "OneWay",
            "Player",
            "Enemy",
            "PlayerProjectile",
            "EnemyProjectile",
            "GrapplePoint"
        };

        [MenuItem(
            "Nova Striker/Validation/Run Gameplay Preflight",
            priority = 20)]
        public static void Run()
        {
            int errors = 0;
            int warnings = 0;

            ValidateCampaign(ref errors, ref warnings);
            ValidateLayers(ref errors);
            ValidateOpenScene(ref errors, ref warnings);

            string summary =
                $"Nova Striker gameplay preflight: " +
                $"{errors} error(s), {warnings} warning(s).";

            if (errors > 0)
                Debug.LogError(summary);
            else if (warnings > 0)
                Debug.LogWarning(summary);
            else
                Debug.Log(summary);
        }

        private static void ValidateCampaign(
            ref int errors,
            ref int warnings)
        {
            int expected =
                CampaignCatalog.SectorCount *
                CampaignCatalog.ActsPerSector;

            if (ActGameplayCatalog.Count != expected)
            {
                Error(
                    ref errors,
                    $"Expected {expected} campaign acts, " +
                    $"found {ActGameplayCatalog.Count}."
                );
            }

            HashSet<string> keys = new();

            for (
                int sector = 0;
                sector < CampaignCatalog.SectorCount;
                sector++
            )
            {
                for (
                    int act = 0;
                    act < CampaignCatalog.ActsPerSector;
                    act++
                )
                {
                    ActGameplayReference reference =
                        ActGameplayCatalog.Get(
                            sector,
                            act
                        );

                    if (!keys.Add(reference.Key))
                    {
                        Error(
                            ref errors,
                            "Duplicate act key: " +
                            reference.Key
                        );
                    }

                    if (
                        reference.Beats == null ||
                        reference.Beats.Length < 3
                    )
                    {
                        Error(
                            ref errors,
                            reference.Key +
                            " needs at least three gameplay beats."
                        );
                    }

                    bool finalAct =
                        act ==
                        CampaignCatalog.ActsPerSector - 1;

                    if (
                        finalAct != reference.HasGuardian ||
                        finalAct == reference.HasMiniBoss
                    )
                    {
                        Error(
                            ref errors,
                            reference.Key +
                            " has an invalid boss endpoint contract."
                        );
                    }

                    if (reference.Beats == null)
                        continue;

                    for (
                        int beat = 0;
                        beat < reference.Beats.Length;
                        beat++
                    )
                    {
                        ActEnemyGroup[] groups =
                            reference.Beats[beat].Groups;

                        if (groups == null)
                            continue;

                        for (
                            int group = 0;
                            group < groups.Length;
                            group++
                        )
                        {
                            if (groups[group].BaseCount < 0)
                            {
                                Error(
                                    ref errors,
                                    reference.Key +
                                    " has a negative base enemy count."
                                );
                            }

                            if (
                                groups[group]
                                    .ExtraPerAdditionalPlayer < 0
                            )
                            {
                                Error(
                                    ref errors,
                                    reference.Key +
                                    " has negative co-op scaling."
                                );
                            }
                        }
                    }
                }
            }

            int enemyCount =
                System.Enum.GetValues(
                    typeof(EnemyArchetype)
                ).Length;

            if (enemyCount != 12)
            {
                Warning(
                    ref warnings,
                    "Expected 12 named standard enemy archetypes; " +
                    $"catalog currently exposes {enemyCount}."
                );
            }
        }

        private static void ValidateLayers(
            ref int errors)
        {
            for (int i = 0; i < RequiredLayers.Length; i++)
            {
                if (
                    LayerMask.NameToLayer(
                        RequiredLayers[i]
                    ) < 0
                )
                {
                    Error(
                        ref errors,
                        "Missing required physics layer: " +
                        RequiredLayers[i]
                    );
                }
            }
        }

        private static void ValidateOpenScene(
            ref int errors,
            ref int warnings)
        {
            StrikeTeamSession[] sessions =
                Object.FindObjectsByType<StrikeTeamSession>(
                    FindObjectsInactive.Include
                );

            if (sessions.Length > 1)
            {
                Error(
                    ref errors,
                    "Open scene contains multiple StrikeTeamSession objects."
                );
            }

            StrikerPlayerIdentity[] players =
                Object.FindObjectsByType<StrikerPlayerIdentity>(
                    FindObjectsInactive.Include
                );

            HashSet<int> actorIds = new();

            for (int i = 0; i < players.Length; i++)
            {
                StrikerPlayerIdentity player =
                    players[i];

                if (!player)
                    continue;

                if (
                    player.IsParticipating &&
                    !actorIds.Add(player.ActorId)
                )
                {
                    Error(
                        ref errors,
                        "Participating players share actor ID " +
                        player.ActorId + "."
                    );
                }

                Require<Damageable2D>(
                    player.gameObject,
                    "Damageable2D",
                    ref errors
                );
                Require<NovaMotor2D>(
                    player.gameObject,
                    "NovaMotor2D",
                    ref errors
                );
                Require<NovaCombatController>(
                    player.gameObject,
                    "NovaCombatController",
                    ref errors
                );
                Require<NovaPlayerGameplay>(
                    player.gameObject,
                    "NovaPlayerGameplay",
                    ref errors
                );
                Require<StrikeSuitAbilityController>(
                    player.gameObject,
                    "StrikeSuitAbilityController",
                    ref errors
                );
                Require<StrikerDownedState>(
                    player.gameObject,
                    "StrikerDownedState",
                    ref errors
                );
                Require<StrikerPresentationBridge>(
                    player.gameObject,
                    "StrikerPresentationBridge",
                    ref errors
                );
            }

            if (
                players.Length > 0 &&
                sessions.Length == 0
            )
            {
                Error(
                    ref errors,
                    "Players exist without a StrikeTeamSession."
                );
            }

            if (
                Object.FindAnyObjectByType<RuntimeScalabilityManager>() ==
                null
            )
            {
                Warning(
                    ref warnings,
                    "No RuntimeScalabilityManager in the open scene."
                );
            }

            if (
                Object.FindAnyObjectByType<WeaponMasteryService>() ==
                null
            )
            {
                Warning(
                    ref warnings,
                    "No WeaponMasteryService in the open scene."
                );
            }

            if (
                Object.FindAnyObjectByType<SkillPerkService>() ==
                null
            )
            {
                Warning(
                    ref warnings,
                    "No SkillPerkService in the open scene."
                );
            }

            if (
                Object.FindAnyObjectByType<CoopAssistChainService>() ==
                null
            )
            {
                Warning(
                    ref warnings,
                    "No CoopAssistChainService in the open scene."
                );
            }

            if (
                Object.FindAnyObjectByType<CampaignRetryController2D>() ==
                null
            )
            {
                Warning(
                    ref warnings,
                    "No CampaignRetryController2D in the open scene."
                );
            }

            EnemyArchetypeController2D[] namedEnemies =
                Object.FindObjectsByType<EnemyArchetypeController2D>(
                    FindObjectsInactive.Include
                );

            if (namedEnemies.Length > 0)
            {
                HashSet<EnemyArchetype> coverage = new();

                for (int i = 0; i < namedEnemies.Length; i++)
                {
                    if (namedEnemies[i])
                        coverage.Add(
                            namedEnemies[i].Archetype
                        );
                }

                if (coverage.Count < 12)
                {
                    Warning(
                        ref warnings,
                        "Named-enemy validation coverage is " +
                        $"{coverage.Count}/12 archetypes in the open scene."
                    );
                }
            }

            float fixedStep =
                1f / 60f;

            if (
                Mathf.Abs(
                    Time.fixedDeltaTime -
                    fixedStep
                ) > 0.0005f
            )
            {
                Warning(
                    ref warnings,
                    "Editor fixedDeltaTime is not currently 1/60. " +
                    "RuntimeScalabilityManager enforces 60 Hz at runtime."
                );
            }
        }

        private static void Require<T>(
            GameObject root,
            string label,
            ref int errors)
            where T : Component
        {
            if (root.GetComponent<T>())
                return;

            Error(
                ref errors,
                root.name +
                " is missing " +
                label +
                "."
            );
        }

        private static void Error(
            ref int count,
            string message)
        {
            count++;
            Debug.LogError(
                "[Gameplay Preflight] " +
                message
            );
        }

        private static void Warning(
            ref int count,
            string message)
        {
            count++;
            Debug.LogWarning(
                "[Gameplay Preflight] " +
                message
            );
        }
    }
}
