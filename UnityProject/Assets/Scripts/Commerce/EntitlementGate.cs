using UnityEngine;

namespace NovaStriker.Commerce
{
    /// <summary>
    /// Enables/disables content according to a DLC/cosmetic entitlement.
    /// This is suitable for optional missions, cosmetic presentation roots,
    /// challenge packs, or expansion entry points.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EntitlementGate : MonoBehaviour
    {
        [SerializeField] private string requiredEntitlementId;
        [SerializeField] private GameObject[] gatedObjects;
        [SerializeField] private bool invert;
        [SerializeField] private bool defaultLocked = true;

        private EntitlementService service;

        public bool IsUnlocked { get; private set; }

        private void OnEnable()
        {
            ResolveService();

            if (service)
                service.EntitlementGranted += OnEntitlementGranted;

            Refresh();
        }

        private void OnDisable()
        {
            if (service)
                service.EntitlementGranted -= OnEntitlementGranted;
        }

        public void Refresh()
        {
            ResolveService();

            bool owned =
                service &&
                service.HasEntitlement(
                    requiredEntitlementId
                );

            IsUnlocked =
                string.IsNullOrEmpty(requiredEntitlementId)
                    ? !defaultLocked
                    : owned;

            bool active =
                invert
                    ? !IsUnlocked
                    : IsUnlocked;

            if (gatedObjects == null)
                return;

            for (int i = 0; i < gatedObjects.Length; i++)
            {
                if (gatedObjects[i])
                    gatedObjects[i].SetActive(active);
            }
        }

        private void OnEntitlementGranted(string entitlementId)
        {
            if (entitlementId == requiredEntitlementId)
                Refresh();
        }

        private void ResolveService()
        {
            if (!service)
                service = EntitlementService.Active;
        }
    }
}
