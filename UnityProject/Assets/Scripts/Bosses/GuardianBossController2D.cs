using NovaStriker.Combat;
using NovaStriker.Core;
using NovaStriker.Data;
using NovaStriker.Player;
using NovaStriker.Session;
using UnityEngine;

namespace NovaStriker.Bosses
{
    public enum GuardianBossPhase
    {
        Phase1 = 1,
        Phase2 = 2,
        Phase3 = 3
    }

    /// <summary>
    /// Shared six-Guardian greybox boss runtime. Each Guardian has distinct
    /// attack selection, mobility and weak-point cadence while sharing phase,
    /// multiplayer targeting, Break, and presentation-event contracts.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Damageable2D), typeof(Rigidbody2D))]
    public sealed class GuardianBossController2D : MonoBehaviour
    {
        [SerializeField] private GuardianId guardianId;
        [SerializeField] private Damageable2D damageable;
        [SerializeField] private CombatState2D combatState;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Projectile2D projectilePrefab;
        [SerializeField] private StrikeTeamSession session;

        [Header("Arena")]
        [SerializeField] private LayerMask lineOfSightMask;
        [SerializeField] private Vector2 arenaX = new(-8f, 8f);
        [SerializeField] private Vector2 arenaY = new(-2.8f, 4.4f);

        [Header("Timing")]
        [SerializeField] private float phase1Interval = 1.55f;
        [SerializeField] private float phase2Interval = 1.25f;
        [SerializeField] private float phase3Interval = 0.95f;
        [SerializeField] private float weakPointWindow = 0.9f;

        private float attackTimer = 0.8f;
        private float actionTimer;
        private float weakPointTimer;
        private int actionIndex;
        private StrikerPlayerIdentity target;

        public GuardianId GuardianId => guardianId;
        public GuardianBossPhase Phase { get; private set; } =
            GuardianBossPhase.Phase1;
        public bool WeakPointOpen => weakPointTimer > 0f;
        public string WeakPointId => WeakPointName();

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

            ConfigureGuardianProfile();
        }

        private void FixedUpdate()
        {
            if (!damageable || damageable.IsDefeated)
                return;

            float dt = Time.fixedDeltaTime;

            if (!session)
                session = StrikeTeamSession.Active;

            UpdatePhase();
            UpdateWeakPoint(dt);
            AcquireTarget();

            if (!target)
                return;

            if (actionTimer > 0f)
            {
                actionTimer =
                    Mathf.Max(0f, actionTimer - dt);
                UpdateActionMovement(dt);
                return;
            }

            attackTimer =
                Mathf.Max(0f, attackTimer - dt);

            if (attackTimer > 0f)
                return;

            ExecuteNextAction();
            attackTimer = CurrentAttackInterval();
        }

        private void ConfigureGuardianProfile()
        {
            damageable?.ConfigureMaxHealth(
                guardianId == GuardianId.Null
                    ? 1250f
                    : 1100f
            );

            switch (guardianId)
            {
                case GuardianId.Aegis:
                    combatState?.ConfigureDefense(
                        220f,
                        80f,
                        220f,
                        0.25f
                    );
                    break;

                case GuardianId.Cinder:
                    combatState?.ConfigureDefense(
                        0f,
                        120f,
                        210f,
                        0.22f
                    );
                    break;

                case GuardianId.Mycel:
                    combatState?.ConfigureDefense(
                        0f,
                        70f,
                        200f,
                        0.18f
                    );
                    break;

                case GuardianId.Rime:
                    combatState?.ConfigureDefense(
                        0f,
                        240f,
                        235f,
                        0.30f
                    );
                    break;

                case GuardianId.Tempest:
                    combatState?.ConfigureDefense(
                        60f,
                        60f,
                        195f,
                        0.16f
                    );
                    break;

                case GuardianId.Null:
                    combatState?.ConfigureDefense(
                        100f,
                        100f,
                        255f,
                        0.24f
                    );
                    break;
            }
        }

        private void AcquireTarget()
        {
            target = null;

            session?.TryGetNearestCombatReadyPlayer(
                transform.position,
                out target
            );
        }

