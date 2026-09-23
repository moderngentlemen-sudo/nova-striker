using NovaStriker.Player;
using NovaStriker.Progression;
using UnityEngine;

namespace NovaStriker.Campaign
{
    /// <summary>
    /// Lightweight campaign checkpoint marker. The first combat-ready local
    /// player that enters advances persistent checkpoint state.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CampaignCheckpoint2D : MonoBehaviour
    {
        [SerializeField, Min(0)] private int checkpointIndex;
        [SerializeField] private CampaignProgressionController progression;
        [SerializeField] private bool activateOnce = true;

        private bool activated;

        private void Awake()
        {
            if (!progression)
                progression =
                    FindFirstObjectByType<CampaignProgressionController>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (activateOnce && activated)
                return;

            StrikerPlayerIdentity player =
                other.GetComponentInParent<StrikerPlayerIdentity>();

            if (!player || !player.IsCombatReady)
                return;

            if (!progression)
                progression =
                    FindFirstObjectByType<CampaignProgressionController>();

            if (!progression)
                return;

            progression.SetCheckpoint(checkpointIndex);
            activated = true;
        }
    }
}
