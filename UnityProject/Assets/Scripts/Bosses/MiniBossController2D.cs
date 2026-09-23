using NovaStriker.Campaign;
using NovaStriker.Combat;
using NovaStriker.Core;
using NovaStriker.Data;
using NovaStriker.Player;
using NovaStriker.Session;
using UnityEngine;

namespace NovaStriker.Bosses
{
    /// <summary>
    /// Greybox implementation of the four preserved mini-boss identities.
    /// Production animation/VFX remain presentation-only; health, targeting,
    /// movement and attack timing live here.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Damageable2D), typeof(Rigidbody2D))]
    public sealed class MiniBossController2D : MonoBehaviour
    {
        [SerializeField] private MiniBossId miniBossId;
        [SerializeField] private Damageable2D damageable;
        [SerializeField] private CombatState2D combatState;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Projectile2D projectilePrefab;
        [SerializeField] private StrikeTeamSession session;

        [Header("Targeting")]
        [SerializeField] private float engagementRange = 18f;
        [SerializeField] private LayerMask lineOfSightMask;

        [Header("Attack")]
        [SerializeField] private float initialAttackDelay = 0.8f;
        [SerializeField] private float attackInterval = 1.2f;

        private float attackTimer;
        private StrikerPlayerIdentity target;

        public MiniBossId MiniBossId => miniBossId;
        public StrikerPlayerIdentity Target => target;

        private void Reset()
        {
            damageable = GetComponent<Damageable2D>();
            combatState = GetComponent<CombatState2D>();
            body = GetComponent<Rigidbody2D>();
        }

        private void Awake()
        {
            if (!damageable)
                damageable = GetComponent<Damageable2D>();
            if (!combatState)
                combatState = GetComponent<CombatState2D>();
            if (!body)
                body = GetComponent<Rigidbody2D>();
            if (!session)
                session = StrikeTeamSession.Active;

            ConfigureProfile();
            attackTimer = initialAttackDelay;
        }

        private void FixedUpdate()
        {
            if (!damageable || damageable.IsDefeated)
                return;

            if (!session)
                session = StrikeTeamSession.Active;

            AcquireTarget();

            if (!target)
            {
                StopHorizontal(18f);
                return;
            }

            float dt = Time.fixedDeltaTime;
            UpdateMovement(dt);

            attackTimer =
                Mathf.Max(0f, attackTimer - dt);

            if (
                attackTimer <= 0f &&
                HasClearLineToTarget()
            )
            {
                FireAtTarget();
                attackTimer = AttackIntervalForProfile();
            }
        }

        private void ConfigureProfile()
        {
            switch (miniBossId)
            {
                case MiniBossId.Bulwark:
                    damageable?.ConfigureMaxHealth(360f);
                    combatState?.ConfigureDefense(
                        70f,
                        120f,
                        120f,
                        0.34f
                    );
                    break;

                case MiniBossId.VectorHound:
                    damageable?.ConfigureMaxHealth(300f);
                    combatState?.ConfigureDefense(
                        0f,
                        45f,
                        105f,
                        0.18f
                    );
                    break;

                case MiniBossId.CliffStalker:
                    damageable?.ConfigureMaxHealth(310f);
                    combatState?.ConfigureDefense(
                        0f,
                        35f,
                        100f,
                        0.16f
                    );
                    break;

                case MiniBossId.RailSentinel:
                    damageable?.ConfigureMaxHealth(285f);
                    combatState?.ConfigureDefense(
                        50f,
                        30f,
                        95f,
                        0.14f
                    );
                    break;
            }
        }

        private void AcquireTarget()
        {
            target = null;

            if (!session)
                return;

            if (
                session.TryGetNearestCombatReadyPlayer(
                    transform.position,
                    out StrikerPlayerIdentity nearest
                ) &&
                Vector2.Distance(
                    transform.position,
                    nearest.transform.position
                ) <= engagementRange
            )
            {
                target = nearest;
            }
        }

