using System.Collections.Generic;
using NovaStriker.Combat;
using NovaStriker.Core;
using NovaStriker.Data;
using NovaStriker.Input;
using UnityEngine;

namespace NovaStriker.Player
{
    public enum CounterMode
    {
        Deflect = 0,
        Grapple = 1,
        DodgeCounter = 2,
        Throw = 3
    }

    /// <summary>
    /// Greybox combat controller for Nova/Echo.
    ///
    /// Circle / Counter is contextual:
    /// - moving toward a nearby enemy -> Throw
    /// - close enemy -> Dodge + Counter
    /// - distance: Nova -> Deflect, Echo -> Grapple
    ///
    /// The gameplay layer chooses the action; animation/VFX/audio receive
    /// character-specific cues and can present Nova/Echo differently.
    /// </summary>
    public sealed class NovaCombatController : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private int playerId;
        [SerializeField] private StrikerCharacter character = StrikerCharacter.Nova;

        [Header("References")]
        [SerializeField] private NovaMotor2D motor;
        [SerializeField] private Damageable2D selfDamageable;
        [SerializeField] private Transform muzzleSocket;
        [SerializeField] private Projectile2D projectilePrefab;
        [SerializeField] private WeaponDefinition equippedWeapon;

        [Header("Collision")]
        [SerializeField] private LayerMask damageableMask;
        [SerializeField] private LayerMask projectileMask;

        [Header("Context Counter — Ranges")]
        [Tooltip("Nova projectile-deflection radius.")]
        [SerializeField] private float deflectRadius = 0.72f;
        [Tooltip("Inside this range, a non-advancing counter becomes Dodge + Counter.")]
        [SerializeField] private float closeCounterRadius = 1.15f;
        [Tooltip("Advancing toward an enemy inside this range triggers Throw.")]
        [SerializeField] private float throwRange = 1.35f;
        [Tooltip("Echo can grapple enemies at or inside this range.")]
        [SerializeField] private float grappleRange = 4.25f;
        [Tooltip("How directly movement must point toward the target to count as advancing.")]
        [SerializeField, Range(-1f, 1f)] private float throwApproachDot = 0.55f;

        [Header("Context Counter — Timing")]
        [SerializeField] private ParryWindow parryWindow;

        [Header("Context Counter — Dodge")]
        [SerializeField] private float dodgeCounterDamage = 14f;
        [SerializeField] private Vector2 dodgeCounterKnockback = new(4.8f, 2.0f);
        [SerializeField] private float dodgeSpeed = 7.4f;
        [SerializeField] private float dodgeDuration = 0.11f;
        [SerializeField] private float dodgeInvulnerability = 0.18f;

        [Header("Context Counter — Throw")]
        [SerializeField] private float throwDamage = 10f;
        [SerializeField] private Vector2 throwKnockback = new(7.0f, 4.5f);
        [SerializeField] private float throwInvulnerability = 0.12f;

        [Header("Context Counter — Echo Grapple")]
        [SerializeField] private float grapplePullSpeed = 10.5f;
        [SerializeField] private float grappleLift = 1.4f;

        [Header("Melee")]
        [SerializeField] private float groundMeleeDuration = 0.20f;
        [SerializeField] private float airMeleeDuration = 0.18f;
        [SerializeField] private float diveMeleeDuration = 0.22f;
        [SerializeField] private float comboReset = 0.38f;

        private readonly List<Collider2D> parryHits = new(24);
        private readonly List<Collider2D> meleeHits = new(24);
        private readonly List<Collider2D> counterHits = new(24);

        private ContactFilter2D parryFilter;
        private ContactFilter2D meleeFilter;
        private ContactFilter2D counterFilter;

        private readonly HashSet<int> meleeTargets = new();

        private PlayerInputState input;

        private float fireCharge;

        private float counterElapsed = -1f;
        private bool contextualCounterPending;
        private Damageable2D contextualCounterTarget;
        private CounterMode currentCounterMode = CounterMode.Deflect;

        private float meleeTimer;
        private float meleeDuration;
        private float meleeResetTimer;
        private int meleeStep;

        private float airMeleeResetTimer;
        private int airMeleeStep;

        public int PlayerId => playerId;
        public StrikerCharacter Character => character;
        public float FireCharge => fireCharge;

