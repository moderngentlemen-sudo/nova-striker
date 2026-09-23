using System;

namespace NovaStriker.Campaign
{
    public enum LevelTopologyKind
    {
        LinearRun = 0,
        VerticalAscent = 1,
        VerticalDescent = 2,
        LayeredArena = 3,
        SplitRoute = 4,
        LoopingArena = 5,
        MovingConvoy = 6,
        ReconfiguringSpace = 7
    }

    public enum TraversalEmphasisKind
    {
        Balanced = 0,
        Momentum = 1,
        Precision = 2,
        Vertical = 3,
        Grapple = 4,
        MovingPlatforms = 5,
        HazardTiming = 6,
        TeamSplit = 7
    }

    public enum EncounterObjectiveKind
    {
        Eliminate = 0,
        Advance = 1,
        HoldZone = 2,
        DisableNodes = 3,
        Pursuit = 4,
        Survival = 5,
        ProtectAsset = 6,
        MultiFront = 7
    }

    public enum RouteChoiceKind
    {
        None = 0,
        SafeVsFast = 1,
        CombatVsTraversal = 2,
        UpperLower = 3,
        RiskReward = 4,
        Dynamic = 5
    }

    public enum HazardPatternKind
    {
        Standard = 0,
        AlternatingLanes = 1,
        Sweeping = 2,
        BurstWindows = 3,
        PursuitPressure = 4,
        Reconfiguring = 5
    }

