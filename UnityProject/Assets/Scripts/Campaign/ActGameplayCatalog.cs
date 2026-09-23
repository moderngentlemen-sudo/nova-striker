using NovaStriker.Data;
using NovaStriker.Enemies;

namespace NovaStriker.Campaign
{
    public enum ActBeatKind
    {
        Skirmish = 0,
        Pressure = 1,
        Elite = 2,
        Setpiece = 3,
        MiniBoss = 4,
        Guardian = 5
    }

    public readonly struct ActEnemyGroup
    {
        public ActEnemyGroup(
            EnemyArchetype archetype,
            int baseCount,
            int extraPerAdditionalPlayer = 1)
        {
            Archetype = archetype;
            BaseCount = baseCount;
            ExtraPerAdditionalPlayer =
                extraPerAdditionalPlayer;
        }

        public EnemyArchetype Archetype { get; }
        public int BaseCount { get; }
        public int ExtraPerAdditionalPlayer { get; }
    }

    public readonly struct ActEncounterBeat
    {
        public ActEncounterBeat(
            string id,
            ActBeatKind kind,
            float startDelay,
            params ActEnemyGroup[] groups)
        {
            Id = id;
            Kind = kind;
            StartDelay = startDelay;
            Groups = groups;
        }

        public string Id { get; }
        public ActBeatKind Kind { get; }
        public float StartDelay { get; }
        public ActEnemyGroup[] Groups { get; }
    }

    public readonly struct ActGameplayReference
    {
        public ActGameplayReference(
            SectorId sector,
            int actIndex,
            string displayName,
            HazardId hazard,
            SetpieceId setpiece,
            float setpieceIntensity,
            SecretChallengeKind secretKind,
            bool hasMiniBoss,
            MiniBossId miniBoss,
            bool hasGuardian,
            GuardianId guardian,
            ActEncounterBeat[] beats)
        {
            Sector = sector;
            ActIndex = actIndex;
            DisplayName = displayName;
            Hazard = hazard;
            Setpiece = setpiece;
            SetpieceIntensity = setpieceIntensity;
            SecretKind = secretKind;
            HasMiniBoss = hasMiniBoss;
            MiniBoss = miniBoss;
            HasGuardian = hasGuardian;
            Guardian = guardian;
            Beats = beats;
        }

        public SectorId Sector { get; }
        public int ActIndex { get; }
        public string DisplayName { get; }
        public HazardId Hazard { get; }
        public SetpieceId Setpiece { get; }
        public float SetpieceIntensity { get; }
        public SecretChallengeKind SecretKind { get; }
        public bool HasMiniBoss { get; }
        public MiniBossId MiniBoss { get; }
        public bool HasGuardian { get; }
        public GuardianId Guardian { get; }
        public ActEncounterBeat[] Beats { get; }

        public string Key =>
            Sector.ToString().ToLowerInvariant() +
            "-act-" +
            (ActIndex + 1);
    }

