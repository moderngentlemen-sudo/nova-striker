using System;
using NovaStriker.Progression;
using UnityEngine;

namespace NovaStriker.Commerce
{
    /// <summary>
    /// Storefront-neutral entitlement authority. Platform providers verify
    /// purchases; gameplay only asks whether an entitlement is owned.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EntitlementService : MonoBehaviour
    {
        [SerializeField] private CommerceCatalog catalog;
        [SerializeField] private SaveGameService saveService;
        [SerializeField] private bool useMockProviderInEditor = true;

        private ICommerceProvider provider;

        public static EntitlementService Active { get; private set; }

        public event Action<string> EntitlementGranted;
        public event Action<string, string> PurchaseFailed;

        private void Awake()
        {
            Active = this;

            if (!saveService)
                saveService = SaveGameService.Active;

#if UNITY_EDITOR
            if (useMockProviderInEditor)
                SetProvider(new MockCommerceProvider());
#endif
        }

        private void OnDestroy()
        {
            DetachProvider();

            if (Active == this)
                Active = null;
        }

        public void SetProvider(ICommerceProvider value)
        {
            DetachProvider();
            provider = value;

            if (provider == null)
                return;

            provider.PurchaseSucceeded += OnPurchaseSucceeded;
            provider.PurchaseFailed += OnPurchaseFailed;
            provider.Initialize(catalog);
        }

        public bool HasEntitlement(string entitlementId)
        {
            if (
                string.IsNullOrEmpty(entitlementId) ||
                !saveService ||
                saveService.Current == null
            )
            {
                return false;
            }

            return
                saveService.Current.ownedEntitlements.Contains(
                    entitlementId
                );
        }

        public bool OwnsCosmetic(string cosmeticId)
        {
            if (
                cosmeticId == "default" ||
                string.IsNullOrEmpty(cosmeticId)
            )
            {
                return true;
            }

            if (
                !saveService ||
                saveService.Current == null
            )
            {
                return false;
            }

            return
                saveService.Current.ownedCosmetics.Contains(
                    cosmeticId
                );
        }

        public void Purchase(string productId)
        {
            if (provider == null || !provider.Ready)
            {
                PurchaseFailed?.Invoke(
                    productId,
                    "No initialized commerce provider."
                );
                return;
            }

            provider.Purchase(productId);
        }

        public void RestorePurchases()
        {
            provider?.RestorePurchases();
        }

        public void GrantEntitlementForDevelopment(
            string entitlementId,
            bool cosmetic = false)
        {
            GrantEntitlement(
                entitlementId,
                cosmetic
            );
        }

        private void OnPurchaseSucceeded(string productId)
        {
            CommerceProductDefinition product =
                catalog ? catalog.Find(productId) : null;

            if (product == null)
            {
                PurchaseFailed?.Invoke(
                    productId,
                    "Purchased product is not present in the local catalog."
                );
                return;
            }

            GrantEntitlement(
                product.EntitlementId,
                product.Type == CommerceProductType.Cosmetic
            );
        }

        private void GrantEntitlement(
            string entitlementId,
            bool cosmetic)
        {
            if (
                string.IsNullOrEmpty(entitlementId) ||
                !saveService ||
                saveService.Current == null
            )
            {
                return;
            }

            if (
                !saveService.Current.ownedEntitlements.Contains(
                    entitlementId
                )
            )
            {
                saveService.Current.ownedEntitlements.Add(
                    entitlementId
                );
            }

            if (
                cosmetic &&
                !saveService.Current.ownedCosmetics.Contains(
                    entitlementId
                )
            )
            {
                saveService.Current.ownedCosmetics.Add(
                    entitlementId
                );
            }

            saveService.Save();
            EntitlementGranted?.Invoke(entitlementId);
        }

        private void OnPurchaseFailed(
            string productId,
            string reason)
        {
            PurchaseFailed?.Invoke(
                productId,
                reason
            );
        }

        private void DetachProvider()
        {
            if (provider == null)
                return;

            provider.PurchaseSucceeded -= OnPurchaseSucceeded;
            provider.PurchaseFailed -= OnPurchaseFailed;
            provider = null;
        }
    }
}
