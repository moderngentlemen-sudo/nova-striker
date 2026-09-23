using System;
using System.Collections;
using NovaStriker.Combat;
using NovaStriker.Core;
using NovaStriker.Player;
using NovaStriker.Progression;
using NovaStriker.Session;
using UnityEngine;

namespace NovaStriker.Campaign
{
    /// <summary>
    /// Party-wipe and checkpoint retry authority for local 1-4 player sessions.
    /// Scene/front-end presentation can observe the events, but retry state,
    /// encounter rewind, player revival, and checkpoint placement live here.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CampaignRetryController2D : MonoBehaviour
    {
        [SerializeField] private StrikeTeamSession session;
        [SerializeField] private CampaignProgressionController progression;

        [Header("Retry")]
        [SerializeField] private bool automaticRetry = true;
        [SerializeField, Min(0f)] private float wipeConfirmDelay = 0.35f;
        [SerializeField, Min(0f)] private float retryDelay = 0.85f;
        [SerializeField, Min(0f)] private float playerRespawnSpacing = 0.75f;
        [SerializeField, Min(0f)] private float respawnInvulnerability = 1.75f;

        private Coroutine wipeRoutine;
        private StrikeTeamSession boundSession;
        private bool awaitingRetry;

        public bool AwaitingRetry => awaitingRetry;

        public event Action PartyWiped;
        public event Action RetryStarted;
        public event Action RetryCompleted;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            GameplayEventHub.CueRaised += OnGameplayCue;
            ResolveReferences();
            BindSession();
        }

        private void OnDisable()
        {
            GameplayEventHub.CueRaised -= OnGameplayCue;
            UnbindSession();

            if (wipeRoutine != null)
            {
                StopCoroutine(wipeRoutine);
                wipeRoutine = null;
            }
        }

        private void Update()
        {
            if (!session)
            {
                ResolveReferences();
                BindSession();
            }
        }

        public void RetryNow()
        {
            if (!session)
                ResolveReferences();

            if (!session)
                return;

            if (wipeRoutine != null)
            {
                StopCoroutine(wipeRoutine);
                wipeRoutine = null;
            }

            awaitingRetry = false;

            GameplayEventHub.Raise(
                new GameplayCue(
                    GameplayCueType.RetryStarted,
                    -1,
                    transform.position,
                    Vector2.zero,
                    progression ? progression.CheckpointIndex : 0,
                    0f,
                    "campaign-retry"
                )
            );

            RetryStarted?.Invoke();

            ResetEncounters();
            Vector3 basePosition = ResolveCheckpointPosition();
            RespawnParticipatingPlayers(basePosition);

            session.SetSynergy(0f);
            progression?.RestartActFromCheckpoint();

            GameplayEventHub.Raise(
                new GameplayCue(
                    GameplayCueType.RetryCompleted,
                    -1,
                    basePosition,
                    Vector2.up,
                    progression ? progression.CheckpointIndex : 0,
                    session.ParticipatingPlayerCount,
                    "campaign-retry"
                )
            );

            RetryCompleted?.Invoke();
        }

        private void OnGameplayCue(GameplayCue cue)
        {
            if (
                cue.Type != GameplayCueType.Defeated &&
                cue.Type != GameplayCueType.Revived
            )
            {
                return;
            }

            if (!session)
                ResolveReferences();

            if (
                !session ||
                !session.GetPlayerByActorId(cue.ActorId)
            )
            {
                return;
            }

            EvaluatePartyState();
        }

        private void OnParticipationChanged(
            int slot,
            bool participating)
        {
            EvaluatePartyState();
        }

        private void EvaluatePartyState()
        {
            if (!session)
                return;

            if (!session.IsPartyWiped)
            {
                awaitingRetry = false;

                if (wipeRoutine != null)
                {
                    StopCoroutine(wipeRoutine);
                    wipeRoutine = null;
                }

                return;
            }

            if (wipeRoutine == null && !awaitingRetry)
                wipeRoutine = StartCoroutine(ConfirmPartyWipe());
        }

