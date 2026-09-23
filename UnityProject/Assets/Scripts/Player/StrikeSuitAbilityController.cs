using System.Collections.Generic;
using NovaStriker.Combat;
using NovaStriker.Core;
using NovaStriker.Data;
using NovaStriker.Input;
using NovaStriker.Session;
using UnityEngine;

namespace NovaStriker.Player
{
    public enum StrikeSuitAbilitySlot
    {
        Secondary1 = 0,
        Secondary2 = 1,
        Secondary3 = 2,
        Ultimate = 3
    }

    public enum StrikeSuitAbilityId
    {
        None = 0,

        NovaBulwarkPulse = 100,
        NovaSentinelLock = 101,
        NovaSentinelScreen = 102,
        NovaFrontlineProtocol = 103,

        EchoPursuitMark = 200,
        EchoReelStrike = 201,
        EchoStaffBurst = 202,
        EchoPursuitProtocol = 203
    }

    /// <summary>
    /// Character-specific Strike Suit abilities. This is gameplay authority;
    /// helmet states, gauntlet transforms, scarf motion, and VFX remain
    /// presentation concerns.
    ///
    /// Nova follows the approved soldier/Sentinel direction: protect allies,
    /// control projectiles, acquire targets, and hold a frontline.
    /// Echo follows the approved Pursuit Protocol direction: mark, close,
    /// grapple, and overwhelm targets with melee-oriented tools.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StrikeSuitAbilityController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private StrikerPlayerIdentity identity;
        [SerializeField] private NovaMotor2D motor;
        [SerializeField] private NovaCombatController combat;
        [SerializeField] private Damageable2D damageable;
        [SerializeField] private StrikerReviveInteractor reviveInteractor;
        [SerializeField] private StrikeTeamSession session;

        [Header("Collision")]
        [SerializeField] private LayerMask damageableMask;
        [SerializeField] private LayerMask projectileMask;
        [SerializeField] private LayerMask worldMask;

        [Header("Suit Energy")]
        [SerializeField] private float maxSuitEnergy = 100f;
        [SerializeField] private float suitEnergyRegenPerSecond = 14f;
        [SerializeField] private float secondary1Cost = 25f;
        [SerializeField] private float secondary2Cost = 30f;
        [SerializeField] private float secondary3Cost = 35f;

        [Header("Cooldowns")]
        [SerializeField] private float secondary1Cooldown = 5.5f;
        [SerializeField] private float secondary2Cooldown = 7.0f;
        [SerializeField] private float secondary3Cooldown = 8.5f;

        [Header("Ultimate")]
        [SerializeField] private float ultimateDuration = 8f;
        [SerializeField] private float ultimateChargeRequired = 100f;

        [Header("Nova — Sentinel")]
        [SerializeField] private float bulwarkRadius = 2.8f;
        [SerializeField] private float bulwarkDamage = 10f;
        [SerializeField] private float bulwarkAllyInvulnerability = 0.55f;
        [SerializeField] private float sentinelLockDuration = 6f;
        [SerializeField] private float sentinelScreenDuration = 4f;
        [SerializeField] private float sentinelScreenForwardOffset = 1.35f;
        [SerializeField] private float sentinelScreenRadius = 1.30f;

        [Header("Echo — Pursuit")]
        [SerializeField] private float pursuitMarkRange = 14f;
        [SerializeField] private float pursuitMarkDuration = 5f;
        [SerializeField] private float reelStrikeRange = 7f;
        [SerializeField] private float reelStrikeDamage = 14f;
        [SerializeField] private float staffBurstRadius = 2.35f;
        [SerializeField] private float staffBurstDamage = 18f;

        private readonly List<Collider2D> overlapHits = new(32);
        private readonly HashSet<Damageable2D> uniqueTargets = new();

        private ContactFilter2D damageFilter;
        private ContactFilter2D projectileFilter;

        private PlayerInputState input;

        private float suitEnergy;
        private float ultimateCharge;

        private float cooldown1;
        private float cooldown2;
        private float cooldown3;

        private float sentinelLockTimer;
        private float sentinelScreenTimer;
        private float ultimateTimer;

        public float SuitEnergy => suitEnergy;
        public float MaxSuitEnergy => maxSuitEnergy;
        public float UltimateCharge => ultimateCharge;
        public float UltimateChargeRequired => ultimateChargeRequired;

        public bool TacticalHelmetActive =>
            sentinelLockTimer > 0f ||
            ultimateTimer > 0f;

        public bool UltimateActive => ultimateTimer > 0f;

