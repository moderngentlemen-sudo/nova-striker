using System;
using System.Collections.Generic;
using NovaStriker.Campaign;
using NovaStriker.Data;
using NovaStriker.Enemies;
using NovaStriker.Progression;
using UnityEditor;
using UnityEngine;

namespace NovaStriker.EditorTools
{
    /// <summary>
    /// Scene-independent structural validation that can run from the Unity
    /// editor or from a headless command-line invocation. This is deliberately
    /// narrower than Play Mode QA: it verifies the authored gameplay catalogs
    /// and four-player architecture contracts without requiring production art.
    ///
    /// Command-line example:
    /// Unity -batchmode -quit -projectPath UnityProject \
    ///   -executeMethod NovaStriker.EditorTools.GameplayBatchValidator.RunForCommandLine
    /// </summary>
    public static class GameplayBatchValidator
    {
        [MenuItem(
            "Nova Striker/Validation/Run Structural Batch Validation",
            priority = 21)]
        public static void RunInteractive()
        {
            Validate();
        }

        public static void RunForCommandLine()
        {
            bool passed = Validate();

            if (Application.isBatchMode)
                EditorApplication.Exit(passed ? 0 : 1);
        }

        private static bool Validate()
        {
            int errors = 0;
            int warnings = 0;

            ValidateCampaign(
                ref errors,
                ref warnings
            );

            ValidateLevelVariety(
                ref errors,
                ref warnings
            );

            ValidateStrikeTeamContracts(
                ref errors
            );

            ValidateSkillPerks(
                ref errors,
                ref warnings
            );

            string summary =
                "Nova Striker structural gameplay validation: " +
                errors + " error(s), " +
                warnings + " warning(s).";

            if (errors > 0)
                Debug.LogError(summary);
            else if (warnings > 0)
                Debug.LogWarning(summary);
            else
                Debug.Log(summary);

            return errors == 0;
        }

        private static void ValidateCampaign(
            ref int errors,
            ref int warnings)
        {
            int expectedActs =
                CampaignCatalog.SectorCount *
                CampaignCatalog.ActsPerSector;

            if (ActGameplayCatalog.Count != expectedActs)
            {
                Error(
                    ref errors,
                    "Expected " + expectedActs +
                    " campaign acts, found " +
                    ActGameplayCatalog.Count + "."
                );
            }

            HashSet<string> actKeys = new();
            HashSet<EnemyArchetype> enemyCoverage = new();

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

                    if (!actKeys.Add(reference.Key))
                    {
                        Error(
                            ref errors,
                            "Duplicate act key: " +
                            reference.Key
                        );
                    }

                    bool finalAct =
                        act ==
                        CampaignCatalog.ActsPerSector - 1;

                    if (finalAct)
                    {
                        if (!reference.HasGuardian)
                        {
                            Error(
                                ref errors,
                                reference.Key +
                                " must end in a Guardian encounter."
                            );
                        }

                        if (reference.HasMiniBoss)
                        {
                            Error(
                                ref errors,
                                reference.Key +
                                " cannot declare both Guardian and mini-boss endpoints."
                            );
                        }
                    }
                    else if (!reference.HasMiniBoss)
                    {
                        Warning(
                            ref warnings,
                            reference.Key +
                            " has no mini-boss endpoint."
                        );
                    }

                    ActEncounterBeat[] beats = reference.Beats;

                    if (beats == null || beats.Length < 3)
                    {
                        Error(
                            ref errors,
                            reference.Key +
                            " requires at least three gameplay beats."
                        );
                        continue;
                    }

                    HashSet<string> beatIds = new();

                    for (int beatIndex = 0; beatIndex < beats.Length; beatIndex++)
                    {
                        ActEncounterBeat beat = beats[beatIndex];

                        if (string.IsNullOrWhiteSpace(beat.Id))
                        {
                            Error(
                                ref errors,
                                reference.Key +
                                " contains an unnamed gameplay beat."
                            );
                        }
                        else if (!beatIds.Add(beat.Id))
                        {
                            Error(
                                ref errors,
                                reference.Key +
                                " contains duplicate beat id " +
                                beat.Id + "."
                            );
                        }

                        if (beat.StartDelay < 0f)
                        {
                            Error(
                                ref errors,
                                reference.Key +
                                "/" + beat.Id +
                                " has a negative start delay."
                            );
                        }

                        ActEnemyGroup[] groups = beat.Groups;

                        if (groups == null)
                            continue;

                        for (int groupIndex = 0; groupIndex < groups.Length; groupIndex++)
                        {
                            ActEnemyGroup group = groups[groupIndex];
                            enemyCoverage.Add(group.Archetype);

                            if (group.BaseCount < 0)
                            {
                                Error(
                                    ref errors,
                                    reference.Key +
                                    "/" + beat.Id +
                                    " has a negative base enemy count."
                                );
                            }

                            if (group.ExtraPerAdditionalPlayer < 0)
                            {
                                Error(
                                    ref errors,
                                    reference.Key +
                                    "/" + beat.Id +
                                    " has negative four-player scaling."
                                );
                            }
                        }
                    }
                }
            }

