using NovaStriker.Data;
using UnityEngine;

namespace NovaStriker.Combat
{
    /// <summary>
    /// Temporary greybox test harness for parry timing and projectile
    /// cancellation. It is not enemy AI and should not ship in production.
    /// </summary>
    public sealed class GreyboxHostileProjectileEmitter : MonoBehaviour
    {
        [SerializeField] private Projectile2D projectilePrefab;
        [SerializeField] private Transform target;
        [SerializeField, Min(0.1f)] private float interval = 1.4f;
        [SerializeField, Min(0f)] private float projectileSpeed = 6.6f;
        [SerializeField, Min(0f)] private float damage = 11f;
        [SerializeField, Min(0)] private int perfectOpportunityEvery = 4;

        private float timer;
        private int fired;

        private void OnEnable()
        {
            timer = interval;
        }

        private void FixedUpdate()
        {
            if (!projectilePrefab || !target)
                return;

            timer -= Time.fixedDeltaTime;

            if (timer > 0f)
                return;

            timer += interval;
            Fire();
        }

        private void Fire()
        {
            Vector2 direction =
                ((Vector2)target.position - (Vector2)transform.position)
                .normalized;

            fired++;

            bool perfectOpportunity =
                perfectOpportunityEvery > 0 &&
                fired % perfectOpportunityEvery == 0;

            Projectile2D shot = Instantiate(
                projectilePrefab,
                transform.position,
                Quaternion.identity
            );

            shot.Initialize(
                -1,
                CombatFaction.Enemy,
                "greybox-hostile",
                WeaponBehavior.Standard,
                direction,
                projectileSpeed,
                0,
                damage,
                false,
                true,
                perfectOpportunity
            );
        }
    }
}
