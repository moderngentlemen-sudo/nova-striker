using NovaStriker.Combat;
using NovaStriker.Data;
using UnityEngine;

namespace NovaStriker.Enemies
{
    public enum AerialMotionPattern
    {
        HoverBob = 0,
        Orbit = 1
    }

    /// <summary>
    /// Shared aerial role derived from the preserved browser roles for Drone
    /// and Orbiter. It maintains an offset above the player while adding either
    /// vertical hover motion or a circular orbit bias.
    /// </summary>
    public sealed class EnemyAerialModule2D : EnemyRoleModule2D
    {
        [Header("Movement")]
        [SerializeField] private AerialMotionPattern motionPattern =
            AerialMotionPattern.HoverBob;
        [SerializeField] private float horizontalOffset = 2.35f;
        [SerializeField] private float verticalOffset = 2.7f;
        [SerializeField] private float horizontalSpeed = 3.25f;
        [SerializeField] private float verticalSpeed = 2.75f;
        [SerializeField] private float acceleration = 12f;

        [Header("Pattern")]
        [SerializeField] private float phaseSpeed = 1.5f;
        [SerializeField] private float bobAmplitude = 0.55f;
        [SerializeField] private float orbitRadiusX = 1.25f;
        [SerializeField] private float orbitRadiusY = 0.75f;

        [Header("Attack")]
        [SerializeField] private Projectile2D projectilePrefab;
        [SerializeField] private Transform muzzle;
        [SerializeField] private float fireRange = 8.5f;
        [SerializeField] private float fireInterval = 2.2f;
        [SerializeField] private float projectileSpeed = 6.8f;
        [SerializeField] private float projectileDamage = 11f;
        [SerializeField] private bool parryable = true;

        private float phase;
        private float fireTimer;
        private float flankSide = 1f;
        private float previousGravityScale;

        public override EnemyRole Role => EnemyRole.Aerial;

        public override void OnRoleEnter(EnemyBrain2D brain)
        {
            phase = Random.Range(0f, Mathf.PI * 2f);
            fireTimer = 0.75f;
            flankSide = Random.value < 0.5f ? -1f : 1f;

            if (brain.Body)
            {
                previousGravityScale =
                    brain.Body.gravityScale;

                brain.Body.gravityScale = 0f;
            }
        }

        public override void OnRoleExit(EnemyBrain2D brain)
        {
            if (brain.Body)
                brain.Body.gravityScale = previousGravityScale;
        }

        public override void TickIdle(EnemyBrain2D brain, float dt)
        {
            phase += dt * phaseSpeed;
            fireTimer = Mathf.Max(0f, fireTimer - dt);

            Vector2 idleVelocity =
                motionPattern == AerialMotionPattern.Orbit
                    ? new Vector2(
                        Mathf.Cos(phase) * horizontalSpeed * 0.25f,
                        Mathf.Sin(phase) * verticalSpeed * 0.25f
                    )
                    : new Vector2(
                        0f,
                        Mathf.Sin(phase) * verticalSpeed * 0.18f
                    );

            brain.MoveVelocity(
                idleVelocity,
                acceleration,
                dt
            );
        }

        public override void TickEngage(EnemyBrain2D brain, float dt)
        {
            if (!brain.Target)
                return;

            phase += dt * phaseSpeed;
            fireTimer = Mathf.Max(0f, fireTimer - dt);

            Vector2 target =
                brain.TacticalTargetPosition +
                new Vector2(
                    flankSide * horizontalOffset,
                    verticalOffset
                );

            if (motionPattern == AerialMotionPattern.Orbit)
            {
                target += new Vector2(
                    Mathf.Cos(phase) * orbitRadiusX,
                    Mathf.Sin(phase) * orbitRadiusY
                );
            }
            else
            {
                target += Vector2.up *
                    (Mathf.Sin(phase) * bobAmplitude);
            }

            Vector2 delta =
                target - (Vector2)transform.position;

            Vector2 desiredVelocity = new(
                Mathf.Clamp(
                    delta.x * 2.4f,
                    -horizontalSpeed,
                    horizontalSpeed
                ),
                Mathf.Clamp(
                    delta.y * 2.4f,
                    -verticalSpeed,
                    verticalSpeed
                )
            );

            brain.MoveVelocity(
                desiredVelocity,
                acceleration,
                dt
            );

            if (
                Mathf.Abs(
                    brain.TargetPosition.x -
                    transform.position.x
                ) < 0.7f
            )
            {
                flankSide *= -1f;
            }

            if (
                fireTimer <= 0f &&
                brain.TargetDistance <= fireRange &&
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
                    : transform.position;

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
                motionPattern == AerialMotionPattern.Orbit
                    ? "enemy-orbiter-pulse"
                    : "enemy-drone-pulse",
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
    }
}