        public float FireChargeRateMultiplier
        {
            get
            {
                if (!combat)
                    return 1f;

                if (
                    combat.Character == StrikerCharacter.Nova &&
                    ultimateTimer > 0f
                )
                {
                    return 1.65f;
                }

                if (
                    combat.Character == StrikerCharacter.Nova &&
                    sentinelLockTimer > 0f
                )
                {
                    return 1.35f;
                }

                if (
                    combat.Character == StrikerCharacter.Echo &&
                    ultimateTimer > 0f
                )
                {
                    return 1.20f;
                }

                return 1f;
            }
        }

        public float OutgoingDamageMultiplier
        {
            get
            {
                if (!combat || ultimateTimer <= 0f)
                    return 1f;

                return combat.Character == StrikerCharacter.Nova
                    ? 1.30f
                    : 1.35f;
            }
        }

        public float DeflectRadiusMultiplier =>
            combat &&
            combat.Character == StrikerCharacter.Nova &&
            ultimateTimer > 0f
                ? 1.55f
                : sentinelLockTimer > 0f
                    ? 1.20f
                    : 1f;

        public float GrapplePullMultiplier =>
            combat &&
            combat.Character == StrikerCharacter.Echo &&
            ultimateTimer > 0f
                ? 1.55f
                : 1f;

        private void Reset()
        {
            identity = GetComponent<StrikerPlayerIdentity>();
            motor = GetComponent<NovaMotor2D>();
            combat = GetComponent<NovaCombatController>();
            damageable = GetComponent<Damageable2D>();
            reviveInteractor = GetComponent<StrikerReviveInteractor>();
        }

        private void Awake()
        {
            if (!identity)
                identity = GetComponent<StrikerPlayerIdentity>();

            if (!motor)
                motor = GetComponent<NovaMotor2D>();

            if (!combat)
                combat = GetComponent<NovaCombatController>();

            if (!damageable)
                damageable = GetComponent<Damageable2D>();

            if (!reviveInteractor)
                reviveInteractor = GetComponent<StrikerReviveInteractor>();

            if (!session)
                session = StrikeTeamSession.Active;

            suitEnergy = maxSuitEnergy;

            damageFilter = new ContactFilter2D
            {
                useTriggers = true
            };
            damageFilter.SetLayerMask(damageableMask);

            projectileFilter = new ContactFilter2D
            {
                useTriggers = true
            };
            projectileFilter.SetLayerMask(projectileMask);
        }

        private void OnEnable()
        {
            GameplayEventHub.CueRaised += OnGameplayCue;
        }

        private void OnDisable()
        {
            GameplayEventHub.CueRaised -= OnGameplayCue;
        }

        public void SetInput(PlayerInputState state)
        {
            input.Ability1Pressed |= state.Ability1Pressed;
            input.Ability2Pressed |= state.Ability2Pressed;
            input.Ability3Pressed |= state.Ability3Pressed;
            input.UltimatePressed |= state.UltimatePressed;
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            if (!session)
                session = StrikeTeamSession.Active;

            suitEnergy =
                Mathf.Min(
                    maxSuitEnergy,
                    suitEnergy +
                    suitEnergyRegenPerSecond * dt
                );

            cooldown1 = Mathf.Max(0f, cooldown1 - dt);
            cooldown2 = Mathf.Max(0f, cooldown2 - dt);
            cooldown3 = Mathf.Max(0f, cooldown3 - dt);

            sentinelLockTimer =
                Mathf.Max(0f, sentinelLockTimer - dt);

            sentinelScreenTimer =
                Mathf.Max(0f, sentinelScreenTimer - dt);

            bool ultimateWasActive =
                ultimateTimer > 0f;

            ultimateTimer =
                Mathf.Max(0f, ultimateTimer - dt);

            if (
                ultimateWasActive &&
                ultimateTimer <= 0f
            )
            {
                RaiseAbilityCue(
                    GameplayCueType.SuitUltimateEnded,
                    CurrentUltimateId(),
                    0f
                );
            }

            if (
                sentinelScreenTimer > 0f &&
                combat &&
                combat.Character == StrikerCharacter.Nova
            )
            {
                UpdateNovaSentinelScreen();
            }

            if (
                !damageable ||
                damageable.IsDefeated ||
                (reviveInteractor && reviveInteractor.IsReviving)
            )
            {
                ClearInputEdges();
                return;
            }

            if (input.Ability1Pressed)
                TryActivateSecondary(0);

            if (input.Ability2Pressed)
                TryActivateSecondary(1);

            if (input.Ability3Pressed)
                TryActivateSecondary(2);

            if (input.UltimatePressed)
                TryActivateUltimate();

            ClearInputEdges();
        }

