using NovaStriker.Core;
using System.Collections.Generic;
using NovaStriker.Data;
using NovaStriker.Player;
using NovaStriker.Session;
using UnityEngine;

namespace NovaStriker.Combat
{
    /// <summary>
    /// Shared projectile ownership/collision model for the Unity migration.
    /// Advanced weapon-specific behaviors are layered on later.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class Projectile2D : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Collider2D hitCollider;
        [SerializeField, Min(0.05f)] private float lifetime = 3f;

        private float lifeRemaining;
        private float behaviorElapsed;
        private float behaviorTickTimer;
        private bool initialized;
        private bool boomerangReturning;
        private bool mineArmed;

        private readonly List<Collider2D> behaviorHits = new(24);
        private readonly HashSet<Damageable2D> behaviorTargets = new();
        private ContactFilter2D enemyFilter;

        public CombatFaction Faction { get; private set; }
        public int OwnerPlayerId { get; private set; } = -1;
        public int Tier { get; private set; }
        public float Damage { get; private set; }
        public string WeaponId { get; private set; }
        public WeaponBehavior Behavior { get; private set; }
        public bool Piercing { get; private set; }
        public bool Parryable { get; private set; }
        public bool PerfectOpportunity { get; private set; }

        public Vector2 Velocity => body ? body.linearVelocity : Vector2.zero;

        private void Reset()
        {
            body = GetComponent<Rigidbody2D>();
            hitCollider = GetComponent<Collider2D>();
            if (hitCollider)
                hitCollider.isTrigger = true;
        }

        private void Awake()
        {
            if (!body)
                body = GetComponent<Rigidbody2D>();

            if (!hitCollider)
                hitCollider = GetComponent<Collider2D>();

            lifeRemaining = lifetime;

            enemyFilter = new ContactFilter2D
            {
                useTriggers = true
            };

            int enemyLayer =
                LayerMask.NameToLayer("Enemy");

            if (enemyLayer >= 0)
                enemyFilter.SetLayerMask(1 << enemyLayer);
        }

        public void Initialize(
            int ownerPlayerId,
            CombatFaction faction,
            string weaponId,
            WeaponBehavior behavior,
            Vector2 direction,
            float speed,
            int tier,
            float damage,
            bool piercing = false,
            bool parryable = true,
            bool perfectOpportunity = false)
        {
            OwnerPlayerId = ownerPlayerId;
            Faction = faction;
            SetPhysicsLayerForFaction();
            WeaponId = weaponId;
            Behavior = behavior;
            Tier = Mathf.Clamp(tier, 0, 3);
            Damage = Mathf.Max(0f, damage);
            Piercing = piercing;
            Parryable = parryable;
            PerfectOpportunity = perfectOpportunity;

            Vector2 normalized = direction.sqrMagnitude > 0.0001f
                ? direction.normalized
                : Vector2.right;

            body.linearVelocity = normalized * Mathf.Max(0f, speed);
            lifeRemaining = lifetime;
            behaviorElapsed = 0f;
            behaviorTickTimer = 0f;
            boomerangReturning = false;
            mineArmed = false;
            initialized = true;
        }

        private void FixedUpdate()
        {
            if (!initialized)
                return;

            float dt = Time.fixedDeltaTime;

            lifeRemaining -= dt;
            behaviorElapsed += dt;
            behaviorTickTimer =
                Mathf.Max(0f, behaviorTickTimer - dt);

            UpdateBehavior(dt);

            if (lifeRemaining <= 0f)
                Destroy(gameObject);
        }

        private void UpdateBehavior(float dt)
        {
            switch (Behavior)
            {
                case WeaponBehavior.Boomerang:
                    UpdateBoomerang();
                    break;

                case WeaponBehavior.Gravity:
                    UpdateGravityWell();
                    break;

                case WeaponBehavior.Mine:
                    UpdateMine();
                    break;
            }
        }

        private void UpdateBoomerang()
        {
            if (!body)
                return;

            if (
                !boomerangReturning &&
                behaviorElapsed >= 0.38f
            )
            {
                boomerangReturning = true;
            }

            if (!boomerangReturning)
                return;

            StrikeTeamSession session =
                StrikeTeamSession.Active;

            StrikerPlayerIdentity owner =
                session
                    ? session.GetPlayerByActorId(OwnerPlayerId)
                    : null;

            if (!owner)
            {
                body.linearVelocity =
                    -body.linearVelocity;
                boomerangReturning = false;
                return;
            }

            Vector2 delta =
                (Vector2)owner.transform.position -
                body.position;

            if (delta.sqrMagnitude <= 0.20f * 0.20f)
            {
                Destroy(gameObject);
                return;
            }

            float speed =
                Mathf.Max(
                    0.1f,
                    body.linearVelocity.magnitude
                );

            Vector2 desired =
                delta.normalized * speed;

            body.linearVelocity =
                Vector2.Lerp(
                    body.linearVelocity,
                    desired,
                    0.24f
                );
        }