        private void UpdatePhase()
        {
            float ratio =
                damageable.MaxHealth > 0f
                    ? damageable.Health / damageable.MaxHealth
                    : 1f;

            GuardianBossPhase desired =
                ratio < 0.33f
                    ? GuardianBossPhase.Phase3
                    : ratio < 0.66f
                        ? GuardianBossPhase.Phase2
                        : GuardianBossPhase.Phase1;

            if (desired == Phase)
                return;

            Phase = desired;

            GameplayEventHub.Raise(
                new GameplayCue(
                    GameplayCueType.BossPhaseChanged,
                    damageable.ActorId,
                    transform.position,
                    Vector2.up,
                    (int)Phase,
                    ratio,
                    guardianId.ToString()
                )
            );
        }

        private void UpdateWeakPoint(float dt)
        {
            if (weakPointTimer <= 0f)
                return;

            weakPointTimer =
                Mathf.Max(0f, weakPointTimer - dt);

            if (weakPointTimer <= 0f)
            {
                GameplayEventHub.Raise(
                    new GameplayCue(
                        GameplayCueType.GuardianWeakPointClosed,
                        damageable.ActorId,
                        transform.position,
                        Vector2.zero,
                        (int)guardianId,
                        0f,
                        WeakPointName()
                    )
                );
            }
        }

        private void ExecuteNextAction()
        {
            string action =
                ActionForIndex(
                    actionIndex++
                );

            GameplayEventHub.Raise(
                new GameplayCue(
                    GameplayCueType.BossAttackStarted,
                    damageable.ActorId,
                    transform.position,
                    DirectionToTarget(),
                    (int)Phase,
                    0f,
                    action
                )
            );

            switch (guardianId)
            {
                case GuardianId.Aegis:
                    ExecuteAegis(action);
                    break;
                case GuardianId.Cinder:
                    ExecuteCinder(action);
                    break;
                case GuardianId.Mycel:
                    ExecuteMycel(action);
                    break;
                case GuardianId.Rime:
                    ExecuteRime(action);
                    break;
                case GuardianId.Tempest:
                    ExecuteTempest(action);
                    break;
                case GuardianId.Null:
                    ExecuteNull(action);
                    break;
            }
        }

        private string ActionForIndex(int index)
        {
            int phase = (int)Phase;
            int mod = Mathf.Abs(index) % 3;

            return guardianId switch
            {
                GuardianId.Aegis =>
                    mod == 0
                        ? "shield-volley"
                        : mod == 1
                            ? "shield-rush"
                            : phase >= 2
                                ? "barrier-cage"
                                : "shield-volley",

                GuardianId.Cinder =>
                    mod == 0
                        ? "magma-burst"
                        : mod == 1
                            ? "furnace-rush"
                            : phase >= 2
                                ? "lava-leap"
                                : "magma-burst",

                GuardianId.Mycel =>
                    mod == 0
                        ? "spore-ring"
                        : mod == 1
                            ? "root-strike"
                            : phase >= 2
                                ? "regrowth"
                                : "spore-ring",

                GuardianId.Rime =>
                    mod == 0
                        ? "ice-lance"
                        : mod == 1
                            ? "mirror-step"
                            : phase >= 2
                                ? "crystal-rain"
                                : "ice-lance",

                GuardianId.Tempest =>
                    mod == 0
                        ? "storm-fan"
                        : mod == 1
                            ? "swoop"
                            : phase >= 2
                                ? "lightning-dive"
                                : "storm-fan",

                _ =>
                    mod == 0
                        ? "null-ring"
                        : mod == 1
                            ? "blink-strike"
                            : phase >= 2
                                ? "gravity-fold"
                                : "null-ring"
            };
        }

        private void ExecuteAegis(string action)
        {
            if (action == "shield-rush")
            {
                actionTimer = 0.55f;
                body.linearVelocity =
                    DirectionToTarget() * 6.2f;
                OpenWeakPoint(0.75f);
                return;
            }

            if (action == "barrier-cage")
            {
                RadialBurst(
                    8 + (int)Phase * 2,
                    6.0f,
                    14f,
                    "aegis-barrier"
                );
                return;
            }

            AimedFan(
                2 + (int)Phase,
                0.17f,
                7.6f,
                13f,
                "aegis-volley"
            );
        }