        public bool IsCountering => counterElapsed >= 0f;
        public bool IsParrying => IsCountering;
        public bool IsParryActive =>
            IsCountering &&
            currentCounterMode == CounterMode.Deflect &&
            parryWindow.IsActive(counterElapsed);
        public bool IsPerfectParryWindow =>
            IsCountering &&
            currentCounterMode == CounterMode.Deflect &&
            parryWindow.IsPerfect(counterElapsed);
        public CounterMode CurrentCounterMode => currentCounterMode;

        public bool IsMeleeActive => meleeTimer > 0f;
        public int MeleeStep => meleeStep;
        public WeaponDefinition EquippedWeapon => equippedWeapon;

        private void Reset()
        {
            motor = GetComponent<NovaMotor2D>();
            selfDamageable = GetComponent<Damageable2D>();
            parryWindow = ParryWindow.Default;
        }

        private void Awake()
        {
            if (!motor)
                motor = GetComponent<NovaMotor2D>();

            if (!selfDamageable)
                selfDamageable = GetComponent<Damageable2D>();

            if (parryWindow.TotalDuration <= 0f)
                parryWindow = ParryWindow.Default;

            parryFilter = new ContactFilter2D
            {
                useTriggers = true
            };
            parryFilter.SetLayerMask(projectileMask);

            meleeFilter = new ContactFilter2D
            {
                useTriggers = true
            };
            meleeFilter.SetLayerMask(damageableMask);

            counterFilter = new ContactFilter2D
            {
                useTriggers = true
            };
            counterFilter.SetLayerMask(damageableMask);
        }

        public void SetInput(PlayerInputState state)
        {
            input = state;
        }

        public void SetWeapon(WeaponDefinition weapon)
        {
            equippedWeapon = weapon;
        }

        public void SetCharacter(StrikerCharacter value)
        {
            character = value;

            if (!IsCountering)
                currentCounterMode = DefaultDistanceMode();
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            UpdateTimers(dt);
            UpdateFire(dt);
            UpdateCounter();
            UpdateMelee();
        }

        private void UpdateTimers(float dt)
        {
            if (counterElapsed >= 0f)
            {
                counterElapsed += dt;

                if (counterElapsed > parryWindow.TotalDuration)
                {
                    counterElapsed = -1f;
                    contextualCounterPending = false;
                    contextualCounterTarget = null;
                    currentCounterMode = DefaultDistanceMode();
                }
            }

            meleeResetTimer = Mathf.Max(0f, meleeResetTimer - dt);
            airMeleeResetTimer = Mathf.Max(0f, airMeleeResetTimer - dt);

            if (meleeTimer > 0f)
                meleeTimer = Mathf.Max(0f, meleeTimer - dt);
        }

        private void UpdateFire(float dt)
        {
            if (input.FireHeld)
            {
                bool wasIdle = fireCharge <= 0f;
                fireCharge += dt;

                if (wasIdle)
                {
                    GameplayEventHub.Raise(new GameplayCue(
                        GameplayCueType.FireChargeStarted,
                        playerId,
                        muzzleSocket ? muzzleSocket.position : transform.position,
                        AimDirection()
                    ));
                }
            }

            if (!input.FireReleased)
                return;

            int tier = ShotChargeRules.TierFromSeconds(fireCharge);
            FireWeapon(tier);
            fireCharge = 0f;
        }

        private void FireWeapon(int tier)
        {
            if (!equippedWeapon || !projectilePrefab)
                return;

            Vector2 aim = AimDirection();
            float baseAngle = Mathf.Atan2(aim.y, aim.x);

            switch (equippedWeapon.Behavior)
            {
                case WeaponBehavior.Spread:
                {
                    int count = tier == 3 ? 7 : tier == 2 ? 5 : 3;
                    float spread = tier == 3 ? 0.52f : tier == 2 ? 0.34f : 0.20f;

                    for (int i = 0; i < count; i++)
                    {
                        float t = count <= 1 ? 0.5f : (float)i / (count - 1);
                        SpawnProjectile(tier, baseAngle + Mathf.Lerp(-spread, spread, t));
                    }
                    break;
                }

                case WeaponBehavior.Cyclone:
                {
                    int count = tier == 3 ? 8 : tier == 2 ? 5 : 3;

                    for (int i = 0; i < count; i++)
                        SpawnProjectile(tier, Mathf.PI * 2f * i / count);

                    break;
                }

                case WeaponBehavior.Beam when tier == 3:
                {
                    for (int i = -2; i <= 2; i++)
                        SpawnProjectile(tier, baseAngle + i * 0.018f);

                    break;
                }

                default:
                    SpawnProjectile(tier, baseAngle);
                    break;
            }

            GameplayEventHub.Raise(new GameplayCue(
                GameplayCueType.FireChargeReleased,
                playerId,
                muzzleSocket ? muzzleSocket.position : transform.position,
                aim,
                tier,
                fireCharge,
                equippedWeapon.Id
            ));
        }

