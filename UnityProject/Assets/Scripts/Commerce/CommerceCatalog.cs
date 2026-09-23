using System;
using System.Collections.Generic;
using UnityEngine;

namespace NovaStriker.Commerce
{
    public enum CommerceProductType
    {
        Cosmetic = 0,
        Dlc = 1,
        Consumable = 2
    }

    [Serializable]
    public sealed class CommerceProductDefinition
    {
        public string ProductId;
        public string EntitlementId;
        public string DisplayName;
        public CommerceProductType Type;
        public bool Restorable = true;
        public bool GameplayStatPurchase;
    }

    [CreateAssetMenu(
        menuName = "Nova Striker/Commerce Catalog",
        fileName = "CommerceCatalog")]
    public sealed class CommerceCatalog : ScriptableObject
    {
        public List<CommerceProductDefinition> Products = new();

        public CommerceProductDefinition Find(
            string productId)
        {
            if (string.IsNullOrEmpty(productId))
                return null;

            for (int i = 0; i < Products.Count; i++)
            {
                CommerceProductDefinition product =
                    Products[i];

                if (
                    product != null &&
                    product.ProductId == productId
                )
                {
                    return product;
                }
            }

            return null;
        }
    }

    public interface ICommerceProvider
    {
        bool Ready { get; }
        event Action<string> PurchaseSucceeded;
        event Action<string, string> PurchaseFailed;
        event Action RestoreCompleted;

        void Initialize(CommerceCatalog catalog);
        void Purchase(string productId);
        void RestorePurchases();
    }

    /// <summary>
    /// Optional provider extension for receipt/storefront reconciliation.
    /// Native adapters can confirm or revoke product ownership after querying
    /// the platform account without changing gameplay entitlement APIs.
    /// </summary>
    public interface ICommerceEntitlementReconciliationProvider
    {
        event Action<string> ProductEntitlementConfirmed;
        event Action<string> ProductEntitlementRevoked;
        void RefreshEntitlements();
    }

    /// <summary>
    /// Editor/offline commerce provider for end-to-end entitlement testing.
    /// Native console/mobile/Steam providers implement the same interface.
    /// </summary>
    public sealed class MockCommerceProvider :
        ICommerceProvider,
        ICommerceEntitlementReconciliationProvider
    {
        private CommerceCatalog catalog;
        private readonly HashSet<string> purchasedProducts = new();

        public bool Ready { get; private set; }

        public event Action<string> PurchaseSucceeded;
        public event Action<string, string> PurchaseFailed;
        public event Action RestoreCompleted;
        public event Action<string> ProductEntitlementConfirmed;
        public event Action<string> ProductEntitlementRevoked;

        public void Initialize(CommerceCatalog value)
        {
            catalog = value;
            Ready = catalog;
        }

        public void Purchase(string productId)
        {
            if (!Ready)
            {
                PurchaseFailed?.Invoke(
                    productId,
                    "Mock commerce provider is not initialized."
                );
                return;
            }

            CommerceProductDefinition product =
                catalog.Find(productId);

            if (product == null)
            {
                PurchaseFailed?.Invoke(
                    productId,
                    "Unknown product."
                );
                return;
            }

            purchasedProducts.Add(productId);
            ProductEntitlementConfirmed?.Invoke(productId);
            PurchaseSucceeded?.Invoke(productId);
        }

        public void RestorePurchases()
        {
            RefreshEntitlements();
        }

        public void RefreshEntitlements()
        {
            foreach (string productId in purchasedProducts)
            {
                ProductEntitlementConfirmed?.Invoke(
                    productId
                );
            }

            RestoreCompleted?.Invoke();
        }

        public void RevokeForDevelopment(
            string productId)
        {
            if (!purchasedProducts.Remove(productId))
                return;

            ProductEntitlementRevoked?.Invoke(productId);
        }
    }
}
