using System;
using System.Collections.Generic;
using NovaStriker.Core;
using NovaStriker.Player;
using UnityEngine;

namespace NovaStriker.Session
{
    public enum TeamSyncTier
    {
        None = 0,
        Pair = 2,
        Formation = 3,
        FullStrike = 4
    }

    /// <summary>
    /// Authoritative local Strike Team runtime for one-to-four players.
    /// The session owns player-slot registration, shared Synergy, team Sync
    /// requests, and multiplayer target discovery. It intentionally has no
    /// dependency on a platform input API.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    [DisallowMultipleComponent]
    public sealed class StrikeTeamSession : MonoBehaviour
    {
        public const int MaxPlayers = 4;

        [Header("Local Co-op Physics")]
        [SerializeField] private bool ignorePlayerPlayerCollision = true;

        [Header("Synergy")]
        [SerializeField] private float maxSynergy = 100f;
        [SerializeField] private float pairSyncCost = 40f;
        [SerializeField] private float formationSyncCost = 70f;
        [SerializeField] private float fullStrikeSyncCost = 100f;
        [SerializeField] private float syncRequestWindow = 0.35f;

        private readonly StrikerPlayerIdentity[] players =
            new StrikerPlayerIdentity[MaxPlayers];

        private readonly bool[] syncRequested =
            new bool[MaxPlayers];

        private float synergy;
        private float syncWindowRemaining;

        public static StrikeTeamSession Active { get; private set; }

        public event Action<float, float> SynergyChanged;
        public event Action<TeamSyncTier, int> TeamSyncActivated;

        public float Synergy => synergy;
        public float MaxSynergy => maxSynergy;
        public float SynergyNormalized =>
            maxSynergy > 0f ? synergy / maxSynergy : 0f;

        public int ActivePlayerCount
        {
            get
            {
                int count = 0;

                for (int i = 0; i < players.Length; i++)
                {
                    if (IsCombatReady(players[i]))
                        count++;
                }

                return count;
            }
        }

        private void Awake()
        {
            if (Active && Active != this)
            {
                Debug.LogWarning(
                    "Multiple StrikeTeamSession instances found. " +
                    "The newest session is becoming active.",
                    this
                );
            }

            Active = this;
            synergy = 0f;

            ConfigureTeamPhysics();
        }

        private void ConfigureTeamPhysics()
        {
            if (!ignorePlayerPlayerCollision)
                return;

            int playerLayer =
                LayerMask.NameToLayer("Player");

            if (playerLayer >= 0)
            {
                Physics2D.IgnoreLayerCollision(
                    playerLayer,
                    playerLayer,
                    true
                );
            }
        }

        private void OnEnable()
        {
            GameplayEventHub.CueRaised += OnGameplayCue;
        }

        private void OnDisable()
        {
            GameplayEventHub.CueRaised -= OnGameplayCue;

            if (Active == this)
                Active = null;
        }

        private void Update()
        {
            if (syncWindowRemaining <= 0f)
                return;

            syncWindowRemaining =
                Mathf.Max(
                    0f,
                    syncWindowRemaining - Time.unscaledDeltaTime
                );

            if (syncWindowRemaining <= 0f)
                ResolveSyncRequests();
        }

        public bool Register(StrikerPlayerIdentity player)
        {
            if (!player)
                return false;

            int slot = player.PlayerSlot;

            if (slot < 0 || slot >= MaxPlayers)
                return false;

            StrikerPlayerIdentity existing =
                players[slot];

            if (existing && existing != player)
            {
                Debug.LogWarning(
                    $"Strike Team slot {slot} was already occupied by " +
                    $"{existing.name}; replacing it with {player.name}.",
                    this
                );
            }

            players[slot] = player;
            return true;
        }

        public void Unregister(StrikerPlayerIdentity player)
        {
            if (!player)
                return;

            int slot = player.PlayerSlot;

            if (
                slot >= 0 &&
                slot < MaxPlayers &&
                players[slot] == player
            )
            {
                players[slot] = null;
                syncRequested[slot] = false;
            }
        }

        public StrikerPlayerIdentity GetPlayer(int slot)
        {
            if (slot < 0 || slot >= MaxPlayers)
                return null;

            return players[slot];
        }

        public bool TryGetNearestCombatReadyPlayer(
            Vector2 worldPosition,
            out StrikerPlayerIdentity player)
        {
            player = null;
            float bestSqr = float.PositiveInfinity;

            for (int i = 0; i < players.Length; i++)
            {
                StrikerPlayerIdentity candidate =
                    players[i];

                if (!IsCombatReady(candidate))
                    continue;

                float sqr =
                    (
                        (Vector2)candidate.transform.position -
                        worldPosition
                    ).sqrMagnitude;

                if (sqr >= bestSqr)
                    continue;

                bestSqr = sqr;
                player = candidate;
            }

            return player;
        }