        private void SpawnProjectile(int tier, float angle)
        {
            Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector3 origin = muzzleSocket
                ? muzzleSocket.position
                : transform.position + (Vector3)(direction * 0.52f);

            Projectile2D shot = Instantiate(
                projectilePrefab,
                origin,
                Quaternion.identity
            );

            bool piercing =
                equippedWeapon.Behavior == WeaponBehavior.Pierce ||
                equippedWeapon.Behavior == WeaponBehavior.Spear ||
                (
                    equippedWeapon.Behavior == WeaponBehavior.Null &&
                    tier >= 1
                );

            shot.Initialize(
                playerId,
                CombatFaction.Player,
                equippedWeapon.Id,
                equippedWeapon.Behavior,
                direction,
                equippedWeapon.ProjectileSpeed,
                tier,
                equippedWeapon.DamageForTier(tier),
                piercing
            );

            float diameter = equippedWeapon.RadiusForTier(tier) * 2f;
            shot.transform.localScale = new Vector3(diameter, diameter, 1f);

            GameplayEventHub.Raise(new GameplayCue(
                GameplayCueType.ProjectileFired,
                playerId,
                origin,
                direction,
                tier,
                equippedWeapon.DamageForTier(tier),
                equippedWeapon.Id
            ));
        }

        private void UpdateCounter()
        {
            if (
                input.CounterPressed &&
                counterElapsed < 0f
            )
            {
                BeginContextCounter();
            }

            if (!IsCountering)
                return;

            if (
                contextualCounterPending &&
                parryWindow.IsActive(counterElapsed)
            )
            {
                contextualCounterPending = false;
                ExecuteContextCounter();
            }

            if (
                currentCounterMode != CounterMode.Deflect ||
                !parryWindow.IsActive(counterElapsed)
            )
            {
                return;
            }

            UpdateProjectileDeflect();
        }

        private void BeginContextCounter()
        {
            counterElapsed = 0f;

            float searchRadius =
                character == StrikerCharacter.Echo
                    ? Mathf.Max(grappleRange, throwRange)
                    : Mathf.Max(closeCounterRadius, throwRange);

            contextualCounterTarget =
                FindNearestCounterTarget(
                    searchRadius,
                    out float distance
                );

            currentCounterMode =
                SelectCounterMode(
                    contextualCounterTarget,
                    distance
                );

            contextualCounterPending =
                currentCounterMode != CounterMode.Deflect;

            if (
                contextualCounterTarget &&
                motor
            )
            {
                motor.FaceToward(
                    contextualCounterTarget.transform.position.x
                );
            }

            string actionId =
                $"{character.ToString().ToLowerInvariant()}-" +
                $"{currentCounterMode.ToString().ToLowerInvariant()}";

            GameplayEventHub.Raise(new GameplayCue(
                GameplayCueType.CounterStarted,
                playerId,
                transform.position,
                contextualCounterTarget
                    ? DirectionTo(contextualCounterTarget.transform.position)
                    : AimDirection(),
                (int)currentCounterMode,
                contextualCounterTarget ? distance : 0f,
                actionId
            ));
        }

        private CounterMode SelectCounterMode(
            Damageable2D target,
            float distance)
        {
            if (target)
            {
                Vector2 toTarget =
                    DirectionTo(target.transform.position);

                if (
                    distance <= throwRange &&
                    IsMovingToward(toTarget)
                )
                {
                    return CounterMode.Throw;
                }

                if (distance <= closeCounterRadius)
                    return CounterMode.DodgeCounter;
            }

            if (character == StrikerCharacter.Echo)
                return CounterMode.Grapple;

            return CounterMode.Deflect;
        }