        private IEnumerator ConfirmPartyWipe()
        {
            if (wipeConfirmDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    wipeConfirmDelay
                );
            }

            wipeRoutine = null;

            if (!session || !session.IsPartyWiped)
                yield break;

            awaitingRetry = true;

            GameplayEventHub.Raise(
                new GameplayCue(
                    GameplayCueType.PartyWiped,
                    -1,
                    transform.position,
                    Vector2.down,
                    progression ? progression.CheckpointIndex : 0,
                    session.ParticipatingPlayerCount,
                    "party-wipe"
                )
            );

            PartyWiped?.Invoke();

            if (!automaticRetry)
                yield break;

            if (retryDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    retryDelay
                );
            }

            if (session && session.IsPartyWiped)
                RetryNow();
        }

        private void ResetEncounters()
        {
            EncounterController2D[] encounters =
                FindObjectsByType<EncounterController2D>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None
                );

            for (int i = 0; i < encounters.Length; i++)
                encounters[i]?.ResetForRetry();
        }

        private Vector3 ResolveCheckpointPosition()
        {
            int desired =
                progression
                    ? progression.CheckpointIndex
                    : 0;

            CampaignCheckpoint2D[] checkpoints =
                FindObjectsByType<CampaignCheckpoint2D>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None
                );

            CampaignCheckpoint2D fallback = null;

            for (int i = 0; i < checkpoints.Length; i++)
            {
                CampaignCheckpoint2D checkpoint =
                    checkpoints[i];

                if (!checkpoint)
                    continue;

                if (
                    fallback == null ||
                    checkpoint.CheckpointIndex <
                    fallback.CheckpointIndex
                )
                {
                    fallback = checkpoint;
                }

                if (checkpoint.CheckpointIndex == desired)
                    return checkpoint.RespawnPosition;
            }

            return fallback
                ? fallback.RespawnPosition
                : transform.position;
        }

        private void RespawnParticipatingPlayers(
            Vector3 basePosition)
        {
            int count =
                Mathf.Max(
                    1,
                    session.ParticipatingPlayerCount
                );

            int ordinal = 0;

            for (int slot = 0; slot < StrikeTeamSession.MaxPlayers; slot++)
            {
                StrikerPlayerIdentity player =
                    session.GetPlayer(slot);

                if (
                    !player ||
                    !player.IsParticipating
                )
                {
                    continue;
                }

                NovaPlayerGameplay input =
                    player.GetComponent<NovaPlayerGameplay>();

                Damageable2D health =
                    player.GetComponent<Damageable2D>();

                Rigidbody2D body =
                    player.GetComponent<Rigidbody2D>();

                PlayerStyleMeter style =
                    player.GetComponent<PlayerStyleMeter>();

                input?.ClearInput();
                style?.ResetStyle();

                if (health)
                {
                    if (health.IsDefeated)
                    {
                        health.Revive(
                            1f,
                            respawnInvulnerability
                        );
                    }
                    else
                    {
                        health.RestoreFullHealth();
                        health.GrantInvulnerability(
                            respawnInvulnerability
                        );
                    }
                }

                float centered =
                    ordinal -
                    (count - 1) * 0.5f;

                Vector2 position =
                    (Vector2)basePosition +
                    Vector2.right *
                    centered *
                    playerRespawnSpacing;

                if (body)
                {
                    body.position = position;
                    body.linearVelocity = Vector2.zero;
                    body.angularVelocity = 0f;
                }
                else
                {
                    player.transform.position = position;
                }

                ordinal++;
            }
        }

        private void ResolveReferences()
        {
            if (!session)
                session = StrikeTeamSession.Active;

            if (!progression)
            {
                progression =
                    FindFirstObjectByType<CampaignProgressionController>();
            }
        }

        private void BindSession()
        {
            if (!session || boundSession == session)
                return;

            UnbindSession();
            boundSession = session;
            boundSession.PlayerParticipationChanged +=
                OnParticipationChanged;
        }

        private void UnbindSession()
        {
            if (!boundSession)
                return;

            boundSession.PlayerParticipationChanged -=
                OnParticipationChanged;
            boundSession = null;
        }
    }
}
