using System;
using System.Collections.Generic;
using NovaStriker.Core;
using NovaStriker.Player;
using NovaStriker.Session;
using UnityEngine;

namespace NovaStriker.Combat
{
    public enum StyleRank
    {
        D = 0,
        C = 1,
        B = 2,
        A = 3,
        S = 4,
        SS = 5
    }

    /// <summary>
    /// Per-player combat-style authority. Rewards damage, variety, counters,
    /// defeats, and team play while decaying during inactivity. Presentation
    /// can render the rank, but this component owns the score/math.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerStyleMeter : MonoBehaviour
    {
        [SerializeField] private StrikerPlayerIdentity identity;
        [SerializeField] private StrikeTeamSession session;

        [Header("Score")]
        [SerializeField] private float maxStyle = 1200f;
        [SerializeField] private float decayGraceSeconds = 2.2f;
        [SerializeField] private float decayPerSecond = 70f;
        [SerializeField] private float damageTakenPenalty = 85f;

        [Header("Variety")]
        [SerializeField] private float varietyMemorySeconds = 4f;
        [SerializeField] private float freshActionMultiplier = 1.30f;
        [SerializeField] private float repeatActionMultiplier = 0.72f;

        private readonly Dictionary<string, float> recentActions = new();
        private readonly List<string> expiredActions = new(16);

        private float style;
        private float graceTimer;

        public float Style => style;
        public float MaxStyle => maxStyle;
        public float Normalized => maxStyle > 0f ? style / maxStyle : 0f;
        public StyleRank Rank { get; private set; } = StyleRank.D;
        public float MasteryMultiplier => 1f + (int)Rank * 0.12f;

        public event Action<StyleRank, StyleRank> RankChanged;

        private void Reset()
        {
            identity = GetComponent<StrikerPlayerIdentity>();
        }

        private void Awake()
        {
            if (!identity)
                identity = GetComponent<StrikerPlayerIdentity>();

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
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            if (graceTimer > 0f)
            {
                graceTimer = Mathf.Max(0f, graceTimer - dt);
            }
            else if (style > 0f)
            {
                SetStyle(style - decayPerSecond * dt);
            }

            if (recentActions.Count == 0)
                return;

            expiredActions.Clear();

            foreach (KeyValuePair<string, float> pair in recentActions)
            {
                if (Time.unscaledTime - pair.Value <= varietyMemorySeconds)
                    continue;

                expiredActions.Add(pair.Key);
            }

            for (int i = 0; i < expiredActions.Count; i++)
                recentActions.Remove(expiredActions[i]);

            expiredActions.Clear();
        }

        private void OnGameplayCue(GameplayCue cue)
        {
            if (!identity || cue.ActorId != identity.ActorId)
                return;

            switch (cue.Type)
            {
                case GameplayCueType.DamageDealt:
                    AddStyle(
                        Mathf.Clamp(cue.Value * 0.85f, 2f, 70f),
                        ActionKey(cue, "damage")
                    );
                    break;

                case GameplayCueType.DefeatDealt:
                    AddStyle(48f, ActionKey(cue, "defeat"));
                    break;

                case GameplayCueType.MeleeHit:
                    AddStyle(16f, "melee-" + cue.Tier);
                    break;

                case GameplayCueType.CounterDeflect:
                    AddStyle(28f, "counter-deflect");
                    break;

                case GameplayCueType.PerfectParry:
                    AddStyle(60f, "perfect-deflect");
                    break;

                case GameplayCueType.CounterDodge:
                    AddStyle(34f, "counter-dodge");
                    break;

                case GameplayCueType.CounterThrow:
                    AddStyle(30f, "counter-throw");
                    break;

                case GameplayCueType.CounterGrapple:
                    AddStyle(22f, ActionKey(cue, "grapple"));
                    break;

                case GameplayCueType.SuitAbilityResolved:
                    AddStyle(32f, ActionKey(cue, "suit"));
                    break;

                case GameplayCueType.GuardianAbilityResolved:
                    AddStyle(38f, ActionKey(cue, "guardian"));
                    break;

                case GameplayCueType.DamageTaken:
                    SetStyle(style - damageTakenPenalty);
                    graceTimer = 0f;
                    break;
            }
        }

        public void AddStyle(float amount, string actionId)
        {
            if (amount <= 0f)
                return;

            float multiplier = 1f;

            if (!string.IsNullOrEmpty(actionId))
            {
                bool repeated =
                    recentActions.TryGetValue(
                        actionId,
                        out float lastTime
                    ) &&
                    Time.unscaledTime - lastTime <= varietyMemorySeconds;

                multiplier =
                    repeated
                        ? repeatActionMultiplier
                        : freshActionMultiplier;

                recentActions[actionId] = Time.unscaledTime;
            }

            SetStyle(style + amount * multiplier);
            graceTimer = decayGraceSeconds;
        }

        public void ResetStyle()
        {
            recentActions.Clear();
            graceTimer = 0f;
            SetStyle(0f);
        }

        private void SetStyle(float value)
        {
            float next =
                Mathf.Clamp(
                    value,
                    0f,
                    Mathf.Max(1f, maxStyle)
                );

            StyleRank previous = Rank;
            style = next;
            Rank = RankFor(style);

            if (previous == Rank)
                return;

            RankChanged?.Invoke(previous, Rank);

            GameplayEventHub.Raise(
                new GameplayCue(
                    GameplayCueType.StyleRankChanged,
                    identity ? identity.ActorId : -1,
                    transform.position,
                    Vector2.up,
                    (int)Rank,
                    style,
                    Rank.ToString()
                )
            );

            if (
                session &&
                (int)Rank > (int)previous
            )
            {
                session.AddSynergy(
                    1.5f * ((int)Rank - (int)previous)
                );
            }
        }

        private static StyleRank RankFor(float value)
        {
            if (value >= 950f)
                return StyleRank.SS;
            if (value >= 720f)
                return StyleRank.S;
            if (value >= 500f)
                return StyleRank.A;
            if (value >= 300f)
                return StyleRank.B;
            if (value >= 140f)
                return StyleRank.C;

            return StyleRank.D;
        }

        private static string ActionKey(
            GameplayCue cue,
            string fallback)
        {
            return string.IsNullOrEmpty(cue.Id)
                ? fallback
                : fallback + ":" + cue.Id;
        }
    }
}
