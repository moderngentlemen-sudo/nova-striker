using System;
using System.Collections.Generic;

namespace NovaStriker.Progression
{
    [Serializable]
    public sealed class WeaponMasterySave
    {
        public string weaponId;
        public float experience;
        public int level;
    }

    [Serializable]
    public sealed class PlayerCosmeticSave
    {
        public int playerSlot;
        public string suitCosmeticId;
        public string helmetCosmeticId;
        public string weaponCosmeticId;
        public string trailCosmeticId;
    }

    [Serializable]
    public sealed class NovaSaveData
    {
        public int version = CurrentVersion;

        public const int CurrentVersion = 2;

        public int sectorIndex;
        public int actIndex;
        public int checkpointIndex;
        public int skillPoints;

        public List<string> unlockedWeapons = new();
        public List<string> unlockedGuardians = new();
        public List<string> unlockedSkills = new();

        public List<WeaponMasterySave> weaponMastery = new();

        // Storefront-neutral ownership. The platform commerce layer reconciles
        // these IDs with native receipts/entitlements before gated content is
        // exposed.
        public List<string> ownedEntitlements = new();
        public List<string> ownedCosmetics = new();
        public List<PlayerCosmeticSave> equippedCosmetics = new();

        public static NovaSaveData CreateDefault()
        {
            NovaSaveData data = new()
            {
                version = CurrentVersion,
                sectorIndex = 0,
                actIndex = 0,
                checkpointIndex = 0,
                skillPoints = 0
            };

            data.unlockedWeapons.Add("pulse");
            data.unlockedGuardians.Add("Aegis");

            for (int i = 0; i < 4; i++)
            {
                data.equippedCosmetics.Add(
                    new PlayerCosmeticSave
                    {
                        playerSlot = i,
                        suitCosmeticId = "default",
                        helmetCosmeticId = "default",
                        weaponCosmeticId = "default",
                        trailCosmeticId = "default"
                    }
                );
            }

            return data;
        }
    }
}