    /// <summary>
    /// Art-independent level-identity contract for one campaign act. Geometry,
    /// presentation, and authored collision remain scene/production concerns;
    /// this contract defines the gameplay grammar those scenes should realize.
    /// </summary>
    public readonly struct ActLevelVarietyReference
    {
        public ActLevelVarietyReference(
            SectorId sector,
            int actIndex,
            LevelTopologyKind topology,
            TraversalEmphasisKind traversal,
            EncounterObjectiveKind objective,
            RouteChoiceKind routeChoice,
            HazardPatternKind hazardPattern,
            float hazardPhaseStride,
            bool supportsPairSplit,
            string climaxId,
            params string[] moduleTags)
        {
            Sector = sector;
            ActIndex = actIndex;
            Topology = topology;
            Traversal = traversal;
            Objective = objective;
            RouteChoice = routeChoice;
            HazardPattern = hazardPattern;
            HazardPhaseStride = hazardPhaseStride;
            SupportsPairSplit = supportsPairSplit;
            ClimaxId = climaxId;
            ModuleTags = moduleTags ?? Array.Empty<string>();
        }

        public SectorId Sector { get; }
        public int ActIndex { get; }
        public LevelTopologyKind Topology { get; }
        public TraversalEmphasisKind Traversal { get; }
        public EncounterObjectiveKind Objective { get; }
        public RouteChoiceKind RouteChoice { get; }
        public HazardPatternKind HazardPattern { get; }
        public float HazardPhaseStride { get; }
        public bool SupportsPairSplit { get; }
        public string ClimaxId { get; }
        public string[] ModuleTags { get; }

        public string Key =>
            Sector.ToString().ToLowerInvariant() +
            "-act-" +
            (ActIndex + 1);

        public bool UsesModule(string tag)
        {
            if (
                string.IsNullOrWhiteSpace(tag) ||
                ModuleTags == null
            )
            {
                return false;
            }

            for (int i = 0; i < ModuleTags.Length; i++)
            {
                if (
                    string.Equals(
                        ModuleTags[i],
                        tag,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Eighteen-act level-variety matrix. The catalog deliberately encodes
    /// gameplay structure rather than production art so it can drive greybox
    /// layouts now and Blender-authored environments later.
    /// </summary>
    public static class ActLevelVarietyCatalog
    {
        private static readonly ActLevelVarietyReference[] Acts =
        {
            // SKYPORT — transit velocity, moving infrastructure, exposed height.
            V(
                SectorId.Skyport, 0,
                LevelTopologyKind.MovingConvoy,
                TraversalEmphasisKind.Momentum,
                EncounterObjectiveKind.Advance,
                RouteChoiceKind.UpperLower,
                HazardPatternKind.Sweeping,
                0.42f,
                true,
                "rail-sentinel-intercept",
                "moving-lanes",
                "upper-service-route",
                "train-gap-jumps"
            ),
            V(
                SectorId.Skyport, 1,
                LevelTopologyKind.SplitRoute,
                TraversalEmphasisKind.Precision,
                EncounterObjectiveKind.DisableNodes,
                RouteChoiceKind.SafeVsFast,
                HazardPatternKind.AlternatingLanes,
                0.58f,
                true,
                "vector-hound-maintenance-lock",
                "maintenance-branches",
                "laser-shutters",
                "service-shortcut"
            ),
            V(
                SectorId.Skyport, 2,
                LevelTopologyKind.VerticalAscent,
                TraversalEmphasisKind.MovingPlatforms,
                EncounterObjectiveKind.Survival,
                RouteChoiceKind.CombatVsTraversal,
                HazardPatternKind.PursuitPressure,
                0.36f,
                true,
                "aegis-skyline-platform",
                "skyline-lifts",
                "open-air-crossfire",
                "moving-platform-chain"
            ),

            // EMBER WORKS — heat cadence, machinery, shafts, pressure windows.
            V(
                SectorId.EmberWorks, 0,
                LevelTopologyKind.LinearRun,
                TraversalEmphasisKind.HazardTiming,
                EncounterObjectiveKind.HoldZone,
                RouteChoiceKind.RiskReward,
                HazardPatternKind.BurstWindows,
                0.64f,
                false,
                "bulwark-furnace-gate",
                "heat-safe-bays",
                "vent-timing",
                "furnace-catwalks"
            ),
            V(
                SectorId.EmberWorks, 1,
                LevelTopologyKind.VerticalDescent,
                TraversalEmphasisKind.Vertical,
                EncounterObjectiveKind.Survival,
                RouteChoiceKind.UpperLower,
                HazardPatternKind.AlternatingLanes,
                0.48f,
                true,
                "vector-hound-foundry-floor",
                "foundry-shaft",
                "falling-platform-route",
                "crusher-bypass"
            ),
            V(
                SectorId.EmberWorks, 2,
                LevelTopologyKind.LoopingArena,
                TraversalEmphasisKind.Momentum,
                EncounterObjectiveKind.DisableNodes,
                RouteChoiceKind.Dynamic,
                HazardPatternKind.PursuitPressure,
                0.30f,
                true,
                "cinder-core-forge",
                "forge-ring",
                "cooling-node-loop",
                "furnace-surge-lanes"
            ),

            // VERDANT VAULT — organic branching, grapple routes, changing paths.
            V(
                SectorId.VerdantVault, 0,
                LevelTopologyKind.SplitRoute,
                TraversalEmphasisKind.Grapple,
                EncounterObjectiveKind.Advance,
                RouteChoiceKind.CombatVsTraversal,
                HazardPatternKind.Standard,
                0.72f,
                true,
                "cliff-stalker-root-chamber",
                "root-branches",
                "grapple-canopy",
                "spore-bypass"
            ),
            V(
                SectorId.VerdantVault, 1,
                LevelTopologyKind.LayeredArena,
                TraversalEmphasisKind.Grapple,
                EncounterObjectiveKind.DisableNodes,
                RouteChoiceKind.UpperLower,
                HazardPatternKind.AlternatingLanes,
                0.50f,
                true,
                "bulwark-canopy-engine",
                "canopy-layers",
                "engine-nodes",
                "wall-hunter-routes"
            ),
            V(
                SectorId.VerdantVault, 2,
                LevelTopologyKind.ReconfiguringSpace,
                TraversalEmphasisKind.MovingPlatforms,
                EncounterObjectiveKind.MultiFront,
                RouteChoiceKind.Dynamic,
                HazardPatternKind.Reconfiguring,
                0.34f,
                true,
                "mycel-memory-garden",
                "vine-bridge",
                "garden-branches",
                "memory-route-shift"
            ),

            // CRYO RELAY — momentum control, fragile footing, collapse cadence.
            V(
                SectorId.CryoRelay, 0,
                LevelTopologyKind.LinearRun,
                TraversalEmphasisKind.Momentum,
                EncounterObjectiveKind.Advance,
                RouteChoiceKind.SafeVsFast,
                HazardPatternKind.AlternatingLanes,
                0.44f,
                false,
                "rail-sentinel-frozen-array",
                "ice-run",
                "stable-side-route",
                "sliding-crossfire"
            ),
            V(
                SectorId.CryoRelay, 1,
                LevelTopologyKind.VerticalDescent,
                TraversalEmphasisKind.Precision,
                EncounterObjectiveKind.HoldZone,
                RouteChoiceKind.RiskReward,
                HazardPatternKind.BurstWindows,
                0.62f,
                true,
                "bulwark-coolant-vault",
                "coolant-shaft",
                "fragile-ledges",
                "vault-safe-pockets"
            ),
            V(
                SectorId.CryoRelay, 2,
                LevelTopologyKind.ReconfiguringSpace,
                TraversalEmphasisKind.HazardTiming,
                EncounterObjectiveKind.Survival,
                RouteChoiceKind.CombatVsTraversal,
                HazardPatternKind.Reconfiguring,
                0.38f,
                true,
                "rime-glass-relay",
                "glass-collapse",
                "temporary-ice-route",
                "relay-platform-cycle"
            ),

            // STORM SPIRE — speed, electrical lanes, pursuit, vertical exposure.
            V(
                SectorId.StormSpire, 0,
                LevelTopologyKind.VerticalAscent,
                TraversalEmphasisKind.Momentum,
                EncounterObjectiveKind.Advance,
                RouteChoiceKind.UpperLower,
                HazardPatternKind.Sweeping,
                0.46f,
                true,
                "vector-hound-conduit-rise",
                "conduit-ascent",
                "energy-rails",
                "lightning-lanes"
            ),
            V(
                SectorId.StormSpire, 1,
                LevelTopologyKind.LoopingArena,
                TraversalEmphasisKind.HazardTiming,
                EncounterObjectiveKind.MultiFront,
                RouteChoiceKind.RiskReward,
                HazardPatternKind.AlternatingLanes,
                0.54f,
                true,
                "cliff-stalker-thunder-ring",
                "thunder-ring",
                "relay-islands",
                "crossfire-shortcuts"
            ),
            V(
                SectorId.StormSpire, 2,
                LevelTopologyKind.MovingConvoy,
                TraversalEmphasisKind.Momentum,
                EncounterObjectiveKind.Pursuit,
                RouteChoiceKind.Dynamic,
                HazardPatternKind.PursuitPressure,
                0.28f,
                true,
                "tempest-spire-crown",
                "crown-chase",
                "moving-energy-lanes",
                "storm-sprint-route"
            ),

            // ECLIPSE CORE — mastered systems destabilized and recombined.
            V(
                SectorId.EclipseCore, 0,
                LevelTopologyKind.LayeredArena,
                TraversalEmphasisKind.TeamSplit,
                EncounterObjectiveKind.MultiFront,
                RouteChoiceKind.UpperLower,
                HazardPatternKind.Reconfiguring,
                0.40f,
                true,
                "rail-sentinel-outer-shell",
                "shell-layers",
                "paired-fronts",
                "null-grid-shift"
            ),
            V(
                SectorId.EclipseCore, 1,
                LevelTopologyKind.ReconfiguringSpace,
                TraversalEmphasisKind.Precision,
                EncounterObjectiveKind.DisableNodes,
                RouteChoiceKind.CombatVsTraversal,
                HazardPatternKind.Reconfiguring,
                0.32f,
                true,
                "cliff-stalker-memory-lattice",
                "lattice-shift",
                "memory-nodes",
                "false-route-cycle"
            ),
            V(
                SectorId.EclipseCore, 2,
                LevelTopologyKind.ReconfiguringSpace,
                TraversalEmphasisKind.TeamSplit,
                EncounterObjectiveKind.Survival,
                RouteChoiceKind.Dynamic,
                HazardPatternKind.Reconfiguring,
                0.24f,
                true,
                "null-dawn-engine",
                "dawn-warp",
                "formation-split",
                "final-route-collapse"
            )
        };

        public static int Count => Acts.Length;

        public static ActLevelVarietyReference Get(
            int sectorIndex,
            int actIndex)
        {
            int sector =
                sectorIndex < 0
                    ? 0
                    : sectorIndex >= CampaignCatalog.SectorCount
                        ? CampaignCatalog.SectorCount - 1
                        : sectorIndex;

            int act =
                actIndex < 0
                    ? 0
                    : actIndex >= CampaignCatalog.ActsPerSector
                        ? CampaignCatalog.ActsPerSector - 1
                        : actIndex;

            return Acts[
                sector * CampaignCatalog.ActsPerSector +
                act
            ];
        }

        public static ActLevelVarietyReference Get(
            SectorId sector,
            int actIndex)
        {
            return Get((int)sector, actIndex);
        }

        private static ActLevelVarietyReference V(
            SectorId sector,
            int actIndex,
            LevelTopologyKind topology,
            TraversalEmphasisKind traversal,
            EncounterObjectiveKind objective,
            RouteChoiceKind routeChoice,
            HazardPatternKind hazardPattern,
            float hazardPhaseStride,
            bool supportsPairSplit,
            string climaxId,
            params string[] moduleTags)
        {
            return new ActLevelVarietyReference(
                sector,
                actIndex,
                topology,
                traversal,
                objective,
                routeChoice,
                hazardPattern,
                hazardPhaseStride,
                supportsPairSplit,
                climaxId,
                moduleTags
            );
        }
    }
}