        private void ExecuteCinder(string action)
        {
            if (action == "furnace-rush")
            {
                actionTimer = 0.62f;
                body.linearVelocity =
                    DirectionToTarget() * 7.5f;
                OpenWeakPoint(0.8f);
                return;
            }

            if (action == "lava-leap")
            {
                actionTimer = 0.55f;
                body.linearVelocity =
                    new Vector2(
                        DirectionToTarget().x * 5f,
                        7.5f
                    );
                OpenWeakPoint(0.85f);
                return;
            }

            AimedFan(
                3 + (int)Phase,
                0.12f,
                8.2f,
                15f,
                "cinder-magma"
            );
        }

        private void ExecuteMycel(string action)
        {
            if (action == "regrowth")
            {
                damageable.Heal(
                    damageable.MaxHealth *
                    (Phase == GuardianBossPhase.Phase3
                        ? 0.06f
                        : 0.035f)
                );

                OpenWeakPoint(1.15f);
                return;
            }

            if (action == "root-strike")
            {
                actionTimer = 0.42f;
                body.linearVelocity =
                    new Vector2(
                        DirectionToTarget().x * 4.5f,
                        0f
                    );
                OpenWeakPoint(0.65f);
                return;
            }

            RadialBurst(
                8 + (int)Phase * 2,
                6.4f,
                12f,
                "mycel-spore"
            );
        }

        private void ExecuteRime(string action)
        {
            if (action == "mirror-step")
            {
                BlinkNearTarget(2.4f);
                OpenWeakPoint(0.55f);
                return;
            }

            if (action == "crystal-rain")
            {
                AimedFan(
                    7,
                    0.09f,
                    7.1f,
                    13f,
                    "rime-crystal"
                );
                return;
            }

            AimedFan(
                3 + (int)Phase,
                0.08f,
                8.8f,
                14f,
                "rime-lance"
            );

            if (combatState && combatState.ArmorBroken)
                OpenWeakPoint(1.25f);
        }

        private void ExecuteTempest(string action)
        {
            if (
                action == "swoop" ||
                action == "lightning-dive"
            )
            {
                actionTimer = 0.65f;
                Vector2 dir = DirectionToTarget();

                body.linearVelocity =
                    new Vector2(
                        dir.x * 7.2f,
                        action == "lightning-dive"
                            ? -6.5f
                            : 3.4f
                    );

                OpenWeakPoint(0.75f);
                return;
            }

            AimedFan(
                5 + (int)Phase,
                0.18f,
                8.0f,
                13f,
                "tempest-fan"
            );
        }

        private void ExecuteNull(string action)
        {
            if (action == "blink-strike")
            {
                BlinkNearTarget(2.8f);
                actionTimer = 0.28f;
                body.linearVelocity =
                    DirectionToTarget() * 7.8f;
                OpenWeakPoint(0.55f);
                return;
            }

            if (action == "gravity-fold")
            {
                PullPlayersTowardBoss(5.5f);
                RadialBurst(
                    10 + (int)Phase * 2,
                    6.2f,
                    14f,
                    "null-gravity"
                );
                OpenWeakPoint(0.85f);
                return;
            }

            RadialBurst(
                12 + (int)Phase * 2,
                7.2f,
                15f,
                "null-ring"
            );
        }

        private void UpdateActionMovement(float dt)
        {
            if (!body)
                return;

            body.position = new Vector2(
                Mathf.Clamp(
                    body.position.x,
                    arenaX.x,
                    arenaX.y
                ),
                Mathf.Clamp(
                    body.position.y,
                    arenaY.x,
                    arenaY.y
                )
            );

            if (guardianId == GuardianId.Mycel)
            {
                body.linearVelocity =
                    Vector2.Lerp(
                        body.linearVelocity,
                        Vector2.zero,
                        5f * dt
                    );
            }
        }

        private void OpenWeakPoint(float duration)
        {
            float window =
                Mathf.Max(
                    duration,
                    weakPointWindow
                );

            bool wasClosed =
                weakPointTimer <= 0f;

            weakPointTimer =
                Mathf.Max(
                    weakPointTimer,
                    window
                );

            combatState?.ApplyVulnerable(window);

            if (!wasClosed)
                return;

            GameplayEventHub.Raise(
                new GameplayCue(
                    GameplayCueType.GuardianWeakPointOpened,
                    damageable.ActorId,
                    transform.position,
                    Vector2.up,
                    (int)guardianId,
                    window,
                    WeakPointName()
                )
            );
        }

