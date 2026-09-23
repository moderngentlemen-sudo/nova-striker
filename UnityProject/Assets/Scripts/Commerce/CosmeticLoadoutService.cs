using System;
using NovaStriker.Progression;
using UnityEngine;

namespace NovaStriker.Commerce
{
    public enum CosmeticSlot
    {
        Suit = 0,
        Helmet = 1,
        Weapon = 2,
        Trail = 3
    }

    /// <summary>
    /// Storefront-neutral cosmetic equip service. Ownership is verified through
    /// EntitlementService and equipped IDs persist per local player slot.
    /// Cosmetics never modify gameplay stats.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CosmeticLoadoutService : MonoBehaviour
    {
        [SerializeField] private SaveGameService saveService;
        [SerializeField] private EntitlementService entitlements;

        public static CosmeticLoadoutService Active { get; private set; }

        public event Action<int, CosmeticSlot, string> CosmeticEquipped;

        private void Awake()
        {
            Active = this;

            if (!saveService)
                saveService = SaveGameService.Active;

            if (!entitlements)
                entitlements = EntitlementService.Active;
        }

        private void OnDestroy()
        {
            if (Active == this)
                Active = null;
        }

        public string GetEquipped(
            int playerSlot,
            CosmeticSlot slot)
        {
            PlayerCosmeticSave save =
                GetPlayerCosmetic(playerSlot);

            if (save == null)
                return "default";

            return slot switch
            {
                CosmeticSlot.Helmet =>
                    string.IsNullOrEmpty(save.helmetCosmeticId)
                        ? "default"
                        : save.helmetCosmeticId,

                CosmeticSlot.Weapon =>
                    string.IsNullOrEmpty(save.weaponCosmeticId)
                        ? "default"
                        : save.weaponCosmeticId,

                CosmeticSlot.Trail =>
                    string.IsNullOrEmpty(save.trailCosmeticId)
                        ? "default"
                        : save.trailCosmeticId,

                _ =>
                    string.IsNullOrEmpty(save.suitCosmeticId)
                        ? "default"
                        : save.suitCosmeticId
            };
        }

        public bool TryEquip(
            int playerSlot,
            CosmeticSlot slot,
            string cosmeticId)
        {
            cosmeticId =
                string.IsNullOrEmpty(cosmeticId)
                    ? "default"
                    : cosmeticId;

            if (
                cosmeticId != "default" &&
                (
                    !entitlements ||
                    !entitlements.OwnsCosmetic(cosmeticId)
                )
            )
            {
                return false;
            }

            PlayerCosmeticSave save =
                GetOrCreatePlayerCosmetic(playerSlot);

            if (save == null)
                return false;

            switch (slot)
            {
                case CosmeticSlot.Helmet:
                    save.helmetCosmeticId = cosmeticId;
                    break;

                case CosmeticSlot.Weapon:
                    save.weaponCosmeticId = cosmeticId;
                    break;

                case CosmeticSlot.Trail:
                    save.trailCosmeticId = cosmeticId;
                    break;

                default:
                    save.suitCosmeticId = cosmeticId;
                    break;
            }

            saveService?.Save();

            CosmeticEquipped?.Invoke(
                Mathf.Clamp(playerSlot, 0, 3),
                slot,
                cosmeticId
            );

            return true;
        }

        private PlayerCosmeticSave GetPlayerCosmetic(int playerSlot)
        {
            if (
                !saveService ||
                saveService.Current == null
            )
            {
                return null;
            }

            int slot = Mathf.Clamp(playerSlot, 0, 3);

            for (
                int i = 0;
                i < saveService.Current.equippedCosmetics.Count;
                i++
            )
            {
                PlayerCosmeticSave item =
                    saveService.Current.equippedCosmetics[i];

                if (item != null && item.playerSlot == slot)
                    return item;
            }

            return null;
        }

        private PlayerCosmeticSave GetOrCreatePlayerCosmetic(
            int playerSlot)
        {
            PlayerCosmeticSave existing =
                GetPlayerCosmetic(playerSlot);

            if (existing != null)
                return existing;

            if (
                !saveService ||
                saveService.Current == null
            )
            {
                return null;
            }

            PlayerCosmeticSave created = new()
            {
                playerSlot = Mathf.Clamp(playerSlot, 0, 3),
                suitCosmeticId = "default",
                helmetCosmeticId = "default",
                weaponCosmeticId = "default",
                trailCosmeticId = "default"
            };

            saveService.Current.equippedCosmetics.Add(created);
            return created;
        }
    }
}