        private bool IsMovingToward(Vector2 directionToTarget)
        {
            if (input.Move.sqrMagnitude < 0.1225f)
                return false;

            Vector2 moveDirection = input.Move.normalized;

            return Vector2.Dot(
                moveDirection,
                directionToTarget
            ) >= throwApproachDot;
        }

        private CounterMode DefaultDistanceMode()
        {
            return character == StrikerCharacter.Echo
                ? CounterMode.Grapple
                : CounterMode.Deflect;
        }

        private void ExecuteContextCounter()
        {
            switch (currentCounterMode)
            {
                case CounterMode.Grapple:
                    ExecuteEchoGrapple();
                    break;

                case CounterMode.DodgeCounter:
                    ExecuteDodgeCounter();
                    break;

                case CounterMode.Throw:
                    ExecuteThrow();
                    break;
            }
        }

        private void ExecuteDodgeCounter()
        {
            if (!HasValidCounterTarget(closeCounterRadius + 0.25f))
                return;

            Vector2 targetPosition =
                contextualCounterTarget.transform.position;

            Vector2 towardTarget =
                DirectionTo(targetPosition);

            if (motor)
            {
                motor.FaceToward(targetPosition.x);

                Vector2 dodgeDirection = new(
                    -Mathf.Sign(towardTarget.x == 0f ? motor.Facing : towardTarget.x),
                    0.16f
                );

                motor.BeginCounterDodge(
                    dodgeDirection,
                    dodgeSpeed,
                    dodgeDuration
                );
            }

            selfDamageable?.GrantInvulnerability(
                dodgeInvulnerability
            );

            Vector2 knockback = new(
                towardTarget.x * dodgeCounterKnockback.x,
                dodgeCounterKnockback.y
            );

            string actionId =
                $"{character.ToString().ToLowerInvariant()}-dodge-counter";

            bool applied =
                contextualCounterTarget.ApplyDamage(
                    new DamagePacket(
                        dodgeCounterDamage,
                        knockback,
                        targetPosition,
                        CombatFaction.Player,
                        playerId,
                        2,
                        actionId
                    )
                );

            if (!applied)
                return;

            GameplayEventHub.Raise(new GameplayCue(
                GameplayCueType.CounterDodge,
                playerId,
                targetPosition,
                towardTarget,
                2,
                dodgeCounterDamage,
                actionId
            ));
        }

        private void ExecuteThrow()
        {
            if (!HasValidCounterTarget(throwRange + 0.20f))
                return;

            Vector2 targetPosition =
                contextualCounterTarget.transform.position;

            Vector2 towardTarget =
                DirectionTo(targetPosition);

            if (motor)
                motor.FaceToward(targetPosition.x);

            selfDamageable?.GrantInvulnerability(
                throwInvulnerability
            );

            Vector2 throwDirection =
                input.Move.sqrMagnitude > 0.1225f
                    ? input.Move.normalized
                    : towardTarget;

            if (Mathf.Abs(throwDirection.x) < 0.25f)
                throwDirection.x = towardTarget.x;

            Vector2 knockback = new(
                Mathf.Sign(throwDirection.x) * throwKnockback.x,
                throwKnockback.y
            );

            string actionId =
                $"{character.ToString().ToLowerInvariant()}-throw";

            bool applied =
                contextualCounterTarget.ApplyDamage(
                    new DamagePacket(
                        throwDamage,
                        knockback,
                        targetPosition,
                        CombatFaction.Player,
                        playerId,
                        2,
                        actionId
                    )
                );

            if (!applied)
                return;

            GameplayEventHub.Raise(new GameplayCue(
                GameplayCueType.CounterThrow,
                playerId,
                targetPosition,
                throwDirection,
                2,
                throwDamage,
                actionId
            ));
        }