        private void UpdateGravityWell()
        {
            if (!body)
                return;

            if (behaviorElapsed < 0.28f)
            {
                body.linearVelocity *= 0.92f;
                return;
            }

            body.linearVelocity = Vector2.zero;

            if (behaviorTickTimer > 0f)
                return;

            behaviorTickTimer = 0.12f;

            float radius =
                Mathf.Max(
                    0.45f,
                    transform.lossyScale.x * 1.55f
                );

            behaviorHits.Clear();

            int count = Physics2D.OverlapCircle(
                transform.position,
                radius,
                enemyFilter,
                behaviorHits
            );

            for (int i = 0; i < count; i++)
            {
                Damageable2D target =
                    behaviorHits[i].GetComponentInParent<Damageable2D>();

                if (
                    !target ||
                    target.Faction != CombatFaction.Enemy ||
                    target.IsDefeated
                )
                {
                    continue;
                }

                Vector2 delta =
                    (Vector2)transform.position -
                    (Vector2)target.transform.position;

                if (delta.sqrMagnitude <= 0.001f)
                    continue;

                target.AddExternalVelocity(
                    delta.normalized *
                    (2.0f + Tier * 0.75f)
                );

                target
                    .GetComponent<CombatState2D>()
                    ?.ApplyVulnerable(0.35f);
            }
        }

        private void UpdateMine()
        {
            if (!body)
                return;

            if (!mineArmed && behaviorElapsed >= 0.22f)
            {
                mineArmed = true;
                body.linearVelocity = Vector2.zero;
            }
        }

        private void ExplodeMine()
        {
            float radius =
                Mathf.Max(
                    0.55f,
                    transform.lossyScale.x * 1.65f
                );

            behaviorHits.Clear();

            int count = Physics2D.OverlapCircle(
                transform.position,
                radius,
                enemyFilter,
                behaviorHits
            );

            behaviorTargets.Clear();

            for (int i = 0; i < count; i++)
            {
                Damageable2D target =
                    behaviorHits[i].GetComponentInParent<Damageable2D>();

                if (
                    !target ||
                    target.Faction != CombatFaction.Enemy ||
                    !behaviorTargets.Add(target)
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
                        Damage,
                        direction * (2.5f + Tier),
                        transform.position,
                        Faction,
                        OwnerPlayerId,
                        Tier,
                        WeaponId
                    )
                );

                target
                    .GetComponent<CombatState2D>()
                    ?.ApplyWeaponStatus(
                        WeaponId,
                        Tier
                    );
            }

