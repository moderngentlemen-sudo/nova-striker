using UnityEngine;

namespace NovaStriker.Commerce
{
    /// <summary>
    /// Simple content gate for DLC/expansion objects. The gate never grants
    /// gameplay power; it only exposes content after a verified entitlement.
    /// </summary>
    public sealed class DlcContentGate : MonoBehaviour
    {
        [SerializeField] private string entitlementId;
        [SerializeField] private GameObject[] gatedObjects;
        [SerializeField] private bool hideWhenNotOwned = true;

        private EntitlementService entitlements;

        private void OnEnable()
        {
            entitlements = EntitlementService.Active;

            if (entitlements)
                entitlements.EntitlementGranted += OnEntitlementGranted;

            Refresh();
        }

        private void OnDisable()
        {
            if (entitlements)
                entitlements.EntitlementGranted -= OnEntitlementGranted;
        }

        public void Refresh()
        {
            if (!entitlements)
                entitlements = EntitlementService.Active;

            bool owned =
                entitlements &&
                entitlements.HasEntitlement(
                    entitlementId
                );

            bool enabled =
                owned || !hideWhenNotOwned;

            if (gatedObjects == null)
                return;

            for (int i = 0; i < gatedObjects.Length; i++)
            {
                if (gatedObjects[i])
                    gatedObjects[i].SetActive(enabled);
            }
        }

        private void OnEntitlementGranted(string id)
        {
            if (id == entitlementId)
                Refresh();
        }
    }
}