        private void TryActivateSecondary(int index)
        {
            float cost =
                index switch
                {
                    0 => secondary1Cost,
                    1 => secondary2Cost,
                    _ => secondary3Cost
                };

            float cooldown =
                index switch
                {
                    0 => cooldown1,
                    1 => cooldown2,
                    _ => cooldown3
                };

            if (
                cooldown > 0f ||
                suitEnergy + 0.001f < cost ||
                !combat
            )
            {
                return;
            }

            bool activated =
                combat.Character == StrikerCharacter.Nova
                    ? ActivateNovaSecondary(index)
                    : ActivateEchoSecondary(index);

            if (!activated)
                return;

            suitEnergy -= cost;

            switch (index)
            {
                case 0:
                    cooldown1 = secondary1Cooldown;
                    break;
                case 1:
                    cooldown2 = secondary2Cooldown;
                    break;
                default:
                    cooldown3 = secondary3Cooldown;
                    break;
            }
        }

        private bool ActivateNovaSecondary(int index)
        {
            switch (index)
            {
                case 0:
                    NovaBulwarkPulse();
                    return true;

                case 1:
                    sentinelLockTimer =
                        Mathf.Max(
                            sentinelLockTimer,
                            sentinelLockDuration
                        );

                    RaiseAbilityCue(
                        GameplayCueType.SuitAbilityStarted,
                        StrikeSuitAbilityId.NovaSentinelLock,
                        sentinelLockDuration
                    );
                    return true;

                case 2:
                    sentinelScreenTimer =
                        Mathf.Max(
                            sentinelScreenTimer,
                            sentinelScreenDuration
                        );

                    RaiseAbilityCue(
                        GameplayCueType.SuitAbilityStarted,
                        StrikeSuitAbilityId.NovaSentinelScreen,
                        sentinelScreenDuration
                    );
                    return true;
            }

            return false;
        }

        private bool ActivateEchoSecondary(int index)
        {
            switch (index)
            {
                case 0:
                    return EchoPursuitMark();

                case 1:
                    return EchoReelStrike();

                case 2:
                    EchoStaffBurst();
                    return true;
            }

            return false;
        }

        private void NovaBulwarkPulse()
        {
            RaiseAbilityCue(
                GameplayCueType.SuitAbilityStarted,
                StrikeSuitAbilityId.NovaBulwarkPulse,
                bulwarkRadius
            );

            CancelHostileProjectiles(
                transform.position,
                bulwarkRadius,
                "nova-bulwark-pulse"
            );

            uniqueTargets.Clear();
            overlapHits.Clear();

            int count = Physics2D.OverlapCircle(
                transform.position,
                bulwarkRadius,
                damageFilter,
                overlapHits
            );

            for (int i = 0; i < count; i++)
            {
                Damageable2D target =
                    overlapHits[i].GetComponentInParent<Damageable2D>();

                if (
                    !target ||
                    target.Faction != CombatFaction.Enemy ||
                    !uniqueTargets.Add(target)
                )
                {
                    continue;
                }

                Vector2 direction =
                    (
                        (Vector2)target.transform.position -
                        (Vector2)transform.position
                    ).normalized;

                target.ApplyDamage(
                    new DamagePacket(
                        bulwarkDamage,
                        direction * 4.5f + Vector2.up * 1.5f,
                        target.transform.position,
                        CombatFaction.Player,
                        identity ? identity.ActorId : 0,
                        1,
                        "nova-bulwark-pulse"
                    )
                );

                target
                    .GetComponent<CombatState2D>()
                    ?.AddBreak(
                        12f,
                        new DamagePacket(
                            0f,
                            direction,
                            target.transform.position,
                            CombatFaction.Player,
                            identity ? identity.ActorId : 0,
                            1,
                            "nova-bulwark-pulse"
                        )
                    );
            }

            if (session)
            {
                for (int i = 0; i < StrikeTeamSession.MaxPlayers; i++)
                {
                    StrikerPlayerIdentity teammate =
                        session.GetPlayer(i);

                    if (!teammate)
                        continue;

                    float distance =
                        Vector2.Distance(
                            transform.position,
                            teammate.transform.position
                        );

                    if (distance > bulwarkRadius + 0.6f)
                        continue;

                    teammate
                        .GetComponent<Damageable2D>()
                        ?.GrantInvulnerability(
                            bulwarkAllyInvulnerability
                        );
                }

                session.AddSynergy(4f);
            }

            RaiseAbilityCue(
                GameplayCueType.SuitAbilityResolved,
                StrikeSuitAbilityId.NovaBulwarkPulse,
                bulwarkDamage
            );
        }

