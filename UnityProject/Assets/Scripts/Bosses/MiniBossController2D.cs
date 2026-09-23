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
        private int attackIndex;
        private bool multiplayerScalingApplied;
        private StrikerPlayerIdentity target;

        public MiniBossId MiniBossId => miniBossId;
        public StrikerPlayerIdentity Target => target;

        public void ConfigureIdentity(MiniBossId value)
        {
            miniBossId = value;
            target = null;
            attackTimer = initialAttackDelay;
            attackIndex = 0;
            multiplayerScalingApplied = false;
            ConfigureProfile();
        }

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
            attackIndex = 0;
            multiplayerScalingApplied = false;
        }

        private void FixedUpdate()
        {
            if (!damageable || damageable.IsDefeated)
                return;

            if (!session)
                session = StrikeTeamSession.Active;

            ApplyMultiplayerScalingOnce();
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

            Rigidbody2D targetBody =
                target.GetComponent<Rigidbody2D>();

            Vector2 targetPosition =
                target.transform.position;

            if (targetBody)
            {
                float leadSeconds =
                    miniBossId == MiniBossId.VectorHound
                        ? 0.40f
                        : miniBossId == MiniBossId.RailSentinel
                            ? 0.18f
                            : 0.12f;

                targetPosition +=
                    targetBody.linearVelocity *
                    leadSeconds;
            }

            Vector2 direction =
                (
                    targetPosition -
                    (Vector2)transform.position
                ).normalized;

            string weaponId;
            float cueDamage;

            switch (miniBossId)
            {
                case MiniBossId.Bulwark:
                {
                    weaponId = "mini-bulwark";
                    cueDamage = 18f;

                    if (attackIndex % 3 == 2)
                    {
                        for (int i = 0; i < 6; i++)
                        {
                            float angle =
                                Mathf.PI * 2f * i / 6f;

                            SpawnShot(
                                new Vector2(
                                    Mathf.Cos(angle),
                                    Mathf.Sin(angle)
                                ),
                                6.4f,
                                14f,
                                false,
                                "mini-bulwark-burst"
                            );
                        }
                    }
                    else
                    {
                        SpawnShot(
                            direction,
                            7.8f,
                            cueDamage,
                            false,
                            weaponId
                        );
                    }

                    break;
                }

                case MiniBossId.VectorHound:
                {
                    weaponId = "mini-vector-hound";
                    cueDamage = 15f;
                    float angle =
                        Mathf.Atan2(
                            direction.y,
                            direction.x
                        );

                    for (int i = -1; i <= 1; i += 2)
                    {
                        float offset =
                            attackIndex % 2 == 0
                                ? i * 0.07f
                                : 0f;

                        SpawnShot(
                            new Vector2(
                                Mathf.Cos(angle + offset),
                                Mathf.Sin(angle + offset)
                            ),
                            9.0f,
                            cueDamage,
                            false,
                            weaponId
                        );

                        if (offset == 0f)
                            break;
                    }

                    break;
                }

                case MiniBossId.CliffStalker:
                {
                    weaponId = "mini-cliff-stalker";
                    cueDamage = 15f;
                    float angle =
                        Mathf.Atan2(
                            direction.y,
                            direction.x
                        );

                    for (int i = -1; i <= 1; i++)
                    {
                        SpawnShot(
                            new Vector2(
                                Mathf.Cos(angle + i * 0.11f),
                                Mathf.Sin(angle + i * 0.11f)
                            ),
                            7.8f,
                            cueDamage,
                            false,
                            weaponId
                        );
                    }

                    break;
                }

                default:
                    weaponId = "mini-rail-sentinel";
                    cueDamage = 17f;

                    SpawnShot(
                        direction,
                        12.2f,
                        cueDamage,
                        true,
                        weaponId
                    );
                    break;
            }

            attackIndex++;

            GameplayEventHub.Raise(
                new GameplayCue(
                    GameplayCueType.BossAttackStarted,
                    damageable ? damageable.ActorId : -1,
                    transform.position,
                    direction,
                    (int)miniBossId,
                    cueDamage,
                    weaponId
                )
            );
        }

        private void SpawnShot(
            Vector2 direction,
            float speed,
            float damage,
            bool perfectOpportunity,
            string weaponId)
        {
            Projectile2D shot =
                ProjectilePool2D.Spawn(
                    projectilePrefab,
                    transform.position,
                    Quaternion.identity
                );

            shot.Initialize(
                -1,
                CombatFaction.Enemy,
                weaponId,
                WeaponBehavior.Standard,
                direction.sqrMagnitude > 0.001f
                    ? direction.normalized
                    : Vector2.left,
                speed,
                0,
                damage,
                false,
                true,
                perfectOpportunity
            );
        }

        private void ApplyMultiplayerScalingOnce()
        {
            if (
                multiplayerScalingApplied ||
                !session ||
                !damageable
            )
            {
                return;
            }

            int players =
                session.ParticipatingPlayerCount;

            if (players <= 0)
                return;

            float baseHealth =
                miniBossId switch
                {
                    MiniBossId.Bulwark => 360f,
                    MiniBossId.VectorHound => 300f,
                    MiniBossId.CliffStalker => 310f,
                    _ => 285f
                };

            float scale =
                1f +
                Mathf.Max(0, players - 1) *
                0.28f;

            damageable.ConfigureMaxHealth(
                baseHealth * scale,
                true
            );

            multiplayerScalingApplied = true;
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
