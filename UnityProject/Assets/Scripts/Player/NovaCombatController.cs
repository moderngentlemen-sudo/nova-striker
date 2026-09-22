using System.Collections.Generic;
using NovaStriker.Combat;
using NovaStriker.Core;
using NovaStriker.Data;
using NovaStriker.Input;
using UnityEngine;

namespace NovaStriker.Player
{
    /// <summary>
    /// Greybox combat controller for Nova/Echo.
    /// Keeps timing, hit windows, projectile ownership, and damage in gameplay
    /// while emitting presentation cues for animation/VFX/audio/camera.
    /// </summary>
    public sealed class NovaCombatController : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private int playerId;

        [Header("References")]
        [SerializeField] private NovaMotor2D motor;
        [SerializeField] private Transform muzzleSocket;
        [SerializeField] private Projectile2D projectilePrefab;
        [SerializeField] private WeaponDefinition equippedWeapon;

        [Header("Collision")]
        [SerializeField] private LayerMask damageableMask;
        [SerializeField] private LayerMask projectileMask;

        [Header("Parry")]
        [SerializeField] private float parryRadius = 0.72f;
        [SerializeField] private ParryWindow parryWindow;

        [Header("Melee")]
        [SerializeField] private float groundMeleeDuration = 0.20f;
        [SerializeField] private float airMeleeDuration = 0.18f;
        [SerializeField] private float diveMeleeDuration = 0.22f;
        [SerializeField] private float comboReset = 0.38f;

        private readonly List<Collider2D> parryHits = new(24);
        private readonly List<Collider2D> meleeHits = new(24);
        private ContactFilter2D parryFilter;
        private ContactFilter2D meleeFilter;
        private readonly HashSet<int> meleeTargets = new();

        private PlayerInputState input;

        private float fireCharge;

        private float parryElapsed = -1f;

        private float meleeTimer;
        private float meleeDuration;
        private float meleeResetTimer;
        private int meleeStep;

        private float airMeleeResetTimer;
        private int airMeleeStep;

        public int PlayerId => playerId;
        public float FireCharge => fireCharge;
        public bool IsParrying => parryElapsed >= 0f;
        public bool IsParryActive => IsParrying && parryWindow.IsActive(parryElapsed);
        public bool IsPerfectParryWindow => IsParrying && parryWindow.IsPerfect(parryElapsed);
        public bool IsMeleeActive => meleeTimer > 0f;
        public int MeleeStep => meleeStep;
        public WeaponDefinition EquippedWeapon => equippedWeapon;

        private void Reset()
        {
            motor = GetComponent<NovaMotor2D>();
            parryWindow = ParryWindow.Default;
        }

        private void Awake()
        {
            if (!motor)
                motor = GetComponent<NovaMotor2D>();

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
        }

        public void SetInput(PlayerInputState state)
        {
            input = state;
        }

        public void SetWeapon(WeaponDefinition weapon)
        {
            equippedWeapon = weapon;
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            UpdateTimers(dt);
            UpdateFire(dt);
            UpdateParry();
            UpdateMelee();
        }

        private void UpdateTimers(float dt)
        {
            if (parryElapsed >= 0f)
            {
                parryElapsed += dt;

                if (parryElapsed > parryWindow.TotalDuration)
                    parryElapsed = -1f;
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

        private void UpdateParry()
        {
            if (
                input.ParryPressed &&
                parryElapsed < 0f
            )
            {
                parryElapsed = 0f;

                GameplayEventHub.Raise(new GameplayCue(
                    GameplayCueType.ParryStarted,
                    playerId,
                    transform.position,
                    AimDirection()
                ));
            }

            if (!IsParryActive)
                return;

            parryHits.Clear();

            int count = Physics2D.OverlapCircle(
                transform.position,
                parryRadius,
                parryFilter,
                parryHits
            );

            bool perfect = IsPerfectParryWindow;

            for (int i = 0; i < count; i++)
            {
                Projectile2D projectile =
                    parryHits[i].GetComponentInParent<Projectile2D>();

                if (!projectile)
                    continue;

                bool bonusOpportunity = projectile.PerfectOpportunity;

                if (!projectile.TryParry(playerId, perfect))
                    continue;

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

        private void UpdateMelee()
        {
            if (input.MeleePressed && meleeTimer <= 0f)
                BeginMelee();

            if (meleeTimer <= 0f)
                return;

            // Browser reference active hit window:
            // meleeTimer < 0.150s && meleeTimer > 0.045s.
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
            Gizmos.DrawWireSphere(transform.position, parryRadius);
        }
#endif
    }
}