        private void AimedFan(
            int count,
            float spreadRadians,
            float speed,
            float damage,
            string id)
        {
            if (!projectilePrefab || !target)
                return;

            Vector2 aim = DirectionToTarget();
            float baseAngle =
                Mathf.Atan2(aim.y, aim.x);

            count = Mathf.Max(1, count);

            for (int i = 0; i < count; i++)
            {
                float t =
                    count <= 1
                        ? 0.5f
                        : (float)i / (count - 1);

                float offset =
                    Mathf.Lerp(
                        -spreadRadians,
                        spreadRadians,
                        t
                    );

                SpawnProjectile(
                    baseAngle + offset,
                    speed,
                    damage,
                    id,
                    false
                );
            }
        }

        private void RadialBurst(
            int count,
            float speed,
            float damage,
            string id)
        {
            if (!projectilePrefab)
                return;

            count = Mathf.Max(1, count);

            for (int i = 0; i < count; i++)
            {
                float angle =
                    Mathf.PI * 2f * i / count;

                SpawnProjectile(
                    angle,
                    speed,
                    damage,
                    id,
                    false
                );
            }
        }

        private void SpawnProjectile(
            float angle,
            float speed,
            float damage,
            string id,
            bool perfectOpportunity)
        {
            Projectile2D projectile =
                Instantiate(
                    projectilePrefab,
                    transform.position,
                    Quaternion.identity
                );

            projectile.Initialize(
                -1,
                CombatFaction.Enemy,
                id,
                WeaponBehavior.Standard,
                new Vector2(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle)
                ),
                speed,
                0,
                damage,
                false,
                true,
                perfectOpportunity
            );
        }

        private void BlinkNearTarget(float radius)
        {
            if (!target || !body)
                return;

            float side =
                target.transform.position.x <
                transform.position.x
                    ? 1f
                    : -1f;

            Vector2 next =
                (Vector2)target.transform.position +
                new Vector2(
                    side * radius,
                    Random.Range(-1.2f, 1.2f)
                );

            body.position = new Vector2(
                Mathf.Clamp(next.x, arenaX.x, arenaX.y),
                Mathf.Clamp(next.y, arenaY.x, arenaY.y)
            );

            body.linearVelocity = Vector2.zero;
        }

        private void PullPlayersTowardBoss(float speed)
        {
            if (!session)
                return;

            for (int i = 0; i < StrikeTeamSession.MaxPlayers; i++)
            {
                StrikerPlayerIdentity player =
                    session.GetPlayer(i);

                if (!player || !player.IsCombatReady)
                    continue;

                Damageable2D health =
                    player.GetComponent<Damageable2D>();

                Vector2 delta =
                    (Vector2)transform.position -
                    (Vector2)player.transform.position;

                if (health && delta.sqrMagnitude > 0.001f)
                {
                    health.ApplyExternalVelocity(
                        delta.normalized * speed
                    );
                }
            }
        }

        private Vector2 DirectionToTarget()
        {
            if (!target)
                return Vector2.left;

            Vector2 delta =
                (Vector2)target.transform.position -
                (Vector2)transform.position;

            return delta.sqrMagnitude > 0.001f
                ? delta.normalized
                : Vector2.left;
        }

        private float CurrentAttackInterval()
        {
            return Phase switch
            {
                GuardianBossPhase.Phase3 =>
                    phase3Interval,
                GuardianBossPhase.Phase2 =>
                    phase2Interval,
                _ =>
                    phase1Interval
            };
        }

        private string WeakPointName()
        {
            return guardianId switch
            {
                GuardianId.Aegis => "shield-core",
                GuardianId.Cinder => "reactor-vents",
                GuardianId.Mycel => "bloom-nodes",
                GuardianId.Rime => "crystal-core",
                GuardianId.Tempest => "wing-capacitor",
                _ => "singularity"
            };
        }

        private bool HasLineToTarget()
        {
            if (!target || lineOfSightMask.value == 0)
                return target;

            RaycastHit2D hit =
                Physics2D.Linecast(
                    transform.position,
                    target.transform.position,
                    lineOfSightMask
                );

            return hit.collider == null;
        }
    }
}
