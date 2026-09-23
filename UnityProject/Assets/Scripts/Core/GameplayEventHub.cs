using System;
using UnityEngine;

namespace NovaStriker.Core
{
    public enum GameplayCueType
    {
        Jump,
        WallJump,
        CrouchStarted,
        CrouchEnded,
        DropThrough,
        DashStarted,
        DashEnded,
        SlideStarted,
        SlideEnded,
        DashHit,
        SlideHit,
        FireChargeStarted,
        FireChargeReleased,
        ProjectileFired,
        ProjectileCancelled,
        MeleeStarted,
        MeleeHit,
        CounterStarted,
        CounterDeflect,
        CounterGrappleLock,
        CounterGrapple,
        CounterGrappleTraversal,
        CounterDodge,
        CounterThrow,
        SuitAbilityStarted,
        SuitAbilityResolved,
        SuitUltimateStarted,
        SuitUltimateEnded,
        GuardianAbilityStarted,
        GuardianAbilityResolved,
        TeamSyncStarted,
        TeamSyncResolved,
        StyleRankChanged,
        EncounterStarted,
        EncounterWaveStarted,
        EncounterCompleted,
        PickupCollected,
        SecretOpened,
        SecretCleared,
        SetpieceStarted,
        SetpieceEnded,
        BossAttackStarted,
        BossPhaseChanged,
        GuardianWeakPointOpened,
        GuardianWeakPointClosed,
        CampaignCompleted,
        ParryStarted,
        ParrySuccess,
        PerfectParry,
        DamageTaken,
        DamageDealt,
        DefeatDealt,
        ShieldBroken,
        ArmorBroken,
        GuardBroken,
        StatusApplied,
        Defeated,
        Revived
    }

    /// <summary>
    /// Presentation-agnostic signal emitted by gameplay systems.
    /// Animation, VFX, audio, camera, and haptics should observe these
    /// signals rather than own combat timing.
    /// </summary>
    public struct GameplayCue
    {
        public GameplayCueType Type;
        public int ActorId;
        public Vector2 Position;
        public Vector2 Direction;
        public int Tier;
        public float Value;
        public string Id;

        public GameplayCue(
            GameplayCueType type,
            int actorId,
            Vector2 position,
            Vector2 direction,
            int tier = 0,
            float value = 0f,
            string id = null)
        {
            Type = type;
            ActorId = actorId;
            Position = position;
            Direction = direction;
            Tier = tier;
            Value = value;
            Id = id;
        }
    }

    public static class GameplayEventHub
    {
        public static event Action<GameplayCue> CueRaised;

        public static void Raise(GameplayCue cue)
        {
            CueRaised?.Invoke(cue);
        }
    }
}
