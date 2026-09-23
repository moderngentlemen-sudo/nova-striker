using NovaStriker.Combat;
using NovaStriker.Data;
using UnityEngine;

namespace NovaStriker.Enemies
{
    /// <summary>
    /// Aggressive lateral role that closes quickly, periodically commits to a
    /// short reposition burst, and attacks from close-to-mid range.
    /// </summary>
    public sealed class EnemyFlankerModule2D : EnemyRoleModule2D
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 4.4f;
        [SerializeField] private float acceleration = 26f;
        [SerializeField] private float closeRange = 1.45f;
        [SerializeField] private float preferredRange = 2.7f;

        [Header("Reposition Burst")]
        [SerializeField] private float burstTriggerRange = 5.5f;
        [SerializeField] private float burstSpeed = 8.8f;
        [SerializeField] private float burstDuration = 0.22f;
        [SerializeField] private float burstCooldown = 1.65f;

        [Header("Attack")]
        [SerializeField] private Projectile2D projectilePrefab;
        [SerializeField] private Transform muzzle;
        [SerializeField] private float fireRange = 4.2f;
        [SerializeField] private float fireInterval = 0.95f;
        [SerializeField] private float projectileSpeed = 9.2f;
        [SerializeField] private float projectileDamage = 8f;
        [SerializeField] private bool parryable = true;

        private float burstTimer;
        private float burstCooldownTimer;
        private float fireTimer;
        private float burstDirection = 1f;

        public override EnemyRole Role => EnemyRole.Flanker;

        public override void OnRoleEnter(EnemyBrain2D brain)
        {
            burstTimer = 0f;
            burstCooldownTimer = 0.55f;
            fireTimer = 0.4f;
        }

        public override void TickIdle(EnemyBrain2D brain, float dt)
        {
            burstTimer = 0f;
            burstCooldownTimer =
                Mathf.Max(0f, burstCooldownTimer - dt);
            fireTimer =
                Mathf.Max(0f, fireTimer - dt);

            brain.StopHorizontal(acceleration);
        }

        public override void TickEngage(EnemyBrain2D brain, float dt)
        {
            burstCooldownTimer =
                Mathf.Max(0f, burstCooldownTimer - dt);
            fireTimer =
                Mathf.Max(0f, fireTimer - dt);

            if (burstTimer > 0f)
            {
                burstTimer =
                    Mathf.Max(0f, burstTimer - dt);

                brain.MoveHorizontal(
                    burstDirection * burstSpeed,
                    acceleration * 4f,
                    dt
                );

                return;
            }

            float distance = brain.TacticalTargetDistance;
            float targetDistance = brain.TargetDistance;
            Vector2 direction = brain.DirectionToTacticalTarget;
            float toward =
                SafeHorizontalSign(direction.x);

            if (
                burstCooldownTimer <= 0f &&
                distance <= burstTriggerRange &&
                distance > closeRange + 0.35f &&
                brain.HasClearLineToTarget()
            )
            {
                burstDirection = toward;
                burstTimer = burstDuration;
                burstCooldownTimer = burstCooldown;

                brain.MoveHorizontal(
                    burstDirection * burstSpeed,
                    acceleration * 4f,
                    dt
                );

                return;
            }

            if (distance < closeRange)
            {
                brain.MoveHorizontal(
                    -toward * moveSpeed,
                    acceleration,
                    dt
                );
            }
            else if (distance > preferredRange)
            {
                brain.MoveHorizontal(
                    toward * moveSpeed,
                    acceleration,
                    dt
                );
            }
            else
            {
                brain.StopHorizontal(acceleration);
            }

            if (
                fireTimer <= 0f &&
                targetDistance <= fireRange &&
                brain.HasClearLineToTarget()
            )
            {
                Fire(brain);
                fireTimer = fireInterval;
            }
        }

        private void Fire(EnemyBrain2D brain)
        {
            if (!projectilePrefab || !brain.Target)
                return;

            Vector3 origin =
                muzzle
                    ? muzzle.position
                    : transform.position + Vector3.up * 0.10f;

            Vector2 direction =
                ((Vector2)brain.Target.position -
                 (Vector2)origin).normalized;

            Projectile2D projectile = ProjectilePool2D.Spawn(
                projectilePrefab,
                origin,
                Quaternion.identity
            );

            projectile.Initialize(
                -1,
                CombatFaction.Enemy,
                "enemy-flanker-pulse",
                WeaponBehavior.Standard,
                direction,
                projectileSpeed,
                0,
                projectileDamage,
                false,
                parryable,
                false
            );
        }

        private static float SafeHorizontalSign(float value)
        {
            if (Mathf.Abs(value) < 0.001f)
                return 1f;

            return Mathf.Sign(value);
        }
    }
}