    /// <summary>
    /// Full eighteen-act combat composition. Counts are base one-player values;
    /// each group declares how it scales for additional local players. Scene
    /// layout, art and authored spawn transforms stay external to this catalog.
    /// </summary>
    public static class ActGameplayCatalog
    {
        private static readonly ActGameplayReference[] Acts =
        {
            // SKYPORT
            Act(
                SectorId.Skyport, 0, SecretChallengeKind.DashCourse, 0.25f,
                MiniBossId.RailSentinel,
                Beat("arrival", ActBeatKind.Skirmish,
                    G(EnemyArchetype.Walker, 3),
                    G(EnemyArchetype.Drone, 1, 0)),
                Beat("platform-crossfire", ActBeatKind.Pressure,
                    G(EnemyArchetype.Turret, 1, 0),
                    G(EnemyArchetype.Hopper, 2)),
                Beat("rail-sentinel", ActBeatKind.MiniBoss)
            ),
            Act(
                SectorId.Skyport, 1, SecretChallengeKind.WeaponTrial, 0.55f,
                MiniBossId.VectorHound,
                Beat("maintenance-lock", ActBeatKind.Skirmish,
                    G(EnemyArchetype.Shield, 1, 0),
                    G(EnemyArchetype.Walker, 2)),
                Beat("service-pincer", ActBeatKind.Elite,
                    G(EnemyArchetype.Charger, 1),
                    G(EnemyArchetype.Interceptor, 1),
                    G(EnemyArchetype.Turret, 1, 0)),
                Beat("vector-hound", ActBeatKind.MiniBoss)
            ),
            GuardianAct(
                SectorId.Skyport, SecretChallengeKind.Gauntlet,
                Beat("skyline-lift", ActBeatKind.Setpiece,
                    G(EnemyArchetype.Drone, 2),
                    G(EnemyArchetype.Orbiter, 1)),
                Beat("upper-crossfire", ActBeatKind.Elite,
                    G(EnemyArchetype.Interceptor, 2),
                    G(EnemyArchetype.Sniper, 1, 0),
                    G(EnemyArchetype.Shield, 1, 0)),
                Beat("aegis", ActBeatKind.Guardian)
            ),

            // EMBER WORKS
            Act(
                SectorId.EmberWorks, 0, SecretChallengeKind.WeaponTrial, 0.30f,
                MiniBossId.Bulwark,
                Beat("furnace-entry", ActBeatKind.Skirmish,
                    G(EnemyArchetype.Walker, 2),
                    G(EnemyArchetype.Heavy, 1, 0)),
                Beat("vent-line", ActBeatKind.Pressure,
                    G(EnemyArchetype.Charger, 2),
                    G(EnemyArchetype.Turret, 1, 0)),
                Beat("bulwark", ActBeatKind.MiniBoss)
            ),
            Act(
                SectorId.EmberWorks, 1, SecretChallengeKind.Gauntlet, 0.60f,
                MiniBossId.VectorHound,
                Beat("foundry-guard", ActBeatKind.Pressure,
                    G(EnemyArchetype.Guard, 1, 0),
                    G(EnemyArchetype.Hopper, 2)),
                Beat("shaft-collapse", ActBeatKind.Elite,
                    G(EnemyArchetype.Heavy, 1, 0),
                    G(EnemyArchetype.Charger, 1),
                    G(EnemyArchetype.Sniper, 1, 0)),
                Beat("vector-hound", ActBeatKind.MiniBoss)
            ),
            GuardianAct(
                SectorId.EmberWorks, SecretChallengeKind.DashCourse,
                Beat("core-forge-rush", ActBeatKind.Setpiece,
                    G(EnemyArchetype.Shield, 1, 0),
                    G(EnemyArchetype.Charger, 2),
                    G(EnemyArchetype.Drone, 1)),
                Beat("forge-lock", ActBeatKind.Elite,
                    G(EnemyArchetype.Guard, 1),
                    G(EnemyArchetype.Heavy, 1, 0),
                    G(EnemyArchetype.Interceptor, 1)),
                Beat("cinder", ActBeatKind.Guardian)
            ),

            // VERDANT VAULT
            Act(
                SectorId.VerdantVault, 0, SecretChallengeKind.DashCourse, 0.30f,
                MiniBossId.CliffStalker,
                Beat("root-skirmish", ActBeatKind.Skirmish,
                    G(EnemyArchetype.Hopper, 3),
                    G(EnemyArchetype.Drone, 1, 0)),
                Beat("spore-pressure", ActBeatKind.Pressure,
                    G(EnemyArchetype.WallHunter, 1),
                    G(EnemyArchetype.Walker, 2)),
                Beat("cliff-stalker", ActBeatKind.MiniBoss)
            ),
            Act(
                SectorId.VerdantVault, 1, SecretChallengeKind.Gauntlet, 0.55f,
                MiniBossId.Bulwark,
                Beat("canopy-orbit", ActBeatKind.Pressure,
                    G(EnemyArchetype.Orbiter, 2),
                    G(EnemyArchetype.Hopper, 2)),
                Beat("engine-guard", ActBeatKind.Elite,
                    G(EnemyArchetype.Guard, 1, 0),
                    G(EnemyArchetype.WallHunter, 2),
                    G(EnemyArchetype.Turret, 1, 0)),
                Beat("bulwark", ActBeatKind.MiniBoss)
            ),
            GuardianAct(
                SectorId.VerdantVault, SecretChallengeKind.WeaponTrial,
                Beat("vine-bridge", ActBeatKind.Setpiece,
                    G(EnemyArchetype.Drone, 2),
                    G(EnemyArchetype.WallHunter, 2)),
                Beat("memory-garden", ActBeatKind.Elite,
                    G(EnemyArchetype.Heavy, 1, 0),
                    G(EnemyArchetype.Guard, 1),
                    G(EnemyArchetype.Orbiter, 1)),
                Beat("mycel", ActBeatKind.Guardian)
            ),

            // CRYO RELAY
            Act(
                SectorId.CryoRelay, 0, SecretChallengeKind.Gauntlet, 0.30f,
                MiniBossId.RailSentinel,
                Beat("frozen-array", ActBeatKind.Skirmish,
                    G(EnemyArchetype.Walker, 2),
                    G(EnemyArchetype.Sniper, 1, 0)),
                Beat("ice-pincer", ActBeatKind.Pressure,
                    G(EnemyArchetype.Interceptor, 2),
                    G(EnemyArchetype.Shield, 1, 0)),
                Beat("rail-sentinel", ActBeatKind.MiniBoss)
            ),
            Act(
                SectorId.CryoRelay, 1, SecretChallengeKind.WeaponTrial, 0.60f,
                MiniBossId.Bulwark,
                Beat("coolant-guard", ActBeatKind.Pressure,
                    G(EnemyArchetype.Guard, 1, 0),
                    G(EnemyArchetype.Hopper, 2)),
                Beat("vault-crossfire", ActBeatKind.Elite,
                    G(EnemyArchetype.Turret, 1, 0),
                    G(EnemyArchetype.Sniper, 1, 0),
                    G(EnemyArchetype.Heavy, 1)),
                Beat("bulwark", ActBeatKind.MiniBoss)
            ),
            GuardianAct(
                SectorId.CryoRelay, SecretChallengeKind.DashCourse,
                Beat("glass-collapse", ActBeatKind.Setpiece,
                    G(EnemyArchetype.Drone, 2),
                    G(EnemyArchetype.Interceptor, 2)),
                Beat("relay-lock", ActBeatKind.Elite,
                    G(EnemyArchetype.Shield, 1),
                    G(EnemyArchetype.Sniper, 1),
                    G(EnemyArchetype.Heavy, 1, 0)),
                Beat("rime", ActBeatKind.Guardian)
            ),

            // STORM SPIRE
            Act(
                SectorId.StormSpire, 0, SecretChallengeKind.WeaponTrial, 0.35f,
                MiniBossId.VectorHound,
                Beat("lower-conduit", ActBeatKind.Skirmish,
                    G(EnemyArchetype.Charger, 2),
                    G(EnemyArchetype.Drone, 1)),
                Beat("lightning-lane", ActBeatKind.Pressure,
                    G(EnemyArchetype.Interceptor, 2),
                    G(EnemyArchetype.Turret, 1, 0)),
                Beat("vector-hound", ActBeatKind.MiniBoss)
            ),
            Act(
                SectorId.StormSpire, 1, SecretChallengeKind.DashCourse, 0.65f,
                MiniBossId.CliffStalker,
                Beat("thunder-ring", ActBeatKind.Pressure,
                    G(EnemyArchetype.Orbiter, 2),
                    G(EnemyArchetype.Hopper, 2)),
                Beat("ring-crossfire", ActBeatKind.Elite,
                    G(EnemyArchetype.Sniper, 1),
                    G(EnemyArchetype.Guard, 1),
                    G(EnemyArchetype.WallHunter, 1)),
                Beat("cliff-stalker", ActBeatKind.MiniBoss)
            ),
            GuardianAct(
                SectorId.StormSpire, SecretChallengeKind.Gauntlet,
                Beat("crown-chase", ActBeatKind.Setpiece,
                    G(EnemyArchetype.Charger, 2),
                    G(EnemyArchetype.Drone, 2)),
                Beat("spire-crown", ActBeatKind.Elite,
                    G(EnemyArchetype.Interceptor, 2),
                    G(EnemyArchetype.Sniper, 1),
                    G(EnemyArchetype.Shield, 1)),
                Beat("tempest", ActBeatKind.Guardian)
            ),

            // ECLIPSE CORE
            Act(
                SectorId.EclipseCore, 0, SecretChallengeKind.Gauntlet, 0.40f,
                MiniBossId.RailSentinel,
                Beat("outer-shell", ActBeatKind.Pressure,
                    G(EnemyArchetype.Guard, 1),
                    G(EnemyArchetype.Interceptor, 2),
                    G(EnemyArchetype.Drone, 1)),
                Beat("null-crossfire", ActBeatKind.Elite,
                    G(EnemyArchetype.Sniper, 1),
                    G(EnemyArchetype.Heavy, 1),
                    G(EnemyArchetype.Orbiter, 1)),
                Beat("rail-sentinel", ActBeatKind.MiniBoss)
            ),
            Act(
                SectorId.EclipseCore, 1, SecretChallengeKind.WeaponTrial, 0.70f,
                MiniBossId.CliffStalker,
                Beat("memory-lattice", ActBeatKind.Elite,
                    G(EnemyArchetype.Shield, 1),
                    G(EnemyArchetype.WallHunter, 2),
                    G(EnemyArchetype.Hopper, 2)),
                Beat("lattice-collapse", ActBeatKind.Setpiece,
                    G(EnemyArchetype.Guard, 1),
                    G(EnemyArchetype.Sniper, 1),
                    G(EnemyArchetype.Interceptor, 2)),
                Beat("cliff-stalker", ActBeatKind.MiniBoss)
            ),
            GuardianAct(
                SectorId.EclipseCore, SecretChallengeKind.DashCourse,
                Beat("dawn-warp", ActBeatKind.Setpiece,
                    G(EnemyArchetype.Charger, 2),
                    G(EnemyArchetype.Orbiter, 2),
                    G(EnemyArchetype.Heavy, 1, 0)),
                Beat("final-formation", ActBeatKind.Elite,
                    G(EnemyArchetype.Guard, 1),
                    G(EnemyArchetype.Sniper, 1),
                    G(EnemyArchetype.Interceptor, 2),
                    G(EnemyArchetype.Shield, 1)),
                Beat("null", ActBeatKind.Guardian)
            )
        };