        private void ExecuteEchoGrapple()
        {
            Vector2 grappleDirection =
                contextualCounterTarget
                    ? DirectionTo(contextualCounterTarget.transform.position)
                    : AimDirection();

            if (
                !contextualCounterTarget ||
                contextualCounterTarget.IsDefeated
            )
            {
                GameplayEventHub.Raise(new GameplayCue(
                    GameplayCueType.CounterGrapple,
                    playerId,
                    transform.position,
                    grappleDirection,
                    0,
                    0f,
                    "echo-grapple-whiff"
                ));
                return;
            }

            float distance =
                Vector2.Distance(
                    transform.position,
                    contextualCounterTarget.transform.position
                );

            if (distance > grappleRange + 0.25f)
                return;

            Vector2 targetToEcho =
                (
                    (Vector2)transform.position -
                    (Vector2)contextualCounterTarget.transform.position
                ).normalized;

            Vector2 pullVelocity =
                targetToEcho * grapplePullSpeed +
                Vector2.up * grappleLift;

            contextualCounterTarget.ApplyExternalVelocity(
                pullVelocity
            );

            if (motor)
            {
                motor.FaceToward(
                    contextualCounterTarget.transform.position.x
                );
            }

            GameplayEventHub.Raise(new GameplayCue(
                GameplayCueType.CounterGrapple,
                playerId,
                contextualCounterTarget.transform.position,
                targetToEcho,
                1,
                distance,
                "echo-grapple"
            ));
        }

        private void UpdateProjectileDeflect()
        {
            parryHits.Clear();

            int count = Physics2D.OverlapCircle(
                transform.position,
                deflectRadius,
                parryFilter,
                parryHits
            );

            bool perfect =
                parryWindow.IsPerfect(counterElapsed);

            for (int i = 0; i < count; i++)
            {
                Projectile2D projectile =
                    parryHits[i].GetComponentInParent<Projectile2D>();

                if (!projectile)
                    continue;

                bool bonusOpportunity =
                    projectile.PerfectOpportunity;

                if (!projectile.TryParry(playerId, perfect))
                    continue;

                GameplayEventHub.Raise(new GameplayCue(
                    GameplayCueType.CounterDeflect,
                    playerId,
                    projectile.transform.position,
                    projectile.Velocity.normalized,
                    projectile.Tier,
                    projectile.Damage,
                    "nova-deflect"
                ));

                GameplayEventHub.Raise(new GameplayCue(
                    perfect
                        ? GameplayCueType.PerfectParry
                        : GameplayCueType.ParrySuccess,
                    playerId,
                    projectile.transform.position,
                    projectile.Velocity.normalized,
                    projectile.Tier,
                    bonusOpportunity && perfect ? 48f : projectile.Damage,
                    projectile.WeaponId
                ));
            }
        }

        private bool HasValidCounterTarget(float allowedRange)
        {
            if (
                !contextualCounterTarget ||
                contextualCounterTarget.IsDefeated
            )
            {
                return false;
            }

            float distance =
                Vector2.Distance(
                    transform.position,
                    contextualCounterTarget.transform.position
                );

            return distance <= allowedRange;
        }

        private Damageable2D FindNearestCounterTarget(
            float radius,
            out float nearestDistance)
        {
            counterHits.Clear();

            int count = Physics2D.OverlapCircle(
                transform.position,
                radius,
                counterFilter,
                counterHits
            );

            Damageable2D nearest = null;
            float nearestSqr = float.PositiveInfinity;

            for (int i = 0; i < count; i++)
            {
                Damageable2D candidate =
                    counterHits[i].GetComponentInParent<Damageable2D>();

                if (
                    !candidate ||
                    candidate == selfDamageable ||
                    candidate.IsDefeated ||
                    candidate.Faction != CombatFaction.Enemy
                )
                {
                    continue;
                }

                float sqr =
                    (
                        (Vector2)candidate.transform.position -
                        (Vector2)transform.position
                    ).sqrMagnitude;

                if (sqr >= nearestSqr)
                    continue;

                nearest = candidate;
                nearestSqr = sqr;
            }

            nearestDistance =
                nearest
                    ? Mathf.Sqrt(nearestSqr)
                    : float.PositiveInfinity;

            return nearest;
        }

        private Vector2 DirectionTo(Vector2 worldPosition)
        {
            Vector2 delta =
                worldPosition -
                (Vector2)transform.position;

            return delta.sqrMagnitude > 0.0001f
                ? delta.normalized
                : AimDirection();
        }