        public void AddSynergy(float amount)
        {
            if (Mathf.Approximately(amount, 0f))
                return;

            float before = synergy;

            synergy =
                Mathf.Clamp(
                    synergy + amount,
                    0f,
                    Mathf.Max(0f, maxSynergy)
                );

            if (!Mathf.Approximately(before, synergy))
                SynergyChanged?.Invoke(synergy, maxSynergy);
        }

        public void SetSynergy(float value)
        {
            float before = synergy;

            synergy =
                Mathf.Clamp(
                    value,
                    0f,
                    Mathf.Max(0f, maxSynergy)
                );

            if (!Mathf.Approximately(before, synergy))
                SynergyChanged?.Invoke(synergy, maxSynergy);
        }

        public void RequestTeamSync(int playerSlot)
        {
            if (
                playerSlot < 0 ||
                playerSlot >= MaxPlayers ||
                !IsCombatReady(players[playerSlot])
            )
            {
                return;
            }

            if (syncWindowRemaining <= 0f)
            {
                Array.Clear(
                    syncRequested,
                    0,
                    syncRequested.Length
                );

                syncWindowRemaining =
                    Mathf.Max(0.05f, syncRequestWindow);
            }

            syncRequested[playerSlot] = true;

            int requested = CountRequestedSyncPlayers();
            int active = ActivePlayerCount;

            if (
                active >= 2 &&
                requested >= active
            )
            {
                ResolveSyncRequests();
            }
        }

        private void ResolveSyncRequests()
        {
            int participants =
                CountRequestedSyncPlayers();

            Array.Clear(
                syncRequested,
                0,
                syncRequested.Length
            );

            syncWindowRemaining = 0f;

            if (participants < 2)
                return;

            TeamSyncTier tier =
                participants >= 4
                    ? TeamSyncTier.FullStrike
                    : participants == 3
                        ? TeamSyncTier.Formation
                        : TeamSyncTier.Pair;

            float cost =
                tier switch
                {
                    TeamSyncTier.FullStrike =>
                        fullStrikeSyncCost,
                    TeamSyncTier.Formation =>
                        formationSyncCost,
                    _ =>
                        pairSyncCost
                };

            if (synergy + 0.001f < cost)
                return;

            AddSynergy(-cost);

            GameplayEventHub.Raise(
                new GameplayCue(
                    GameplayCueType.TeamSyncStarted,
                    -1,
                    transform.position,
                    Vector2.zero,
                    participants,
                    cost,
                    tier switch
                    {
                        TeamSyncTier.FullStrike =>
                            "team-sync-full-strike",
                        TeamSyncTier.Formation =>
                            "team-sync-formation",
                        _ =>
                            "team-sync-pair"
                    }
                )
            );

            TeamSyncActivated?.Invoke(
                tier,
                participants
            );
        }

        private int CountRequestedSyncPlayers()
        {
            int count = 0;

            for (int i = 0; i < syncRequested.Length; i++)
            {
                if (
                    syncRequested[i] &&
                    IsCombatReady(players[i])
                )
                {
                    count++;
                }
            }

            return count;
        }

        private void OnGameplayCue(GameplayCue cue)
        {
            StrikerPlayerIdentity actor =
                FindPlayerByActorId(cue.ActorId);

            if (!actor)
                return;

            switch (cue.Type)
            {
                case GameplayCueType.ProjectileCancelled:
                    AddSynergy(2f);
                    break;

                case GameplayCueType.MeleeHit:
                    AddSynergy(1.25f);
                    break;

                case GameplayCueType.CounterDeflect:
                    AddSynergy(3f);
                    break;

                case GameplayCueType.PerfectParry:
                    AddSynergy(7f);
                    break;

                case GameplayCueType.CounterDodge:
                    AddSynergy(3.5f);
                    break;

                case GameplayCueType.CounterThrow:
                    AddSynergy(2.5f);
                    break;

                case GameplayCueType.CounterGrapple:
                    AddSynergy(1.5f);
                    break;

                case GameplayCueType.DamageTaken:
                    AddSynergy(-4f);
                    break;

                case GameplayCueType.Defeated:
                    AddSynergy(-12f);
                    break;
            }
        }

        private StrikerPlayerIdentity FindPlayerByActorId(
            int actorId)
        {
            for (int i = 0; i < players.Length; i++)
            {
                StrikerPlayerIdentity candidate =
                    players[i];

                if (
                    candidate &&
                    candidate.ActorId == actorId
                )
                {
                    return candidate;
                }
            }

            return null;
        }

        private static bool IsCombatReady(
            StrikerPlayerIdentity player)
        {
            return
                player &&
                player.isActiveAndEnabled &&
                player.IsCombatReady;
        }
    }
}
