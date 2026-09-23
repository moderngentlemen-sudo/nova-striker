using System;
using System.Collections.Generic;
using NovaStriker.Combat;
using NovaStriker.Session;
using UnityEngine;

namespace NovaStriker.Campaign
{
    /// <summary>
    /// Reusable art-independent objective authority for campaign greyboxes and
    /// production scenes. Scene triggers report progress; visual presentation
    /// consumes state without owning completion timing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ActObjectiveController2D : MonoBehaviour
    {
        [SerializeField] private EncounterObjectiveKind objectiveKind;
        [SerializeField] private EncounterController2D[] encounters;
        [SerializeField] private Damageable2D protectedAsset;

        [Header("Timed Objectives")]
        [SerializeField, Min(0.1f)] private float holdDuration = 5f;
        [SerializeField, Min(0.1f)] private float survivalDuration = 8f;
        [SerializeField] private bool requireAllParticipatingPlayersForHold;

        [Header("Node Objectives")]
        [SerializeField, Min(1)] private int requiredNodeCount = 2;

        private readonly HashSet<int> zoneActors = new();
        private readonly HashSet<string> activatedNodes =
            new(StringComparer.OrdinalIgnoreCase);

        private float elapsed;
        private bool goalReached;

        public EncounterObjectiveKind ObjectiveKind => objectiveKind;
        public bool Started { get; private set; }
        public bool Completed { get; private set; }
        public float Progress01 => CalculateProgress01();

        public event Action<EncounterObjectiveKind> ObjectiveStarted;
        public event Action<EncounterObjectiveKind> ObjectiveCompleted;
        public event Action<float> ProgressChanged;

        public void ConfigureObjective(
            EncounterObjectiveKind kind)
        {
            objectiveKind = kind;
            ResetObjective();
            BeginObjective();
        }

        public void BeginObjective()
        {
            if (Started || Completed)
                return;

            Started = true;
            ObjectiveStarted?.Invoke(objectiveKind);
            ProgressChanged?.Invoke(Progress01);
        }

        public void ResetObjective()
        {
            Started = false;
            Completed = false;
            elapsed = 0f;
            goalReached = false;
            zoneActors.Clear();
            activatedNodes.Clear();
        }

        public void SetZoneActor(
            int actorId,
            bool inside)
        {
            if (inside)
                zoneActors.Add(actorId);
            else
                zoneActors.Remove(actorId);
        }

        public void ActivateNode(string nodeId)
        {
            if (
                !Started ||
                Completed ||
                string.IsNullOrWhiteSpace(nodeId)
            )
            {
                return;
            }

            if (activatedNodes.Add(nodeId))
            {
                ProgressChanged?.Invoke(
                    Progress01
                );
            }

            if (
                IsNodeObjective() &&
                activatedNodes.Count >=
                Mathf.Max(1, requiredNodeCount)
            )
            {
                CompleteObjective();
            }
        }

        public void ReportGoalReached()
        {
            if (!Started || Completed)
                return;

            goalReached = true;
            ProgressChanged?.Invoke(1f);

            if (
                objectiveKind == EncounterObjectiveKind.Advance ||
                objectiveKind == EncounterObjectiveKind.Pursuit
            )
            {
                CompleteObjective();
            }
        }

        private void FixedUpdate()
        {
            if (!Started || Completed)
                return;

            switch (objectiveKind)
            {
                case EncounterObjectiveKind.HoldZone:
                    UpdateHoldObjective();
                    break;

                case EncounterObjectiveKind.Survival:
                    elapsed += Time.fixedDeltaTime;
                    ProgressChanged?.Invoke(Progress01);

                    if (elapsed >= survivalDuration)
                        CompleteObjective();
                    break;

                case EncounterObjectiveKind.Eliminate:
                    if (AllEncountersComplete())
                        CompleteObjective();
                    break;

                case EncounterObjectiveKind.ProtectAsset:
                    if (
                        protectedAsset &&
                        !protectedAsset.IsDefeated &&
                        AllEncountersComplete()
                    )
                    {
                        CompleteObjective();
                    }
                    break;
            }
        }

        private void UpdateHoldObjective()
        {
            int requiredOccupants = 1;

            if (requireAllParticipatingPlayersForHold)
            {
                StrikeTeamSession session =
                    StrikeTeamSession.Active;

                if (session)
                {
                    requiredOccupants =
                        Mathf.Max(
                            1,
                            session.ParticipatingPlayerCount
                        );
                }
            }

            if (zoneActors.Count >= requiredOccupants)
            {
                elapsed += Time.fixedDeltaTime;
                ProgressChanged?.Invoke(Progress01);

                if (elapsed >= holdDuration)
                    CompleteObjective();
            }
        }

        private bool AllEncountersComplete()
        {
            if (
                encounters == null ||
                encounters.Length == 0
            )
            {
                return false;
            }

            bool found = false;

            for (int i = 0; i < encounters.Length; i++)
            {
                EncounterController2D encounter =
                    encounters[i];

                if (!encounter)
                    continue;

                found = true;

                if (!encounter.Completed)
                    return false;
            }

            return found;
        }

        private bool IsNodeObjective()
        {
            return
                objectiveKind == EncounterObjectiveKind.DisableNodes ||
                objectiveKind == EncounterObjectiveKind.MultiFront;
        }

        private float CalculateProgress01()
        {
            if (Completed)
                return 1f;

            return objectiveKind switch
            {
                EncounterObjectiveKind.HoldZone =>
                    Mathf.Clamp01(
                        elapsed /
                        Mathf.Max(0.1f, holdDuration)
                    ),

                EncounterObjectiveKind.Survival =>
                    Mathf.Clamp01(
                        elapsed /
                        Mathf.Max(0.1f, survivalDuration)
                    ),

                EncounterObjectiveKind.DisableNodes or
                EncounterObjectiveKind.MultiFront =>
                    Mathf.Clamp01(
                        (float)activatedNodes.Count /
                        Mathf.Max(1, requiredNodeCount)
                    ),

                EncounterObjectiveKind.Advance or
                EncounterObjectiveKind.Pursuit =>
                    goalReached ? 1f : 0f,

                EncounterObjectiveKind.Eliminate or
                EncounterObjectiveKind.ProtectAsset =>
                    AllEncountersComplete() ? 1f : 0f,

                _ => 0f
            };
        }

        private void CompleteObjective()
        {
            if (Completed)
                return;

            Completed = true;
            Started = false;
            ProgressChanged?.Invoke(1f);
            ObjectiveCompleted?.Invoke(objectiveKind);
        }
    }
}