            int archetypeCount =
                Enum.GetValues(
                    typeof(EnemyArchetype)
                ).Length;

            if (archetypeCount != 12)
            {
                Error(
                    ref errors,
                    "Expected exactly 12 standard named enemy archetypes; " +
                    "found " + archetypeCount + "."
                );
            }

            if (enemyCoverage.Count != archetypeCount)
            {
                Warning(
                    ref warnings,
                    "Campaign composition currently exercises " +
                    enemyCoverage.Count + "/" +
                    archetypeCount +
                    " named enemy archetypes."
                );
            }
        }

        private static void ValidateLevelVariety(
            ref int errors,
            ref int warnings)
        {
            int expectedActs =
                CampaignCatalog.SectorCount *
                CampaignCatalog.ActsPerSector;

            if (ActLevelVarietyCatalog.Count != expectedActs)
            {
                Error(
                    ref errors,
                    "Expected " + expectedActs +
                    " level-variety plans, found " +
                    ActLevelVarietyCatalog.Count + "."
                );
            }

            HashSet<LevelTopologyKind> topologyCoverage = new();
            HashSet<TraversalEmphasisKind> traversalCoverage = new();
            HashSet<EncounterObjectiveKind> objectiveCoverage = new();
            HashSet<RouteChoiceKind> routeCoverage = new();
            HashSet<HazardPatternKind> hazardCoverage = new();

            for (
                int sector = 0;
                sector < CampaignCatalog.SectorCount;
                sector++
            )
            {
                HashSet<LevelTopologyKind> sectorTopologies = new();
                HashSet<EncounterObjectiveKind> sectorObjectives = new();

                for (
                    int act = 0;
                    act < CampaignCatalog.ActsPerSector;
                    act++
                )
                {
                    ActGameplayReference gameplay =
                        ActGameplayCatalog.Get(
                            sector,
                            act
                        );

                    ActLevelVarietyReference variety =
                        ActLevelVarietyCatalog.Get(
                            sector,
                            act
                        );

                    if (variety.Key != gameplay.Key)
                    {
                        Error(
                            ref errors,
                            "Level-variety plan " +
                            variety.Key +
                            " does not align with gameplay act " +
                            gameplay.Key + "."
                        );
                    }

                    if (string.IsNullOrWhiteSpace(variety.ClimaxId))
                    {
                        Error(
                            ref errors,
                            variety.Key +
                            " has no level-variety climax id."
                        );
                    }

                    if (
                        variety.ModuleTags == null ||
                        variety.ModuleTags.Length < 2
                    )
                    {
                        Error(
                            ref errors,
                            variety.Key +
                            " requires at least two modular level tags."
                        );
                    }
                    else
                    {
                        HashSet<string> tags =
                            new(StringComparer.OrdinalIgnoreCase);

                        for (int i = 0; i < variety.ModuleTags.Length; i++)
                        {
                            string tag =
                                variety.ModuleTags[i];

                            if (string.IsNullOrWhiteSpace(tag))
                            {
                                Error(
                                    ref errors,
                                    variety.Key +
                                    " contains an empty level module tag."
                                );
                            }
                            else if (!tags.Add(tag))
                            {
                                Error(
                                    ref errors,
                                    variety.Key +
                                    " contains duplicate module tag " +
                                    tag + "."
                                );
                            }
                        }
                    }

                    if (variety.HazardPhaseStride < 0f)
                    {
                        Error(
                            ref errors,
                            variety.Key +
                            " has a negative hazard phase stride."
                        );
                    }

                    if (
                        variety.SupportsPairSplit &&
                        variety.RouteChoice == RouteChoiceKind.None
                    )
                    {
                        Warning(
                            ref warnings,
                            variety.Key +
                            " supports pair splitting without a route-choice contract."
                        );
                    }

                    topologyCoverage.Add(variety.Topology);
                    traversalCoverage.Add(variety.Traversal);
                    objectiveCoverage.Add(variety.Objective);
                    routeCoverage.Add(variety.RouteChoice);
                    hazardCoverage.Add(variety.HazardPattern);

                    sectorTopologies.Add(variety.Topology);
                    sectorObjectives.Add(variety.Objective);
                }

                if (sectorTopologies.Count < 2)
                {
                    Warning(
                        ref warnings,
                        CampaignCatalog.Get(sector).DisplayName +
                        " exposes fewer than two distinct act topologies."
                    );
                }

                if (sectorObjectives.Count < 2)
                {
                    Warning(
                        ref warnings,
                        CampaignCatalog.Get(sector).DisplayName +
                        " exposes fewer than two distinct objective styles."
                    );
                }
            }

            if (topologyCoverage.Count < 7)
            {
                Warning(
                    ref warnings,
                    "Campaign level-variety topology coverage is only " +
                    topologyCoverage.Count + "/8."
                );
            }

            if (traversalCoverage.Count < 7)
            {
                Warning(
                    ref warnings,
                    "Campaign traversal-emphasis coverage is only " +
                    traversalCoverage.Count + "/8."
                );
            }

            if (objectiveCoverage.Count < 6)
            {
                Warning(
                    ref warnings,
                    "Campaign objective-style coverage is only " +
                    objectiveCoverage.Count + "/8."
                );
            }

            if (routeCoverage.Count < 5)
            {
                Warning(
                    ref warnings,
                    "Campaign route-choice coverage is only " +
                    routeCoverage.Count + "/6."
                );
            }

            if (hazardCoverage.Count < 5)
            {
                Warning(
                    ref warnings,
                    "Campaign hazard-pattern coverage is only " +
                    hazardCoverage.Count + "/6."
                );
            }
        }

        private static void ValidateStrikeTeamContracts(
            ref int errors)
        {
            Array roles =
                Enum.GetValues(
                    typeof(StrikeTeamRole)
                );

            if (roles.Length != 4)
            {
                Error(
                    ref errors,
                    "Strike Team architecture must expose exactly four canonical roles."
                );
            }

            if (
                StrikeTeamRoleRules.CommandRank(
                    StrikeTeamRole.Striker1
                ) != StrikeTeamCommandRank.Lead
            )
            {
                Error(
                    ref errors,
                    "Striker 1 must remain the canonical team lead."
                );
            }

            if (
                StrikeTeamRoleRules.CommandRank(
                    StrikeTeamRole.Striker0
                ) != StrikeTeamCommandRank.SecondInCommand
            )
            {
                Error(
                    ref errors,
                    "Striker 0 must remain second-in-command."
                );
            }

            for (int i = 0; i < roles.Length; i++)
            {
                StrikeTeamRole role =
                    (StrikeTeamRole)roles.GetValue(i);

                StrikeTeamRoleGameplayContract contract =
                    StrikeTeamRoleRules.GameplayContract(role);

                if (contract.Role != role)
                {
                    Error(
                        ref errors,
                        role +
                        " returns the wrong gameplay contract role."
                    );
                }

                if (
                    string.IsNullOrWhiteSpace(
                        contract.GameplayFocus
                    ) ||
                    contract.ThreatWeight <= 0f ||
                    contract.ReviveContribution <= 0f ||
                    contract.AssistContribution <= 0f
                )
                {
                    Error(
                        ref errors,
                        role +
                        " has an invalid art-independent gameplay contract."
                    );
                }
            }
        }

        private static void ValidateSkillPerks(
            ref int errors,
            ref int warnings)
        {
            ReadOnlySpan<SkillPerkDefinition> definitions =
                SkillPerkCatalog.All;

            HashSet<string> ids = new();

            if (definitions.Length < 6)
            {
                Warning(
                    ref warnings,
                    "Skill/perk catalog contains fewer than six baseline perks."
                );
            }

            for (int i = 0; i < definitions.Length; i++)
            {
                SkillPerkDefinition definition =
                    definitions[i];

                if (string.IsNullOrWhiteSpace(definition.Id))
                {
                    Error(
                        ref errors,
                        "Skill/perk catalog contains an empty id."
                    );
                    continue;
                }

                if (!ids.Add(definition.Id))
                {
                    Error(
                        ref errors,
                        "Duplicate skill/perk id: " +
                        definition.Id
                    );
                }

                if (
                    definition.Cost <= 0 ||
                    string.IsNullOrWhiteSpace(
                        definition.DisplayName
                    ) ||
                    string.IsNullOrWhiteSpace(
                        definition.Description
                    )
                )
                {
                    Error(
                        ref errors,
                        definition.Id +
                        " has an invalid progression definition."
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
                "[Structural Gameplay Validation] " +
                message
            );
        }

        private static void Warning(
            ref int count,
            string message)
        {
            count++;
            Debug.LogWarning(
                "[Structural Gameplay Validation] " +
                message
            );
        }
    }
}
