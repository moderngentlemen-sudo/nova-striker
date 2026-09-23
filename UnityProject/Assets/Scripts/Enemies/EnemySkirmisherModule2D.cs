using NovaStriker.Combat;
using NovaStriker.Data;
using UnityEngine;

namespace NovaStriker.Enemies
{
    /// <summary>
    /// First representative standard-enemy role: keeps a mid-range spacing
    /// band, pursues/retreats on the ground, and fires readable pulse shots.
    /// </summary>
    public sealed class EnemySkirmisherModule2D : EnemyRoleModule2D
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 3.4f;
        [SerializeField] private float acceleration = 18f;
        [SerializeField] private float preferredMinRange = 2.6f;
        [SerializeField] private float preferredMaxRange = 4.8f;

        [Header("Attack")]
        [SerializeField] private Projectile2D projectilePrefab;
        [SerializeField] private Transform muzzle;
        [SerializeField] private float fireRange = 8.5f;
        [SerializeField] private float fireInterval = 1.15f;
        [SerializeField] private float projectileSpeed = 6.8f;
        [SerializeField] private float projectileDamage = 10f;
        [SerializeField] private bool parryable = true;

        private float fireTimer;

        public override EnemyRole Role => EnemyRole.Skirmisher;

        public override void OnRoleEnter(EnemyBrain2D brain)
        {
            fireTimer = 0.35f;
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

            if (distance > preferredMaxRange)
            {
                brain.MoveHorizontal(
                    Mathf.Sign(direction.x) * moveSpeed,
                    acceleration,
                    dt
                );
            }
            else if (distance < preferredMinRange)
            {
                brain.MoveHorizontal(
                    -Mathf.Sign(direction.x) * moveSpeed * 0.82f,
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

            Fire(brain);
            fireTimer = fireInterval;
        }

        private void Fire(EnemyBrain2D brain)
        {
            if (!projectilePrefab || !brain.Target)
                return;

            Vector3 origin =
                muzzle
                    ? muzzle.position
                    : transform.position + Vector3.up * 0.15f;

            Vector2 direction =
                ((Vector2)brain.Target.position - (Vector2)origin).normalized;

            Projectile2D projectile = Instantiate(
                projectilePrefab,
                origin,
                Quaternion.identity
            );

            projectile.Initialize(
                -1,
                CombatFaction.Enemy,
                "enemy-skirmisher-pulse",
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
