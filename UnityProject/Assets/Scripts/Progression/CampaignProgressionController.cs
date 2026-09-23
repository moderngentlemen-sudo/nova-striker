using System;
using NovaStriker.Core;
using UnityEngine;

namespace NovaStriker.Progression
{
    /// <summary>
    /// Campaign progression authority for six sectors × three acts.
    /// Scene loading/presentation may observe this state but does not own it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CampaignProgressionController : MonoBehaviour
    {
        [SerializeField] private SaveGameService saveService;

        public event Action<int, int> ActChanged;
        public event Action<int> CheckpointChanged;
        public event Action CampaignCompleted;

        public int SectorIndex =>
            Save != null ? Save.sectorIndex : 0;

        public int ActIndex =>
            Save != null ? Save.actIndex : 0;

        public int CheckpointIndex =>
            Save != null ? Save.checkpointIndex : 0;

        private NovaSaveData Save =>
            saveService
                ? saveService.Current
                : null;

        private void Awake()
        {
            if (!saveService)
                saveService = SaveGameService.Active;
        }

        public void SetCheckpoint(int checkpoint)
        {
            if (Save == null)
                return;

            Save.checkpointIndex =
                Mathf.Max(0, checkpoint);

            saveService.Save();

            CheckpointChanged?.Invoke(
                Save.checkpointIndex
            );
        }

        public void CompleteAct()
        {
            if (Save == null)
                return;

            Save.checkpointIndex = 0;

            if (Save.actIndex < 2)
            {
                Save.actIndex++;
                saveService.Save();
                ActChanged?.Invoke(
                    Save.sectorIndex,
                    Save.actIndex
                );
                return;
            }

            if (Save.sectorIndex < 5)
            {
                Save.sectorIndex++;
                Save.actIndex = 0;
                saveService.Save();
                ActChanged?.Invoke(
                    Save.sectorIndex,
                    Save.actIndex
                );
                return;
            }

            saveService.Save();
            CampaignCompleted?.Invoke();

            GameplayEventHub.Raise(
                new GameplayCue(
                    GameplayCueType.CampaignCompleted,
                    -1,
                    transform.position,
                    Vector2.zero,
                    0,
                    0f,
                    "campaign-complete"
                )
            );
        }

        public void RestartActFromCheckpoint()
        {
            if (Save == null)
                return;

            ActChanged?.Invoke(
                Save.sectorIndex,
                Save.actIndex
            );

            CheckpointChanged?.Invoke(
                Save.checkpointIndex
            );
        }
    }
}
