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
        [SerializeField] private bool reconcileOnInitialize = true;
        [SerializeField] private bool requireReconciliationBeforeUse;

        private ICommerceProvider provider;
        private ICommerceEntitlementReconciliationProvider reconciliationProvider;
        private bool reconciled;

        public static EntitlementService Active { get; private set; }

        public event Action<string> EntitlementGranted;
        public event Action<string> EntitlementRevoked;
        public event Action<string, string> PurchaseFailed;
        public event Action RestoreCompleted;

        public bool Reconciled => reconciled;

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
            reconciled = false;

            if (provider == null)
                return;

            provider.PurchaseSucceeded += OnPurchaseSucceeded;
            provider.PurchaseFailed += OnPurchaseFailed;
            provider.RestoreCompleted += OnRestoreCompleted;

            reconciliationProvider =
                provider as ICommerceEntitlementReconciliationProvider;

            if (reconciliationProvider != null)
            {
                reconciliationProvider.ProductEntitlementConfirmed +=
                    OnProductEntitlementConfirmed;

                reconciliationProvider.ProductEntitlementRevoked +=
                    OnProductEntitlementRevoked;
            }

            provider.Initialize(catalog);

            if (reconcileOnInitialize && provider.Ready)
            {
                if (reconciliationProvider != null)
                    reconciliationProvider.RefreshEntitlements();
                else
                    provider.RestorePurchases();
            }
            else if (!reconcileOnInitialize)
            {
                reconciled = true;
            }
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

            if (
                requireReconciliationBeforeUse &&
                provider != null &&
                !reconciled
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
            reconciled = false;

            if (reconciliationProvider != null)
                reconciliationProvider.RefreshEntitlements();
            else
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
            GrantProductEntitlement(
                productId,
                "Purchased product is not present in the local catalog."
            );
        }

        private void OnProductEntitlementConfirmed(
            string productId)
        {
            GrantProductEntitlement(
                productId,
                "Confirmed product is not present in the local catalog."
            );
        }

        private void GrantProductEntitlement(
            string productId,
            string missingProductReason)
        {
            CommerceProductDefinition product =
                catalog ? catalog.Find(productId) : null;

            if (product == null)
            {
                PurchaseFailed?.Invoke(
                    productId,
                    missingProductReason
                );
                return;
            }

            GrantEntitlement(
                product.EntitlementId,
                product.Type == CommerceProductType.Cosmetic
            );
        }

        private void OnProductEntitlementRevoked(
            string productId)
        {
            CommerceProductDefinition product =
                catalog ? catalog.Find(productId) : null;

            if (
                product == null ||
                !saveService ||
                saveService.Current == null
            )
            {
                return;
            }

            bool changed =
                saveService.Current.ownedEntitlements.Remove(
                    product.EntitlementId
                );

            if (
                product.Type == CommerceProductType.Cosmetic
            )
            {
                changed |=
                    saveService.Current.ownedCosmetics.Remove(
                        product.EntitlementId
                    );
            }

            if (!changed)
                return;

            saveService.Save();
            EntitlementRevoked?.Invoke(
                product.EntitlementId
            );
        }

        private void OnRestoreCompleted()
        {
            reconciled = true;
            RestoreCompleted?.Invoke();
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

            bool changed = false;

            if (
                !saveService.Current.ownedEntitlements.Contains(
                    entitlementId
                )
            )
            {
                saveService.Current.ownedEntitlements.Add(
                    entitlementId
                );
                changed = true;
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
                changed = true;
            }

            if (!changed)
                return;

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
            if (reconciliationProvider != null)
            {
                reconciliationProvider.ProductEntitlementConfirmed -=
                    OnProductEntitlementConfirmed;

                reconciliationProvider.ProductEntitlementRevoked -=
                    OnProductEntitlementRevoked;

                reconciliationProvider = null;
            }

            if (provider == null)
                return;

            provider.PurchaseSucceeded -= OnPurchaseSucceeded;
            provider.PurchaseFailed -= OnPurchaseFailed;
            provider.RestoreCompleted -= OnRestoreCompleted;
            provider = null;
            reconciled = false;
        }
    }
}
