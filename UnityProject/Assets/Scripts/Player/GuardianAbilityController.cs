using System.Collections.Generic;
using NovaStriker.Combat;
using NovaStriker.Core;
using NovaStriker.Data;
using NovaStriker.Input;
using NovaStriker.Session;
using UnityEngine;

namespace NovaStriker.Player
{
    /// <summary>
    /// Player-usable Guardian abilities. These are gameplay implementations of
    /// the six established Guardian identities; production VFX/animation can
    /// replace the greybox presentation without changing the mechanics.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GuardianAbilityController : MonoBehaviour
    {
        [SerializeField] private StrikerPlayerIdentity identity;
        [SerializeField] private StrikerLoadoutController loadout;
        [SerializeField] private Damageable2D damageable;
        [SerializeField] private StrikeTeamSession session;

        [Header("Collision")]
        [SerializeField] private LayerMask damageableMask;

        [Header("Aegis")]
        [SerializeField] private float aegisRadius = 4f;
        [SerializeField] private float aegisInvulnerability = 1.15f;

        [Header("Cinder")]
        [SerializeField] private float cinderRadius = 3.5f;
        [SerializeField] private float cinderDamage = 22f;

        [Header("Mycel")]
        [SerializeField] private float mycelHealFraction = 0.30f;

        [Header("Rime")]
        [SerializeField] private float rimeRadius = 5f;
        [SerializeField] private float rimeSlowDuration = 4f;

        [Header("Tempest")]
        [SerializeField] private float tempestRadius = 4.5f;
        [SerializeField] private Vector2 tempestLiftVelocity = new(0f, 10.5f);

        [Header("Null")]
        [SerializeField] private float nullRadius = 5.5f;
        [SerializeField] private float nullPullSpeed = 8.5f;
        [SerializeField] private float nullVulnerableDuration = 4f;

        private readonly List<Collider2D> hits = new(32);
        private readonly HashSet<Damageable2D> uniqueTargets = new();
        private ContactFilter2D damageFilter;

        private bool activationPressed;
        private float cooldown;

        public float CooldownRemaining => cooldown;
        public bool Ready => cooldown <= 0f;

        private void Reset()
        {
            identity = GetComponent<StrikerPlayerIdentity>();
            loadout = GetComponent<StrikerLoadoutController>();
            damageable = GetComponent<Damageable2D>();
        }

        private void Awake()
        {
            if (!identity)
                identity = GetComponent<StrikerPlayerIdentity>();

            if (!loadout)
                loadout = GetComponent<StrikerLoadoutController>();

            if (!damageable)
                damageable = GetComponent<Damageable2D>();

            if (!session)
                session = StrikeTeamSession.Active;

            damageFilter = new ContactFilter2D
            {
                useTriggers = true
            };
            damageFilter.SetLayerMask(damageableMask);
        }

        public void SetInput(PlayerInputState input)
        {
            activationPressed |=
                input.GuardianActivatePressed;
        }

        private void FixedUpdate()
        {
            cooldown =
                Mathf.Max(
                    0f,
                    cooldown - Time.fixedDeltaTime
                );

            if (!session)
                session = StrikeTeamSession.Active;

            if (
                activationPressed &&
                Ready &&
                damageable &&
                !damageable.IsDefeated
            )
            {
                TryActivate();
            }

            activationPressed = false;
        }

        private void TryActivate()
        {
            GuardianDefinition guardian =
                loadout
                    ? loadout.CurrentGuardian
                    : null;

            if (!guardian)
                return;

            GameplayEventHub.Raise(
                new GameplayCue(
                    GameplayCueType.GuardianAbilityStarted,
                    identity ? identity.ActorId : 0,
                    transform.position,
                    Vector2.up,
                    (int)guardian.Id,
                    guardian.CooldownSeconds,
                    guardian.Id.ToString()
                )
            );

            switch (guardian.Id)
            {
                case GuardianId.Aegis:
                    ActivateAegis();
                    break;

                case GuardianId.Cinder:
                    ActivateCinder();
                    break;

                case GuardianId.Mycel:
                    ActivateMycel();
                    break;

                case GuardianId.Rime:
                    ActivateRime();
                    break;

                case GuardianId.Tempest:
                    ActivateTempest();
                    break;

                case GuardianId.Null:
                    ActivateNull();
                    break;
            }

            cooldown =
                Mathf.Max(
                    0f,
                    guardian.CooldownSeconds
                );

            GameplayEventHub.Raise(
                new GameplayCue(
                    GameplayCueType.GuardianAbilityResolved,
                    identity ? identity.ActorId : 0,
                    transform.position,
                    Vector2.up,
                    (int)guardian.Id,
                    cooldown,
                    guardian.Id.ToString()
                )
            );
        }

        private void ActivateAegis()
        {
            ForEachNearbyTeammate(
                aegisRadius,
                teammate =>
                {
                    Damageable2D health =
                        teammate.GetComponent<Damageable2D>();

                    health?.GrantInvulnerability(
                        aegisInvulnerability
                    );

                    teammate
                        .GetComponent<CombatState2D>()
                        ?.RestoreShield(35f);
                }
            );

            session?.AddSynergy(5f);
        }

        private void ActivateCinder()
        {
            ForEachEnemy(
                transform.position,
                cinderRadius,
                target =>
                {
                    Vector2 direction =
                        (
                            (Vector2)target.transform.position -
                            (Vector2)transform.position
                        ).normalized;

                    target.ApplyDamage(
                        new DamagePacket(
                            cinderDamage,
                            direction * 4f +
                            Vector2.up * 2f,
                            target.transform.position,
                            CombatFaction.Player,
                            identity ? identity.ActorId : 0,
                            2,
                            "guardian-cinder"
                        )
                    );

                    target
                        .GetComponent<CombatState2D>()
                        ?.ApplyWeaponStatus("magma", 2);
                }
            );
        }

        private void ActivateMycel()
        {
            if (!session)
                return;

            for (int i = 0; i < StrikeTeamSession.MaxPlayers; i++)
            {
                StrikerPlayerIdentity teammate =
                    session.GetPlayer(i);

                if (!teammate)
                    continue;

                Damageable2D health =
                    teammate.GetComponent<Damageable2D>();

                if (!health || health.IsDefeated)
                    continue;

                health.Heal(
                    health.MaxHealth *
                    mycelHealFraction
                );
            }

            session.AddSynergy(6f);
        }

        private void ActivateRime()
        {
            ForEachEnemy(
                transform.position,
                rimeRadius,
                target =>
                {
                    CombatState2D state =
                        target.GetComponent<CombatState2D>();

                    state?.ApplySlow(
                        rimeSlowDuration
                    );

                    state?.ApplyStagger(0.20f);
                }
            );
        }

        private void ActivateTempest()
        {
            ForEachNearbyTeammate(
                tempestRadius,
                teammate =>
                {
                    Damageable2D health =
                        teammate.GetComponent<Damageable2D>();

                    health?.ApplyExternalVelocity(
                        tempestLiftVelocity
                    );

                    health?.GrantInvulnerability(0.25f);
                }
            );
        }

        private void ActivateNull()
        {
            ForEachEnemy(
                transform.position,
                nullRadius,
                target =>
                {
                    Vector2 toCenter =
                        (
                            (Vector2)transform.position -
                            (Vector2)target.transform.position
                        );

                    Vector2 direction =
                        toCenter.sqrMagnitude > 0.0001f
                            ? toCenter.normalized
                            : Vector2.zero;

                    target.ApplyExternalVelocity(
                        direction * nullPullSpeed
                    );

                    target
                        .GetComponent<CombatState2D>()
                        ?.ApplyVulnerable(
                            nullVulnerableDuration
                        );
                }
            );
        }

        private void ForEachNearbyTeammate(
            float radius,
            System.Action<StrikerPlayerIdentity> action)
        {
            if (!session || action == null)
                return;

            float sqrRadius = radius * radius;

            for (int i = 0; i < StrikeTeamSession.MaxPlayers; i++)
            {
                StrikerPlayerIdentity teammate =
                    session.GetPlayer(i);

                if (!teammate)
                    continue;

                float sqr =
                    (
                        (Vector2)teammate.transform.position -
                        (Vector2)transform.position
                    ).sqrMagnitude;

                if (sqr <= sqrRadius)
                    action(teammate);
            }
        }

        private void ForEachEnemy(
            Vector2 center,
            float radius,
            System.Action<Damageable2D> action)
        {
            if (action == null)
                return;

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

                action(target);
            }
        }
    }
}
