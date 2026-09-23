using System;
using NovaStriker.Core;
using NovaStriker.Player;
using UnityEngine;

namespace NovaStriker.Session
{
    /// <summary>
    /// Rewards rapid handoffs between different local players. Assist chains
    /// build from resolved combat actions, never from presentation timing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CoopAssistChainService : MonoBehaviour
    {
        [SerializeField] private StrikeTeamSession session;
        [SerializeField, Min(0.25f)] private float chainWindow = 1.6f;
        [SerializeField, Range(2, 8)] private int maxChain = 6;
        [SerializeField, Min(0f)] private float synergyPerStep = 1.5f;

        private int lastActorId = -1;
        private int chainStep;
        private int participantMask;
        private float lastActionTime = float.NegativeInfinity;

        public int ChainStep => chainStep;
        public int ParticipantMask => participantMask;

        public event Action<int, int> ChainAdvanced;
        public event Action ChainExpired;

        private void Awake()
        {
            if (!session)
                session = GetComponent<StrikeTeamSession>();

            if (!session)
                session = StrikeTeamSession.Active;
        }

        private void OnEnable()
        {
            GameplayEventHub.CueRaised += OnGameplayCue;
        }

        private void OnDisable()
        {
            GameplayEventHub.CueRaised -= OnGameplayCue;
            ResetChain(false);
        }

        private void Update()
        {
            if (
                chainStep > 0 &&
                Time.unscaledTime - lastActionTime >
                chainWindow
            )
            {
                ResetChain(true);
            }
        }

        private void OnGameplayCue(GameplayCue cue)
        {
            if (!IsChainAction(cue.Type))
                return;

            if (!session)
                session = StrikeTeamSession.Active;

            StrikerPlayerIdentity player =
                session
                    ? session.GetPlayerByActorId(cue.ActorId)
                    : null;

            if (
                !player ||
                !player.IsCombatReady
            )
            {
                return;
            }

            float now = Time.unscaledTime;

            if (
                chainStep <= 0 ||
                now - lastActionTime > chainWindow
            )
            {
                ResetChain(false);
                chainStep = 1;
                participantMask =
                    1 << player.PlayerSlot;
                lastActorId = cue.ActorId;
                lastActionTime = now;
                return;
            }

            lastActionTime = now;

            if (cue.ActorId == lastActorId)
                return;

            lastActorId = cue.ActorId;
            participantMask |=
                1 << player.PlayerSlot;

            chainStep =
                Mathf.Min(
                    maxChain,
                    chainStep + 1
                );

            float synergyReward =
                synergyPerStep *
                Mathf.Max(1, chainStep - 1);

            session.AddSynergy(synergyReward);

            if (chainStep == 3)
            {
                RestoreParticipantResources(
                    suitEnergy: 5f,
                    ultimateCharge: 0f
                );
            }
            else if (chainStep >= 5)
            {
                RestoreParticipantResources(
                    suitEnergy: 5f,
                    ultimateCharge: 3f
                );
            }

            GameplayEventHub.Raise(
                new GameplayCue(
                    GameplayCueType.AssistChainAdvanced,
                    cue.ActorId,
                    cue.Position,
                    cue.Direction,
                    chainStep,
                    synergyReward,
                    "assist-chain"
                )
            );

            ChainAdvanced?.Invoke(
                chainStep,
                participantMask
            );
        }

        private void RestoreParticipantResources(
            float suitEnergy,
            float ultimateCharge)
        {
            if (!session)
                return;

            for (
                int slot = 0;
                slot < StrikeTeamSession.MaxPlayers;
                slot++
            )
            {
                if ((participantMask & (1 << slot)) == 0)
                    continue;

                StrikerPlayerIdentity player =
                    session.GetPlayer(slot);

                if (!player || !player.IsCombatReady)
                    continue;

                StrikeSuitAbilityController suit =
                    player.GetComponent<StrikeSuitAbilityController>();

                if (!suit)
                    continue;

                if (suitEnergy > 0f)
                    suit.RestoreSuitEnergy(suitEnergy);

                if (ultimateCharge > 0f)
                    suit.AddUltimateCharge(ultimateCharge);
            }
        }

        private void ResetChain(bool raiseExpired)
        {
            bool hadChain = chainStep > 0;

            lastActorId = -1;
            chainStep = 0;
            participantMask = 0;
            lastActionTime = float.NegativeInfinity;

            if (raiseExpired && hadChain)
                ChainExpired?.Invoke();
        }

        private static bool IsChainAction(
            GameplayCueType type)
        {
            return type switch
            {
                GameplayCueType.ProjectileCancelled => true,
                GameplayCueType.MeleeHit => true,
                GameplayCueType.CounterDeflect => true,
                GameplayCueType.PerfectParry => true,
                GameplayCueType.CounterDodge => true,
                GameplayCueType.CounterThrow => true,
                GameplayCueType.CounterGrapple => true,
                GameplayCueType.SuitAbilityResolved => true,
                GameplayCueType.GuardianAbilityResolved => true,
                _ => false
            };
        }
    }
}