        private void UpdateNovaSentinelScreen()
        {
            Vector2 center =
                (Vector2)transform.position +
                new Vector2(
                    motor ? motor.Facing : 1,
                    0f
                ) *
                sentinelScreenForwardOffset;

            CancelHostileProjectiles(
                center,
                sentinelScreenRadius,
                "nova-sentinel-screen"
            );
        }

        private bool EchoPursuitMark()
        {
            Damageable2D target =
                FindBestEnemy(
                    pursuitMarkRange,
                    true
                );

            if (!target)
                return false;

            CombatState2D state =
                target.GetComponent<CombatState2D>();

            if (!state)
                return false;

            state.ApplyVulnerable(
                pursuitMarkDuration
            );

            RaiseAbilityCue(
                GameplayCueType.SuitAbilityResolved,
                StrikeSuitAbilityId.EchoPursuitMark,
                pursuitMarkDuration,
                target.transform.position
            );

            session?.AddSynergy(2f);
            return true;
        }

        private bool EchoReelStrike()
        {
            Damageable2D target =
                FindBestEnemy(
                    reelStrikeRange,
                    true
                );

            if (!target || !motor)
                return false;

            Vector2 direction =
                (
                    (Vector2)target.transform.position -
                    (Vector2)transform.position
                ).normalized;

            motor.BeginCounterDodge(
                direction,
                12.5f,
                0.15f
            );

            target.ApplyDamage(
                new DamagePacket(
                    reelStrikeDamage *
                    OutgoingDamageMultiplier,
                    direction * 3.4f +
                    Vector2.up * 1.6f,
                    target.transform.position,
                    CombatFaction.Player,
                    identity ? identity.ActorId : 0,
                    1,
                    "echo-reel-strike"
                )
            );

            target
                .GetComponent<CombatState2D>()
                ?.ApplyStagger(0.25f);

            RaiseAbilityCue(
                GameplayCueType.SuitAbilityResolved,
                StrikeSuitAbilityId.EchoReelStrike,
                reelStrikeDamage,
                target.transform.position
            );

            return true;
        }

        private void EchoStaffBurst()
        {
            RaiseAbilityCue(
                GameplayCueType.SuitAbilityStarted,
                StrikeSuitAbilityId.EchoStaffBurst,
                staffBurstRadius
            );

            uniqueTargets.Clear();
            overlapHits.Clear();

            int count = Physics2D.OverlapCircle(
                transform.position,
                staffBurstRadius,
                damageFilter,
                overlapHits
            );

            for (int i = 0; i < count; i++)
            {
                Damageable2D target =
                    overlapHits[i].GetComponentInParent<Damageable2D>();

                if (
                    !target ||
                    target.Faction != CombatFaction.Enemy ||
                    !uniqueTargets.Add(target)
                )
                {
                    continue;
                }

                Vector2 direction =
                    (
                        (Vector2)target.transform.position -
                        (Vector2)transform.position
                    ).normalized;

                target.ApplyDamage(
                    new DamagePacket(
                        staffBurstDamage *
                        OutgoingDamageMultiplier,
                        direction * 5.4f +
                        Vector2.up * 2.2f,
                        target.transform.position,
                        CombatFaction.Player,
                        identity ? identity.ActorId : 0,
                        2,
                        "echo-staff-burst"
                    )
                );

                target
                    .GetComponent<CombatState2D>()
                    ?.ApplyStagger(0.35f);
            }

            RaiseAbilityCue(
                GameplayCueType.SuitAbilityResolved,
                StrikeSuitAbilityId.EchoStaffBurst,
                staffBurstDamage
            );
        }

        private void TryActivateUltimate()
        {
            if (
                !combat ||
                ultimateTimer > 0f ||
                ultimateCharge + 0.001f <
                ultimateChargeRequired
            )
            {
                return;
            }

            ultimateCharge = 0f;
            ultimateTimer = ultimateDuration;

            StrikeSuitAbilityId id =
                CurrentUltimateId();

            if (combat.Character == StrikerCharacter.Echo)
            {
                cooldown1 = 0f;
                cooldown2 = 0f;
                cooldown3 = 0f;
            }
            else
            {
                damageable?.GrantInvulnerability(0.6f);

                if (session)
                {
                    for (int i = 0; i < StrikeTeamSession.MaxPlayers; i++)
                    {
                        StrikerPlayerIdentity teammate =
                            session.GetPlayer(i);

                        teammate
                            ?.GetComponent<Damageable2D>()
                            ?.GrantInvulnerability(0.35f);
                    }
                }
            }

            RaiseAbilityCue(
                GameplayCueType.SuitUltimateStarted,
                id,
                ultimateDuration
            );
        }