        public static int Count => Acts.Length;

        public static ActGameplayReference Get(
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

        public static ActGameplayReference Get(
            SectorId sector,
            int actIndex)
        {
            return Get((int)sector, actIndex);
        }

        private static ActGameplayReference Act(
            SectorId sector,
            int actIndex,
            SecretChallengeKind secret,
            float setpieceIntensity,
            MiniBossId miniBoss,
            params ActEncounterBeat[] beats)
        {
            SectorReference sectorReference =
                CampaignCatalog.Get(sector);

            return new ActGameplayReference(
                sector,
                actIndex,
                sectorReference.ActName(actIndex),
                sectorReference.Hazard,
                sectorReference.Setpiece,
                setpieceIntensity,
                secret,
                true,
                miniBoss,
                false,
                sectorReference.Guardian,
                beats
            );
        }

        private static ActGameplayReference GuardianAct(
            SectorId sector,
            SecretChallengeKind secret,
            params ActEncounterBeat[] beats)
        {
            SectorReference sectorReference =
                CampaignCatalog.Get(sector);

            return new ActGameplayReference(
                sector,
                2,
                sectorReference.ActName(2),
                sectorReference.Hazard,
                sectorReference.Setpiece,
                1f,
                secret,
                false,
                sectorReference.MiniBossB,
                true,
                sectorReference.Guardian,
                beats
            );
        }

        private static ActEncounterBeat Beat(
            string id,
            ActBeatKind kind,
            params ActEnemyGroup[] groups)
        {
            return new ActEncounterBeat(
                id,
                kind,
                0.35f,
                groups
            );
        }

        private static ActEnemyGroup G(
            EnemyArchetype archetype,
            int baseCount,
            int extraPerAdditionalPlayer = 1)
        {
            return new ActEnemyGroup(
                archetype,
                baseCount,
                extraPerAdditionalPlayer
            );
        }
    }
}