            Destroy(gameObject);
        }

        public bool TryCancel(
            CombatFaction cancellingFaction,
            int actorId,
            string sourceId)
        {
            if (!initialized)
                return false;

            if (
                cancellingFaction != CombatFaction.Neutral &&
                Faction == cancellingFaction
            )
            {
                return false;
            }

            GameplayEventHub.Raise(
                new GameplayCue(
                    GameplayCueType.ProjectileCancelled,
                    actorId,
                    transform.position,
                    Velocity.sqrMagnitude > 0.0001f
                        ? Velocity.normalized
                        : Vector2.zero,
                    Tier,
                    Damage,
                    sourceId
                )
            );

            Destroy(gameObject);
            return true;
        }

        public bool TryEnemyReflect(
            float speedMultiplier = 0.75f,
            float damageMultiplier = 1f)
        {
            if (
                !initialized ||
                Faction != CombatFaction.Player
            )
            {
                return false;
            }

            Vector2 reverse =
                body &&
                body.linearVelocity.sqrMagnitude > 0.0001f
                    ? -body.linearVelocity.normalized
                    : Vector2.left;

            float speed =
                body
                    ? Mathf.Max(
                        0.1f,
                        body.linearVelocity.magnitude *
                        Mathf.Max(0.1f, speedMultiplier)
                    )
                    : 1f;

            Faction = CombatFaction.Enemy;
            OwnerPlayerId = -1;
            Damage *= Mathf.Max(0f, damageMultiplier);
            SetPhysicsLayerForFaction();

            if (body)
                body.linearVelocity = reverse * speed;

            return true;
        }

        /// <summary>
        /// Reflect an enemy projectile using the timing/damage behavior from the
        /// browser reference. A perfect opportunity plus a perfect parry receives
        /// the highest reflected damage.
        /// </summary>
        public bool TryParry(int newOwnerPlayerId, bool perfect)
        {
            if (
                Faction != CombatFaction.Enemy ||
                !Parryable
            )
            {
                return false;
            }

            Vector2 reverse = body.linearVelocity.sqrMagnitude > 0.0001f
                ? -body.linearVelocity.normalized
                : Vector2.right;

            float speed = Mathf.Max(0.01f, body.linearVelocity.magnitude) * 1.25f;
            bool bonus = PerfectOpportunity && perfect;

            Faction = CombatFaction.Player;
            SetPhysicsLayerForFaction();
            OwnerPlayerId = newOwnerPlayerId;
            Tier = (bonus || perfect) ? 3 : 2;
            Damage = bonus ? 48f : perfect ? 34f : 20f;
            PerfectOpportunity = false;

            body.linearVelocity = reverse * speed;
            return true;
        }

        private void SetPhysicsLayerForFaction()
        {
            string layerName = Faction == CombatFaction.Player
                ? "PlayerProjectile"
                : Faction == CombatFaction.Enemy
                    ? "EnemyProjectile"
                    : "Default";

            int layer = LayerMask.NameToLayer(layerName);

            if (layer >= 0)
                gameObject.layer = layer;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!initialized)
                return;

            if (IsWorldCollider(other))
            {
                HandleWorldCollision();
                return;
            }

            Projectile2D otherProjectile = other.GetComponentInParent<Projectile2D>();

            if (otherProjectile && otherProjectile != this)
            {
                HandleProjectileCollision(otherProjectile);
                return;
            }

            Damageable2D damageable = other.GetComponentInParent<Damageable2D>();

            if (!damageable)
                return;

            if (
                damageable.Faction != CombatFaction.Neutral &&
                damageable.Faction == Faction
            )
            {
                return;
            }

            Vector2 direction = body.linearVelocity.sqrMagnitude > 0.0001f
                ? body.linearVelocity.normalized
                : Vector2.zero;

            float knockbackScale = Tier >= 2 ? (1.4f + Tier * 0.6f) : 0f;
            Vector2 knockback = new(
                direction.x * knockbackScale,
                Tier >= 2 ? 0.6f * Tier : 0f
            );

            if (
                Behavior == WeaponBehavior.Mine &&
                mineArmed &&
                Faction == CombatFaction.Player
            )
            {
                ExplodeMine();
                return;
            }

            bool applied = damageable.ApplyDamage(new DamagePacket(
                Damage,
                knockback,
                transform.position,
                Faction,
                OwnerPlayerId,
                Tier,
                WeaponId
            ));

            if (applied)
            {
                CombatState2D combatState =
                    damageable.GetComponent<CombatState2D>();

                combatState?.ApplyWeaponStatus(
                    WeaponId,
                    Tier
                );
            }

            if (
                applied &&
                !Piercing &&
                Behavior != WeaponBehavior.Boomerang &&
                Behavior != WeaponBehavior.Gravity
            )
            {
                Destroy(gameObject);
            }
        }

        private bool IsWorldCollider(Collider2D other)
        {
            if (!other)
                return false;

            int worldLayer =
                LayerMask.NameToLayer("World");

            int oneWayLayer =
                LayerMask.NameToLayer("OneWay");

            return
                other.gameObject.layer == worldLayer ||
                other.gameObject.layer == oneWayLayer;
        }

        private void HandleWorldCollision()
        {
            switch (Behavior)
            {
                case WeaponBehavior.Gravity:
                    if (body)
                        body.linearVelocity = Vector2.zero;
                    break;

                case WeaponBehavior.Mine:
                    if (body)
                        body.linearVelocity = Vector2.zero;
                    mineArmed = true;
                    break;

                case WeaponBehavior.Boomerang:
                    boomerangReturning = true;
                    break;

                default:
                    Destroy(gameObject);
                    break;
            }
        }

        private void HandleProjectileCollision(Projectile2D other)
        {
            // Browser reference behavior: a friendly charge tier 1+ deletes a
            // hostile projectile and continues travelling.
            if (
                Faction == CombatFaction.Player &&
                Tier >= 1 &&
                other.Faction == CombatFaction.Enemy
            )
            {
                GameplayEventHub.Raise(new GameplayCue(
                    GameplayCueType.ProjectileCancelled,
                    OwnerPlayerId,
                    other.transform.position,
                    Velocity.normalized,
                    Tier,
                    other.Damage,
                    WeaponId
                ));

                Destroy(other.gameObject);
            }
        }
    }
}