        private Damageable2D FindBestEnemy(
            float radius,
            bool requireLineOfSight)
        {
            overlapHits.Clear();

            int count = Physics2D.OverlapCircle(
                transform.position,
                radius,
                damageFilter,
                overlapHits
            );

            Vector2 aim =
                motor &&
                motor.AimDirection.sqrMagnitude > 0.0001f
                    ? motor.AimDirection.normalized
                    : Vector2.right;

            Damageable2D best = null;
            float bestScore = float.NegativeInfinity;

            for (int i = 0; i < count; i++)
            {
                Damageable2D target =
                    overlapHits[i].GetComponentInParent<Damageable2D>();

                if (
                    !target ||
                    target.Faction != CombatFaction.Enemy ||
                    target.IsDefeated
                )
                {
                    continue;
                }

                Vector2 delta =
                    (Vector2)target.transform.position -
                    (Vector2)transform.position;

                float distance = delta.magnitude;

                if (distance <= 0.001f)
                    continue;

                if (
                    requireLineOfSight &&
                    worldMask.value != 0 &&
                    Physics2D.Linecast(
                        transform.position,
                        target.transform.position,
                        worldMask
                    ).collider
                )
                {
                    continue;
                }

                float alignment =
                    Vector2.Dot(
                        aim,
                        delta / distance
                    );

                float score =
                    alignment * 2.5f -
                    distance / Mathf.Max(0.01f, radius);

                if (score <= bestScore)
                    continue;

                bestScore = score;
                best = target;
            }

            return best;
        }

        private void CancelHostileProjectiles(
            Vector2 center,
            float radius,
            string sourceId)
        {
            overlapHits.Clear();

            int count = Physics2D.OverlapCircle(
                center,
                radius,
                projectileFilter,
                overlapHits
            );

            for (int i = 0; i < count; i++)
            {
                Projectile2D projectile =
                    overlapHits[i].GetComponentInParent<Projectile2D>();

                if (
                    !projectile ||
                    projectile.Faction != CombatFaction.Enemy
                )
                {
                    continue;
                }

                projectile.TryCancel(
                    CombatFaction.Player,
                    identity ? identity.ActorId : 0,
                    sourceId
                );
            }
        }

        private void OnGameplayCue(GameplayCue cue)
        {
            if (
                !identity ||
                cue.ActorId != identity.ActorId
            )
            {
                return;
            }

            float gain =
                cue.Type switch
                {
                    GameplayCueType.ProjectileCancelled => 3f,
                    GameplayCueType.MeleeHit => 2f,
                    GameplayCueType.CounterDeflect => 4f,
                    GameplayCueType.PerfectParry => 8f,
                    GameplayCueType.CounterDodge => 4f,
                    GameplayCueType.CounterThrow => 3f,
                    GameplayCueType.CounterGrapple => 2f,
                    GameplayCueType.Revived => 8f,
                    _ => 0f
                };

            if (gain <= 0f)
                return;

            ultimateCharge =
                Mathf.Clamp(
                    ultimateCharge + gain,
                    0f,
                    ultimateChargeRequired
                );
        }

        private StrikeSuitAbilityId CurrentUltimateId()
        {
            return
                combat &&
                combat.Character == StrikerCharacter.Echo
                    ? StrikeSuitAbilityId.EchoPursuitProtocol
                    : StrikeSuitAbilityId.NovaFrontlineProtocol;
        }

        private void RaiseAbilityCue(
            GameplayCueType type,
            StrikeSuitAbilityId id,
            float value,
            Vector2? positionOverride = null)
        {
            GameplayEventHub.Raise(
                new GameplayCue(
                    type,
                    identity ? identity.ActorId : 0,
                    positionOverride ?? (Vector2)transform.position,
                    motor ? motor.AimDirection : Vector2.right,
                    0,
                    value,
                    id.ToString()
                )
            );
        }

        private void ClearInputEdges()
        {
            input.Ability1Pressed = false;
            input.Ability2Pressed = false;
            input.Ability3Pressed = false;
            input.UltimatePressed = false;
        }
    }
}
