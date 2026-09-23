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
    /// Editor/offline commerce provider for end-to-end entitlement testing.
    /// Native console/mobile/Steam providers implement the same interface.
    /// </summary>
    public sealed class MockCommerceProvider : ICommerceProvider
    {
        private CommerceCatalog catalog;

        public bool Ready { get; private set; }

        public event Action<string> PurchaseSucceeded;
        public event Action<string, string> PurchaseFailed;
        public event Action RestoreCompleted;

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

            PurchaseSucceeded?.Invoke(productId);
        }

        public void RestorePurchases()
        {
            RestoreCompleted?.Invoke();
        }
    }
}
