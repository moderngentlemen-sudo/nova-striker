using System.Collections.Generic;
using NovaStriker.Combat;
using NovaStriker.Player;
using UnityEngine;

namespace NovaStriker.Enemies
{
    /// <summary>
    /// Adds browser-reference archetype identity and reactions on top of the
    /// shared role modules. It intentionally handles only behaviors the
    /// preserved reference actually establishes.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyArchetypeController2D : MonoBehaviour
    {
        [SerializeField] private EnemyArchetype archetype;
        [SerializeField] private EnemyBrain2D brain;
        [SerializeField] private Damageable2D damageable;
        [SerializeField] private CombatState2D combatState;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private LayerMask playerProjectileMask;

        [Header("Reaction Scan")]
        [SerializeField] private float reactionRadius = 3.2f;
        [SerializeField] private float reactionScanInterval = 0.05f;
        [SerializeField] private float dodgeSpeed = 6.6f;
        [SerializeField] private float dodgeDuration = 0.18f;

        [Header("Guard Counter")]
        [SerializeField] private float guardCounterRange = 1.9f;
        [SerializeField] private float guardCounterDamage = 16f;
        [SerializeField] private float guardCounterCooldown = 1.8f;

        private readonly List<Collider2D> projectileHits = new(24);
        private ContactFilter2D projectileFilter;

        private EnemyArchetypeReference profile;
        private float reactionScanTimer;
        private float reactionCooldown;
        private float guardCounterTimer;
        private float contactCooldown;

        public EnemyArchetype Archetype => archetype;
        public EnemyArchetypeReference Profile => profile;

        private void Reset()
        {
            brain = GetComponent<EnemyBrain2D>();
            damageable = GetComponent<Damageable2D>();
            combatState = GetComponent<CombatState2D>();
            body = GetComponent<Rigidbody2D>();
        }

        private void Awake()
        {
            if (!brain)
                brain = GetComponent<EnemyBrain2D>();

            if (!damageable)
                damageable = GetComponent<Damageable2D>();

            if (!combatState)
                combatState = GetComponent<CombatState2D>();

            if (!body)
                body = GetComponent<Rigidbody2D>();

            profile =
                EnemyArchetypeCatalog.Get(archetype);

            ApplyReferenceProfile();

            projectileFilter = new ContactFilter2D
            {
                useTriggers = true
            };
            projectileFilter.SetLayerMask(playerProjectileMask);
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            reactionCooldown =
                Mathf.Max(0f, reactionCooldown - dt);
            guardCounterTimer =
                Mathf.Max(0f, guardCounterTimer - dt);
            contactCooldown =
                Mathf.Max(0f, contactCooldown - dt);

            reactionScanTimer =
                Mathf.Max(0f, reactionScanTimer - dt);

            if (
                !damageable ||
                damageable.IsDefeated ||
                !brain
            )
            {
                return;
            }

            if (archetype == EnemyArchetype.Guard)
                UpdateGuardMeleeCounter();

            if (
                reactionCooldown > 0f ||
                reactionScanTimer > 0f
            )
            {
                return;
            }

            reactionScanTimer = reactionScanInterval;
            EvaluateProjectileReaction();
        }

        public void ConfigureArchetype(
            EnemyArchetype value)
        {
            archetype = value;
            profile =
                EnemyArchetypeCatalog.Get(archetype);

            ApplyReferenceProfile();
        }

        private void ApplyReferenceProfile()
        {
            if (brain)
                brain.SetRole(profile.Role);

            damageable?.ConfigureMaxHealth(
                profile.MaxHealth,
                true
            );

            combatState?.ConfigureDefense(
                profile.ShieldHp,
                profile.ArmorHp,
                Mathf.Max(
                    45f,
                    profile.MaxHealth * 0.72f
                ),
                archetype == EnemyArchetype.Heavy
                    ? 0.32f
                    : 0.24f,
                true
            );
        }

        private void EvaluateProjectileReaction()
        {
            if (
                playerProjectileMask.value == 0 ||
                !combatState
            )
            {
                return;
            }

            Projectile2D threat =
                FindIncomingProjectile();

            if (!threat)
                return;

            bool front =
                ProjectileApproachesFront(threat);

            if (
                archetype == EnemyArchetype.Shield &&
                front &&
                combatState.Shield > 0f
            )
            {
                float shieldDamage =
                    threat.Damage *
                    (threat.Piercing ? 1.5f : 1f);

                combatState.DamageShield(
                    shieldDamage
                );

                // Browser shield units brace and recover a small amount.
                combatState.RestoreShield(4f);

                if (!threat.Piercing)
                {
                    threat.TryCancel(
                        CombatFaction.Enemy,
                        damageable.ActorId,
                        "enemy-shield-brace"
                    );
                }

                reactionCooldown = 0.45f;
                return;
            }

            if (
                archetype == EnemyArchetype.Guard &&
                front &&
                combatState.Shield > 0f &&
                threat.Tier <= 1
            )
            {
                if (threat.TryEnemyReflect(0.75f, 1f))
                {
                    combatState.DamageShield(
                        threat.Damage * 0.25f
                    );

                    reactionCooldown = 1.0f;
                }

                return;
            }

            if (
                profile.DodgesChargedShots &&
                threat.Tier >= 2
            )
            {
                Vector2 velocity =
                    threat.Velocity;

                float dodgeDirection =
                    Mathf.Abs(velocity.x) > 0.05f
                        ? -Mathf.Sign(velocity.x)
                        : (brain.Facing > 0 ? -1f : 1f);

                brain.BeginReactionOverride(
                    new Vector2(
                        dodgeDirection * dodgeSpeed,
                        body ? body.linearVelocity.y : 0f
                    ),
                    dodgeDuration
                );

                reactionCooldown =
                    Random.Range(0.70f, 1.20f);
            }
        }

        private Projectile2D FindIncomingProjectile()
        {
            projectileHits.Clear();

            int count = Physics2D.OverlapCircle(
                transform.position,
                reactionRadius,
                projectileFilter,
                projectileHits
            );

            Projectile2D best = null;
            float bestDistance = float.PositiveInfinity;

            for (int i = 0; i < count; i++)
            {
                Projectile2D projectile =
                    projectileHits[i].GetComponentInParent<Projectile2D>();

                if (
                    !projectile ||
                    projectile.Faction != CombatFaction.Player
                )
                {
                    continue;
                }

                Vector2 toEnemy =
                    (Vector2)transform.position -
                    (Vector2)projectile.transform.position;

                float distance =
                    toEnemy.magnitude;

                if (distance <= 0.001f)
                    continue;

                Vector2 travel =
                    projectile.Velocity.sqrMagnitude > 0.001f
                        ? projectile.Velocity.normalized
                        : Vector2.zero;

                if (
                    Vector2.Dot(
                        travel,
                        toEnemy / distance
                    ) < 0.78f
                )
                {
                    continue;
                }

                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                best = projectile;
            }

            return best;
        }

        private bool ProjectileApproachesFront(
            Projectile2D projectile)
        {
            if (!projectile || !brain)
                return false;

            float direction =
                Mathf.Sign(projectile.Velocity.x);

            return
                Mathf.Abs(direction) > 0f &&
                direction == -brain.Facing;
        }

        private void UpdateGuardMeleeCounter()
        {
            if (
                guardCounterTimer > 0f ||
                !brain.Target ||
                !combatState ||
                combatState.Shield <= 0f
            )
            {
                return;
            }

            if (
                Vector2.Distance(
                    transform.position,
                    brain.Target.position
                ) > guardCounterRange
            )
            {
                return;
            }

            NovaCombatController playerCombat =
                brain.Target.GetComponent<NovaCombatController>();

            Damageable2D playerHealth =
                brain.Target.GetComponent<Damageable2D>();

            if (
                !playerCombat ||
                !playerCombat.IsMeleeActive ||
                !playerHealth ||
                playerHealth.IsDefeated
            )
            {
                return;
            }

            Vector2 direction =
                (
                    (Vector2)brain.Target.position -
                    (Vector2)transform.position
                ).normalized;

            brain.BeginReactionOverride(
                direction * 5.2f,
                0.28f
            );

            playerHealth.ApplyDamage(
                new DamagePacket(
                    guardCounterDamage,
                    direction * 4.2f +
                    Vector2.up * 1.4f,
                    playerHealth.transform.position,
                    CombatFaction.Enemy,
                    -1,
                    0,
                    "enemy-guard-counter"
                )
            );

            guardCounterTimer =
                guardCounterCooldown;
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (
                contactCooldown > 0f ||
                damageable == null ||
                damageable.IsDefeated
            )
            {
                return;
            }

            Damageable2D target =
                collision.collider.GetComponentInParent<Damageable2D>();

            if (
                !target ||
                target.Faction != CombatFaction.Player ||
                target.IsDefeated
            )
            {
                return;
            }

            Vector2 direction =
                (
                    (Vector2)target.transform.position -
                    (Vector2)transform.position
                ).normalized;

            target.ApplyDamage(
                new DamagePacket(
                    profile.ContactDamage,
                    direction * 2.2f,
                    collision.GetContact(0).point,
                    CombatFaction.Enemy,
                    -1,
                    0,
                    "enemy-contact"
                )
            );

            contactCooldown = 0.60f;
        }
    }
}
