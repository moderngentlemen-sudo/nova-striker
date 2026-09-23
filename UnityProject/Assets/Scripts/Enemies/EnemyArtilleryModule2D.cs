using NovaStriker.Combat;
using NovaStriker.Data;
using UnityEngine;

namespace NovaStriker.Enemies
{
    /// <summary>
    /// Long-range role that prefers a distant firing band and launches
    /// readable, parryable salvos only with clear line of sight.
    /// </summary>
    public sealed class EnemyArtilleryModule2D : EnemyRoleModule2D
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 2.0f;
        [SerializeField] private float acceleration = 14f;
        [SerializeField] private float preferredMinRange = 5.25f;
        [SerializeField] private float preferredMaxRange = 8.5f;

        [Header("Salvo")]
        [SerializeField] private Projectile2D projectilePrefab;
        [SerializeField] private Transform muzzle;
        [SerializeField] private float fireRange = 12f;
        [SerializeField] private float fireInterval = 1.85f;
        [SerializeField, Range(1, 5)] private int salvoCount = 3;
        [SerializeField] private float spreadDegrees = 7f;
        [SerializeField] private float projectileSpeed = 5.8f;
        [SerializeField] private float projectileDamage = 13f;
        [SerializeField] private bool parryable = true;

        private float fireTimer;
        private EnemyArchetypeController2D archetypeController;

        public override EnemyRole Role => EnemyRole.Artillery;

        private void Awake()
        {
            archetypeController =
                GetComponent<EnemyArchetypeController2D>();
        }

        public override void OnRoleEnter(EnemyBrain2D brain)
        {
            fireTimer = 0.65f;
        }

        public override void TickIdle(EnemyBrain2D brain, float dt)
        {
            brain.StopHorizontal(acceleration);
            fireTimer = Mathf.Max(0f, fireTimer - dt);
        }

        public override void TickEngage(EnemyBrain2D brain, float dt)
        {
            float distance = brain.TacticalTargetDistance;
            float targetDistance = brain.TargetDistance;
            Vector2 direction = brain.DirectionToTacticalTarget;

            if (distance < preferredMinRange)
            {
                brain.MoveHorizontal(
                    -SafeHorizontalSign(direction.x) *
                    moveSpeed,
                    acceleration,
                    dt
                );
            }
            else if (distance > preferredMaxRange)
            {
                brain.MoveHorizontal(
                    SafeHorizontalSign(direction.x) *
                    moveSpeed * 0.72f,
                    acceleration,
                    dt
                );
            }
            else
            {
                brain.StopHorizontal(acceleration);
            }

            fireTimer = Mathf.Max(0f, fireTimer - dt);

            if (
                fireTimer > 0f ||
                targetDistance > fireRange ||
                !brain.HasClearLineToTarget()
            )
            {
                return;
            }

            FireSalvo(brain);
            fireTimer = EffectiveFireInterval();
        }

        private void FireSalvo(EnemyBrain2D brain)
        {
            if (!projectilePrefab || !brain.Target)
                return;

            Vector3 origin =
                muzzle
                    ? muzzle.position
                    : transform.position + Vector3.up * 0.28f;

            Vector2 baseDirection =
                ((Vector2)brain.Target.position -
                 (Vector2)origin).normalized;

            float baseAngle =
                Mathf.Atan2(
                    baseDirection.y,
                    baseDirection.x
                ) * Mathf.Rad2Deg;

            if (!archetypeController)
            {
                archetypeController =
                    GetComponent<EnemyArchetypeController2D>();
            }

            bool sniper =
                archetypeController &&
                archetypeController.Archetype ==
                EnemyArchetype.Sniper;

            bool turret =
                archetypeController &&
                archetypeController.Archetype ==
                EnemyArchetype.Turret;

            int count =
                sniper || turret
                    ? 1
                    : Mathf.Max(1, salvoCount);

            float effectiveSpeed =
                sniper
                    ? projectileSpeed * 1.35f
                    : projectileSpeed;

            float effectiveDamage =
                sniper
                    ? projectileDamage * 1.15f
                    : projectileDamage;

            bool perfectOpportunity =
                sniper ||
                (
                    archetypeController &&
                    archetypeController.Profile.PerfectOpportunityShots
                );

            string weaponId =
                sniper
                    ? "enemy-sniper-perfect"
                    : turret
                        ? "enemy-turret-shot"
                        : "enemy-artillery-salvo";

            for (int i = 0; i < count; i++)
            {
                float t =
                    count <= 1
                        ? 0.5f
                        : (float)i / (count - 1);

                float offset =
                    Mathf.Lerp(
                        -spreadDegrees,
                        spreadDegrees,
                        t
                    );

                float angle =
                    (baseAngle + offset) *
                    Mathf.Deg2Rad;

                Vector2 direction = new(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle)
                );

                Projectile2D projectile = ProjectilePool2D.Spawn(
                    projectilePrefab,
                    origin,
                    Quaternion.identity
                );

                projectile.Initialize(
                    -1,
                    CombatFaction.Enemy,
                    weaponId,
                    WeaponBehavior.Standard,
                    direction,
                    effectiveSpeed,
                    0,
                    effectiveDamage,
                    false,
                    parryable,
                    perfectOpportunity
                );
            }
        }

        private float EffectiveFireInterval()
        {
            if (!archetypeController)
            {
                archetypeController =
                    GetComponent<EnemyArchetypeController2D>();
            }

            if (!archetypeController)
                return fireInterval;

            EnemyArchetype archetype =
                archetypeController.Archetype;

            if (
                archetype == EnemyArchetype.Sniper ||
                archetype == EnemyArchetype.Turret
            )
            {
                return Mathf.Max(
                    0.25f,
                    archetypeController.Profile.FireInterval
                );
            }

            return fireInterval;
        }

        private static float SafeHorizontalSign(float value)
        {
            if (Mathf.Abs(value) < 0.001f)
                return 1f;

            return Mathf.Sign(value);
        }
    }
}