        private void UpdateMelee()
        {
            if (input.MeleePressed && meleeTimer <= 0f)
                BeginMelee();

            if (meleeTimer <= 0f)
                return;

            if (
                meleeTimer >= 0.150f ||
                meleeTimer <= 0.045f
            )
            {
                return;
            }

            ApplyMeleeHits();
        }

        private void BeginMelee()
        {
            bool grounded = motor && motor.Grounded;
            Vector2 aim = AimDirection();

            if (!grounded)
            {
                bool dive =
                    aim.y < -0.55f &&
                    airMeleeResetTimer <= 0f;

                if (dive)
                {
                    airMeleeStep = 3;
                    meleeStep = 3;
                    meleeDuration = diveMeleeDuration;
                    airMeleeResetTimer = 0.45f;
                }
                else
                {
                    if (airMeleeResetTimer <= 0f)
                        airMeleeStep = 0;

                    airMeleeStep = (airMeleeStep % 2) + 1;
                    meleeStep = airMeleeStep;
                    meleeDuration = airMeleeDuration;
                    airMeleeResetTimer = comboReset;
                }
            }
            else
            {
                if (meleeResetTimer <= 0f)
                    meleeStep = 0;

                meleeStep = (meleeStep % 3) + 1;
                meleeDuration = groundMeleeDuration;
                meleeResetTimer = comboReset;
            }

            meleeTimer = meleeDuration;
            meleeTargets.Clear();

            GameplayEventHub.Raise(new GameplayCue(
                GameplayCueType.MeleeStarted,
                playerId,
                transform.position,
                aim,
                meleeStep
            ));
        }

        private void ApplyMeleeHits()
        {
            Vector2 aim = AimDirection();
            bool grounded = motor && motor.Grounded;

            float reach = grounded ? 0.70f : 0.60f;
            float diameter = grounded
                ? (meleeStep == 3 ? 1.28f : 0.98f)
                : 1.08f;

            Vector2 center =
                (Vector2)transform.position +
                aim * reach;

            meleeHits.Clear();

            int count = Physics2D.OverlapCircle(
                center,
                diameter * 0.5f,
                meleeFilter,
                meleeHits
            );

            for (int i = 0; i < count; i++)
            {
                Damageable2D target =
                    meleeHits[i].GetComponentInParent<Damageable2D>();

                if (!target)
                    continue;

                int instanceId = target.GetInstanceID();

                if (!meleeTargets.Add(instanceId))
                    continue;

                float damage = grounded
                    ? 9f + meleeStep * 4f
                    : 10f + airMeleeStep * 5f;

                Vector2 knockback;

                if (grounded)
                {
                    knockback = new Vector2(
                        motor.Facing * (meleeStep == 3 ? 5.2f : 2.4f),
                        meleeStep == 3 ? 4.2f : 1.2f
                    );
                }
                else
                {
                    bool downwardDive = aim.y < -0.45f;

                    knockback = new Vector2(
                        motor.Facing * (meleeStep == 3 ? 3.9f : 1.8f),
                        downwardDive ? -6.4f : 3.6f
                    );
                }

                bool applied = target.ApplyDamage(new DamagePacket(
                    damage,
                    knockback,
                    meleeHits[i].ClosestPoint(center),
                    CombatFaction.Player,
                    playerId,
                    meleeStep,
                    "melee"
                ));

                if (!applied)
                    continue;

                GameplayEventHub.Raise(new GameplayCue(
                    GameplayCueType.MeleeHit,
                    playerId,
                    target.transform.position,
                    aim,
                    meleeStep,
                    damage,
                    "melee"
                ));
            }
        }

        private Vector2 AimDirection()
        {
            if (motor && motor.AimDirection.sqrMagnitude > 0.0001f)
                return motor.AimDirection.normalized;

            return motor && motor.Facing < 0
                ? Vector2.left
                : Vector2.right;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, deflectRadius);

            Gizmos.color = new Color(1f, 0.75f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, closeCounterRadius);

            Gizmos.color = new Color(1f, 0.35f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, throwRange);

            Gizmos.color = new Color(0.65f, 0.4f, 1f);
            Gizmos.DrawWireSphere(transform.position, grappleRange);
        }
#endif
    }
}
