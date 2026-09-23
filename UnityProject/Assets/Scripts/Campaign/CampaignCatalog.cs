using NovaStriker.Data;

namespace NovaStriker.Campaign
{
    public enum SectorId
    {
        Skyport = 0,
        EmberWorks = 1,
        VerdantVault = 2,
        CryoRelay = 3,
        StormSpire = 4,
        EclipseCore = 5
    }

    public enum HazardId
    {
        Laser = 0,
        Vent = 1,
        Spore = 2,
        Ice = 3,
        Lightning = 4,
        NullGrid = 5
    }

    public enum SetpieceId
    {
        TrainRush = 0,
        FurnaceSurge = 1,
        VineBridge = 2,
        IceCollapse = 3,
        LightningChase = 4,
        NullWarp = 5
    }

    public enum MiniBossId
    {
        Bulwark = 0,
        VectorHound = 1,
        CliffStalker = 2,
        RailSentinel = 3
    }

    public readonly struct SectorReference
    {
        public readonly SectorId Id;
        public readonly string DisplayName;
        public readonly string Act1;
        public readonly string Act2;
        public readonly string Act3;
        public readonly GuardianId Guardian;
        public readonly HazardId Hazard;
        public readonly SetpieceId Setpiece;
        public readonly MiniBossId MiniBossA;
        public readonly MiniBossId MiniBossB;

        public SectorReference(
            SectorId id,
            string displayName,
            string act1,
            string act2,
            string act3,
            GuardianId guardian,
            HazardId hazard,
            SetpieceId setpiece,
            MiniBossId miniBossA,
            MiniBossId miniBossB)
        {
            Id = id;
            DisplayName = displayName;
            Act1 = act1;
            Act2 = act2;
            Act3 = act3;
            Guardian = guardian;
            Hazard = hazard;
            Setpiece = setpiece;
            MiniBossA = miniBossA;
            MiniBossB = miniBossB;
        }

        public string ActName(int actIndex)
        {
            return actIndex switch
            {
                <= 0 => Act1,
                1 => Act2,
                _ => Act3
            };
        }
    }

    /// <summary>
    /// Six-sector / eighteen-act campaign reference carried forward from the
    /// established game structure.
    /// </summary>
    public static class CampaignCatalog
    {
        public const int SectorCount = 6;
        public const int ActsPerSector = 3;

        public static SectorReference Get(SectorId id)
        {
            return id switch
            {
                SectorId.Skyport =>
                    new SectorReference(
                        id,
                        "Skyport",
                        "Transit Spine",
                        "Maintenance Interior",
                        "Upper Skyline",
                        GuardianId.Aegis,
                        HazardId.Laser,
                        SetpieceId.TrainRush,
                        MiniBossId.RailSentinel,
                        MiniBossId.VectorHound
                    ),

                SectorId.EmberWorks =>
                    new SectorReference(
                        id,
                        "Ember Works",
                        "Furnace Walk",
                        "Foundry Shaft",
                        "Core Forge",
                        GuardianId.Cinder,
                        HazardId.Vent,
                        SetpieceId.FurnaceSurge,
                        MiniBossId.Bulwark,
                        MiniBossId.VectorHound
                    ),

                SectorId.VerdantVault =>
                    new SectorReference(
                        id,
                        "Verdant Vault",
                        "Root Access",
                        "Canopy Engine",
                        "Memory Garden",
                        GuardianId.Mycel,
                        HazardId.Spore,
                        SetpieceId.VineBridge,
                        MiniBossId.CliffStalker,
                        MiniBossId.Bulwark
                    ),

                SectorId.CryoRelay =>
                    new SectorReference(
                        id,
                        "Cryo Relay",
                        "Frozen Array",
                        "Coolant Vault",
                        "Glass Relay",
                        GuardianId.Rime,
                        HazardId.Ice,
                        SetpieceId.IceCollapse,
                        MiniBossId.RailSentinel,
                        MiniBossId.Bulwark
                    ),

                SectorId.StormSpire =>
                    new SectorReference(
                        id,
                        "Storm Spire",
                        "Lower Conduit",
                        "Thunder Ring",
                        "Spire Crown",
                        GuardianId.Tempest,
                        HazardId.Lightning,
                        SetpieceId.LightningChase,
                        MiniBossId.VectorHound,
                        MiniBossId.CliffStalker
                    ),

                _ =>
                    new SectorReference(
                        SectorId.EclipseCore,
                        "Eclipse Core",
                        "Outer Shell",
                        "Memory Lattice",
                        "Dawn Engine",
                        GuardianId.Null,
                        HazardId.NullGrid,
                        SetpieceId.NullWarp,
                        MiniBossId.RailSentinel,
                        MiniBossId.CliffStalker
                    )
            };
        }

        public static SectorReference Get(int sectorIndex)
        {
            return Get(
                (SectorId)System.Math.Clamp(
                    sectorIndex,
                    0,
                    SectorCount - 1
                )
            );
        }
    }
}
