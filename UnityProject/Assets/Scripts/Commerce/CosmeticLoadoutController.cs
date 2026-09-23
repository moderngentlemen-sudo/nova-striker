using System;
using NovaStriker.Progression;
using UnityEngine;

namespace NovaStriker.Commerce
{
    /// <summary>
    /// Per-player cosmetic selection backed by save data and storefront-neutral
    /// entitlements. Cosmetic IDs are presentation data only and never alter
    /// gameplay statistics.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CosmeticLoadoutController : MonoBehaviour
    {
        [SerializeField, Range(0, 3)] private int playerSlot;
        [SerializeField] private SaveGameService saveService;
        [SerializeField] private EntitlementService entitlementService;

        public event Action<CosmeticSlot, string> CosmeticChanged;

        private void Awake()
        {
            ResolveServices();
        }

        public void ConfigurePlayerSlot(int slot)
        {
            playerSlot = Mathf.Clamp(slot, 0, 3);
        }

        public string GetEquipped(CosmeticSlot slot)
        {
            ResolveServices();

            PlayerCosmeticSave save = GetOrCreatePlayerSave();
            if (save == null)
                return "default";

            return slot switch
            {
                CosmeticSlot.Helmet =>
                    Normalize(save.helmetCosmeticId),
                CosmeticSlot.Weapon =>
                    Normalize(save.weaponCosmeticId),
                CosmeticSlot.Trail =>
                    Normalize(save.trailCosmeticId),
                _ =>
                    Normalize(save.suitCosmeticId)
            };
        }

        public bool TryEquip(
            CosmeticSlot slot,
            string cosmeticId)
        {
            ResolveServices();

            string id = Normalize(cosmeticId);

            if (
                id != "default" &&
                (
                    !entitlementService ||
                    !entitlementService.OwnsCosmetic(id)
                )
            )
            {
                return false;
            }

            PlayerCosmeticSave save = GetOrCreatePlayerSave();

            if (save == null)
                return false;

            switch (slot)
            {
                case CosmeticSlot.Helmet:
                    save.helmetCosmeticId = id;
                    break;
                case CosmeticSlot.Weapon:
                    save.weaponCosmeticId = id;
                    break;
                case CosmeticSlot.Trail:
                    save.trailCosmeticId = id;
                    break;
                default:
                    save.suitCosmeticId = id;
                    break;
            }

            saveService.Save();
            CosmeticChanged?.Invoke(slot, id);
            return true;
        }

        private void ResolveServices()
        {
            if (!saveService)
                saveService = SaveGameService.Active;

            if (!entitlementService)
                entitlementService = EntitlementService.Active;
        }

        private PlayerCosmeticSave GetOrCreatePlayerSave()
        {
            if (
                !saveService ||
                saveService.Current == null
            )
            {
                return null;
            }

            for (
                int i = 0;
                i < saveService.Current.equippedCosmetics.Count;
                i++
            )
            {
                PlayerCosmeticSave entry =
                    saveService.Current.equippedCosmetics[i];

                if (
                    entry != null &&
                    entry.playerSlot == playerSlot
                )
                {
                    return entry;
                }
            }

            PlayerCosmeticSave created = new()
            {
                playerSlot = playerSlot,
                suitCosmeticId = "default",
                helmetCosmeticId = "default",
                weaponCosmeticId = "default",
                trailCosmeticId = "default"
            };

            saveService.Current.equippedCosmetics.Add(created);
            return created;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrEmpty(value)
                ? "default"
                : value;
        }
    }
}