        private void UpdateMovement(float dt)
        {
            if (!body || !target)
                return;

            Vector2 delta =
                (Vector2)target.transform.position -
                (Vector2)transform.position;

            switch (miniBossId)
            {
                case MiniBossId.Bulwark:
                    MoveHorizontal(
                        Mathf.Sign(delta.x) * 1.3f,
                        8f,
                        dt
                    );
                    break;

                case MiniBossId.VectorHound:
                    MoveHorizontal(
                        Mathf.Sign(delta.x) * 4.9f,
                        22f,
                        dt
                    );
                    break;

                case MiniBossId.CliffStalker:
                {
                    Vector2 desired = new(
                        Mathf.Sign(delta.x) * 1.8f,
                        Mathf.Clamp(delta.y * 2.2f, -4.2f, 4.2f)
                    );

                    body.linearVelocity =
                        Vector2.MoveTowards(
                            body.linearVelocity,
                            desired,
                            16f * dt
                        );
                    break;
                }

                case MiniBossId.RailSentinel:
                    StopHorizontal(30f);
                    break;
            }
        }

        private void FireAtTarget()
        {
            if (!projectilePrefab || !target)
                return;

            Vector2 direction =
                (
                    (Vector2)target.transform.position -
                    (Vector2)transform.position
                ).normalized;

            float speed;
            float damage;
            bool perfectOpportunity;
            string weaponId;

            switch (miniBossId)
            {
                case MiniBossId.Bulwark:
                    speed = 7.8f;
                    damage = 18f;
                    perfectOpportunity = false;
                    weaponId = "mini-bulwark";
                    break;

                case MiniBossId.RailSentinel:
                    speed = 11.4f;
                    damage = 16f;
                    perfectOpportunity = true;
                    weaponId = "mini-rail-sentinel";
                    break;

                case MiniBossId.VectorHound:
                    speed = 8.2f;
                    damage = 15f;
                    perfectOpportunity = false;
                    weaponId = "mini-vector-hound";
                    break;

                default:
                    speed = 7.8f;
                    damage = 15f;
                    perfectOpportunity = false;
                    weaponId = "mini-cliff-stalker";
                    break;
            }

            Projectile2D shot =
                Instantiate(
                    projectilePrefab,
                    transform.position,
                    Quaternion.identity
                );

            shot.Initialize(
                -1,
                CombatFaction.Enemy,
                weaponId,
                WeaponBehavior.Standard,
                direction,
                speed,
                0,
                damage,
                false,
                true,
                perfectOpportunity
            );

            GameplayEventHub.Raise(
                new GameplayCue(
                    GameplayCueType.BossAttackStarted,
                    damageable ? damageable.ActorId : -1,
                    transform.position,
                    direction,
                    (int)miniBossId,
                    damage,
                    weaponId
                )
            );
        }

        private float AttackIntervalForProfile()
        {
            return miniBossId == MiniBossId.RailSentinel
                ? 1.65f
                : miniBossId == MiniBossId.Bulwark
                    ? 1.35f
                    : Mathf.Max(0.5f, attackInterval);
        }

        private bool HasClearLineToTarget()
        {
            if (!target)
                return false;

            if (lineOfSightMask.value == 0)
                return true;

            RaycastHit2D hit =
                Physics2D.Linecast(
                    transform.position,
                    target.transform.position,
                    lineOfSightMask
                );

            return hit.collider == null;
        }

        private void MoveHorizontal(
            float desired,
            float acceleration,
            float dt)
        {
            if (!body)
                return;

            body.linearVelocity = new Vector2(
                Mathf.MoveTowards(
                    body.linearVelocity.x,
                    desired,
                    acceleration * dt
                ),
                body.linearVelocity.y
            );
        }

        private void StopHorizontal(float acceleration)
        {
            MoveHorizontal(
                0f,
                acceleration,
                Time.fixedDeltaTime
            );
        }
    }
}
