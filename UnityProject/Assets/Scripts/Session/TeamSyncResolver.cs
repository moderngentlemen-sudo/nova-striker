using System.Collections.Generic;
using NovaStriker.Combat;
using NovaStriker.Core;
using NovaStriker.Player;
using UnityEngine;

namespace NovaStriker.Session
{
    /// <summary>
    /// Resolves Pair, Formation, and Full-Strike Sync tiers for two-to-four
    /// simultaneous players. These greybox mechanics are intentionally
    /// presentation-independent.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TeamSyncResolver : MonoBehaviour
    {
        [SerializeField] private StrikeTeamSession session;
        [SerializeField] private LayerMask damageableMask;

        [Header("Pair Sync")]
        [SerializeField] private float pairRadius = 4f;
        [SerializeField] private float pairDamage = 28f;
        [SerializeField] private float pairBreak = 20f;

        [Header("Formation Sync")]
        [SerializeField] private float formationRadius = 6f;
        [SerializeField] private float formationDamage = 42f;
        [SerializeField] private float formationBreak = 35f;

        [Header("Full Strike")]
        [SerializeField] private float fullStrikeRadius = 9f;
        [SerializeField] private float fullStrikeDamage = 60f;
        [SerializeField] private float fullStrikeBreak = 55f;

        private readonly List<Collider2D> hits = new(64);
        private readonly HashSet<Damageable2D> uniqueTargets = new();
        private ContactFilter2D damageFilter;

        private void Awake()
        {
            if (!session)
                session = GetComponent<StrikeTeamSession>();

            damageFilter = new ContactFilter2D
            {
                useTriggers = true
            };
            damageFilter.SetLayerMask(damageableMask);
        }

        private void OnEnable()
        {
            if (!session)
                session = GetComponent<StrikeTeamSession>();

            if (session)
                session.TeamSyncActivated += OnTeamSyncActivated;
        }

        private void OnDisable()
        {
            if (session)
                session.TeamSyncActivated -= OnTeamSyncActivated;
        }

        private void OnTeamSyncActivated(
            TeamSyncTier tier,
            int participantCount)
        {
            int mask =
                session
                    ? session.LastSyncParticipantMask
                    : 0;

            if (!TryGetParticipantCenter(mask, out Vector2 center))
                return;

            float radius;
            float damage;
            float breakDamage;
            float healFraction;
            float invulnerability;

            switch (tier)
            {
                case TeamSyncTier.FullStrike:
                    radius = fullStrikeRadius;
                    damage = fullStrikeDamage;
                    breakDamage = fullStrikeBreak;
                    healFraction = 0.25f;
                    invulnerability = 1.10f;
                    break;

                case TeamSyncTier.Formation:
                    radius = formationRadius;
                    damage = formationDamage;
                    breakDamage = formationBreak;
                    healFraction = 0.15f;
                    invulnerability = 0.70f;
                    break;

                default:
                    radius = pairRadius;
                    damage = pairDamage;
                    breakDamage = pairBreak;
                    healFraction = 0f;
                    invulnerability = 0.40f;
                    break;
            }

            DamageEnemies(
                center,
                radius,
                damage,
                breakDamage,
                participantCount
            );

            BuffParticipants(
                mask,
                healFraction,
                invulnerability,
                tier
            );

            GameplayEventHub.Raise(
                new GameplayCue(
                    GameplayCueType.TeamSyncResolved,
                    -1,
                    center,
                    Vector2.up,
                    participantCount,
                    damage,
                    tier.ToString()
                )
            );
        }

        private void DamageEnemies(
            Vector2 center,
            float radius,
            float damage,
            float breakDamage,
            int participantCount)
        {
            hits.Clear();
            uniqueTargets.Clear();

            int count = Physics2D.OverlapCircle(
                center,
                radius,
                damageFilter,
                hits
            );

            for (int i = 0; i < count; i++)
            {
                Damageable2D target =
                    hits[i].GetComponentInParent<Damageable2D>();

                if (
                    !target ||
                    target.Faction != CombatFaction.Enemy ||
                    target.IsDefeated ||
                    !uniqueTargets.Add(target)
                )
                {
                    continue;
                }

                Vector2 direction =
                    (
                        (Vector2)target.transform.position -
                        center
                    ).normalized;

                DamagePacket packet =
                    new(
                        damage,
                        direction * 5f +
                        Vector2.up * 2.5f,
                        target.transform.position,
                        CombatFaction.Player,
                        -1,
                        participantCount,
                        "team-sync"
                    );

                target.ApplyDamage(packet);

                target
                    .GetComponent<CombatState2D>()
                    ?.AddBreak(
                        breakDamage,
                        packet
                    );
            }
        }

        private void BuffParticipants(
            int mask,
            float healFraction,
            float invulnerability,
            TeamSyncTier tier)
        {
            for (int i = 0; i < StrikeTeamSession.MaxPlayers; i++)
            {
                if ((mask & (1 << i)) == 0)
                    continue;

                StrikerPlayerIdentity player =
                    session.GetPlayer(i);

                if (!player)
                    continue;

                Damageable2D health =
                    player.GetComponent<Damageable2D>();

                if (
                    health &&
                    !health.IsDefeated
                )
                {
                    if (healFraction > 0f)
                    {
                        health.Heal(
                            health.MaxHealth *
                            healFraction
                        );
                    }

                    health.GrantInvulnerability(
                        invulnerability
                    );
                }

                if (tier == TeamSyncTier.FullStrike)
                {
                    player
                        .GetComponent<StrikeSuitAbilityController>()
                        ?.RestoreSuitEnergy(45f);
                }
            }
        }

        private bool TryGetParticipantCenter(
            int mask,
            out Vector2 center)
        {
            center = Vector2.zero;
            int count = 0;

            for (int i = 0; i < StrikeTeamSession.MaxPlayers; i++)
            {
                if ((mask & (1 << i)) == 0)
                    continue;

                StrikerPlayerIdentity player =
                    session.GetPlayer(i);

                if (!player)
                    continue;

                center +=
                    (Vector2)player.transform.position;

                count++;
            }

            if (count <= 0)
                return false;

            center /= count;
            return true;
        }
    }
}
