using System;
using System.Collections.Generic;
using NovaStriker.Combat;
using NovaStriker.Player;
using NovaStriker.Session;
using UnityEngine;

namespace NovaStriker.Enemies
{
    public enum EnemyRole
    {
        Anchor = 0,
        Artillery = 1,
        Flanker = 2,
        Skirmisher = 3,
        Aerial = 4
    }

    public enum EnemyBrainState
    {
        Idle = 0,
        Engage = 1,
        Recover = 2,
        Defeated = 3
    }

    /// <summary>
    /// Shared enemy runtime shell. Role modules own tactical behavior while
    /// Damageable2D, Rigidbody2D, and Unity collision remain authoritative.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(Damageable2D))]
    public sealed class EnemyBrain2D : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private int actorId = 100;
        [SerializeField] private EnemyRole role = EnemyRole.Skirmisher;

        [Header("References")]
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Damageable2D damageable;
        [SerializeField] private CombatState2D combatState;
        [SerializeField] private Transform target;

        [Header("Perception")]
        [SerializeField] private float detectionRange = 12f;
        [SerializeField] private LayerMask lineOfSightMask;
        [SerializeField] private bool useStrikeTeamTargeting = true;
        [SerializeField, Min(0.05f)] private float retargetInterval = 0.30f;

        private readonly List<EnemyRoleModule2D> modules = new();
        private EnemyRoleModule2D activeModule;
        private float retargetTimer;
        private float reactionOverrideTimer;
        private Vector2 reactionOverrideVelocity;
        private Vector2 tacticalTargetOffset;
        private Vector2 archetypeTargetOffset;
        private bool wasDisabled;

        public int ActorId => actorId;
        public EnemyRole Role => role;
        public EnemyBrainState State { get; private set; } = EnemyBrainState.Idle;
        public Rigidbody2D Body => body;
        public Damageable2D Damageable => damageable;
        public Transform Target => target;
        public bool HasTarget => target;
        public bool IsDefeated => !damageable || damageable.IsDefeated;
        public int Facing { get; private set; } = 1;

        public Vector2 TargetPosition =>
            target ? (Vector2)target.position : (Vector2)transform.position;

        public Vector2 TacticalTargetOffset =>
            tacticalTargetOffset + archetypeTargetOffset;

        public Vector2 TacticalTargetPosition =>
            TargetPosition + TacticalTargetOffset;

        public Vector2 DirectionToTarget
        {
            get
            {
                Vector2 delta =
                    TargetPosition - (Vector2)transform.position;

                return delta.sqrMagnitude > 0.0001f
                    ? delta.normalized
                    : Vector2.zero;
            }
        }

        public float TargetDistance =>
            target
                ? Vector2.Distance(transform.position, target.position)
                : float.PositiveInfinity;

        public Vector2 DirectionToTacticalTarget
        {
            get
            {
                Vector2 delta =
                    TacticalTargetPosition -
                    (Vector2)transform.position;

                return delta.sqrMagnitude > 0.0001f
                    ? delta.normalized
                    : Vector2.zero;
            }
        }

        public float TacticalTargetDistance =>
            target
                ? Vector2.Distance(
                    transform.position,
                    TacticalTargetPosition
                )
                : float.PositiveInfinity;

        public Vector2 TargetVelocity
        {
            get
            {
                if (!target)
                    return Vector2.zero;

                Rigidbody2D targetBody =
                    target.GetComponent<Rigidbody2D>();

                return targetBody
                    ? targetBody.linearVelocity
                    : Vector2.zero;
            }
        }

        private void Reset()
        {
            body = GetComponent<Rigidbody2D>();
            damageable = GetComponent<Damageable2D>();
            combatState = GetComponent<CombatState2D>();
        }

        private void Awake()
        {
            if (!body)
                body = GetComponent<Rigidbody2D>();

            if (!damageable)
                damageable = GetComponent<Damageable2D>();

            if (!combatState)
                combatState = GetComponent<CombatState2D>();

            ResolveModules();
        }

        private void OnEnable()
        {
            EnemySquadCoordinator2D.Active?.Register(this);

            if (damageable)
            {
                damageable.Damaged += OnDamaged;
                damageable.Defeated += OnDefeated;
            }

            if (wasDisabled)
            {
                activeModule?.OnRoleEnter(this);
                wasDisabled = false;
            }
        }

        private void OnDisable()
        {
            EnemySquadCoordinator2D.Active?.Unregister(this);

            if (damageable)
            {
                damageable.Damaged -= OnDamaged;
                damageable.Defeated -= OnDefeated;
            }

            activeModule?.OnRoleExit(this);
            wasDisabled = true;
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            UpdateStrikeTeamTarget(dt);

            if (IsDefeated)
            {
                State = EnemyBrainState.Defeated;
                StopHorizontal();
                return;
            }

            if (combatState && combatState.IsStaggered)
            {
                State = EnemyBrainState.Recover;
                StopHorizontal(1000f);
                return;
            }

            if (reactionOverrideTimer > 0f)
            {
                reactionOverrideTimer =
                    Mathf.Max(
                        0f,
                        reactionOverrideTimer - dt
                    );

                if (body)
                    body.linearVelocity = reactionOverrideVelocity;

                State = EnemyBrainState.Recover;
                return;
            }

            if (!target || TargetDistance > detectionRange)
            {
                State = EnemyBrainState.Idle;
                activeModule?.TickIdle(this, dt);
                return;
            }

            Vector2 targetDirection =
                DirectionToTarget;

            if (Mathf.Abs(targetDirection.x) > 0.05f)
            {
                Facing =
                    targetDirection.x < 0f
                        ? -1
                        : 1;
            }

            State = EnemyBrainState.Engage;
            activeModule?.TickEngage(this, dt);
        }

        private void UpdateStrikeTeamTarget(float dt)
        {
            if (!useStrikeTeamTargeting)
                return;

            retargetTimer =
                Mathf.Max(0f, retargetTimer - dt);

            bool targetInvalid =
                !target ||
                !target.gameObject.activeInHierarchy;

            if (
                !targetInvalid &&
                retargetTimer > 0f
            )
            {
                return;
            }

            StrikeTeamSession session =
                StrikeTeamSession.Active;

            if (
                session &&
                session.TryGetNearestCombatReadyPlayer(
                    transform.position,
                    out StrikerPlayerIdentity nearest
                )
            )
            {
                target = nearest.transform;
            }
            else if (targetInvalid)
            {
                target = null;
            }

            retargetTimer =
                Mathf.Max(0.05f, retargetInterval);
        }

        public void SetTacticalTargetOffset(
            Vector2 offset)
        {
            tacticalTargetOffset = offset;
        }

        public void ClearTacticalTargetOffset()
        {
            tacticalTargetOffset = Vector2.zero;
        }

        public void SetArchetypeTargetOffset(
            Vector2 offset)
        {
            archetypeTargetOffset = offset;
        }

        public void ClearArchetypeTargetOffset()
        {
            archetypeTargetOffset = Vector2.zero;
        }

        public void PrepareForPoolSpawn()
        {
            State = EnemyBrainState.Idle;
            target = null;
            retargetTimer = 0f;
            reactionOverrideTimer = 0f;
            reactionOverrideVelocity = Vector2.zero;
            tacticalTargetOffset = Vector2.zero;
            archetypeTargetOffset = Vector2.zero;
            Facing = 1;

            if (body)
            {
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
            }
        }

        public void SetTarget(Transform value)
        {
            target = value;
        }

        public void SetRole(EnemyRole value)
        {
            if (role == value && activeModule)
                return;

            activeModule?.OnRoleExit(this);
            role = value;
            ResolveModules();
        }

        public bool HasClearLineToTarget()
        {
            if (!target)
                return false;

            if (lineOfSightMask.value == 0)
                return true;

            RaycastHit2D hit = Physics2D.Linecast(
                transform.position,
                target.position,
                lineOfSightMask
            );

            return hit.collider == null;
        }

        public void BeginReactionOverride(
            Vector2 velocity,
            float duration)
        {
            reactionOverrideVelocity = velocity;
            reactionOverrideTimer =
                Mathf.Max(
                    reactionOverrideTimer,
                    Mathf.Max(0f, duration)
                );

            if (body)
                body.linearVelocity = velocity;
        }

        public void MoveVelocity(
            Vector2 desiredVelocity,
            float acceleration,
            float dt)
        {
            if (!body)
                return;

            float movementMultiplier =
                combatState
                    ? combatState.MovementMultiplier
                    : 1f;

            body.linearVelocity =
                Vector2.MoveTowards(
                    body.linearVelocity,
                    desiredVelocity * movementMultiplier,
                    Mathf.Max(0f, acceleration) * dt
                );
        }

        public void MoveHorizontal(
            float desiredVelocity,
            float acceleration,
            float dt)
        {
            if (!body)
                return;

            float movementMultiplier =
                combatState
                    ? combatState.MovementMultiplier
                    : 1f;

            float nextX = Mathf.MoveTowards(
                body.linearVelocity.x,
                desiredVelocity * movementMultiplier,
                Mathf.Max(0f, acceleration) * dt
            );

            body.linearVelocity = new Vector2(
                nextX,
                body.linearVelocity.y
            );
        }

        public void MoveVertical(
            float desiredVelocity,
            float acceleration,
            float dt)
        {
            if (!body)
                return;

            float movementMultiplier =
                combatState
                    ? combatState.MovementMultiplier
                    : 1f;

            float nextY = Mathf.MoveTowards(
                body.linearVelocity.y,
                desiredVelocity * movementMultiplier,
                Mathf.Max(0f, acceleration) * dt
            );

            body.linearVelocity = new Vector2(
                body.linearVelocity.x,
                nextY
            );
        }

        public void StopHorizontal(float acceleration = 24f)
        {
            if (!body)
                return;

            float dt = Time.fixedDeltaTime;

            body.linearVelocity = new Vector2(
                Mathf.MoveTowards(
                    body.linearVelocity.x,
                    0f,
                    Mathf.Max(0f, acceleration) * dt
                ),
                body.linearVelocity.y
            );
        }

        private void ResolveModules()
        {
            modules.Clear();
            GetComponents(modules);

            activeModule = null;

            for (int i = 0; i < modules.Count; i++)
            {
                EnemyRoleModule2D module = modules[i];

                if (!module || module.Role != role)
                    continue;

                activeModule = module;
                break;
            }

            activeModule?.OnRoleEnter(this);
        }

        private void OnDamaged(DamagePacket packet)
        {
            if (State == EnemyBrainState.Idle)
                State = EnemyBrainState.Engage;

            activeModule?.OnDamaged(this, packet);
        }

        private void OnDefeated(DamagePacket packet)
        {
            State = EnemyBrainState.Defeated;
            activeModule?.OnDefeated(this, packet);
            StopHorizontal(1000f);
        }
    }

    /// <summary>
    /// Base class for composable tactical roles. A prefab may carry multiple
    /// modules and switch roles without replacing its shared combat shell.
    /// </summary>
    public abstract class EnemyRoleModule2D : MonoBehaviour
    {
        public abstract EnemyRole Role { get; }

        public virtual void OnRoleEnter(EnemyBrain2D brain) { }
        public virtual void OnRoleExit(EnemyBrain2D brain) { }
        public virtual void TickIdle(EnemyBrain2D brain, float dt) { }
        public abstract void TickEngage(EnemyBrain2D brain, float dt);
        public virtual void OnDamaged(EnemyBrain2D brain, DamagePacket packet) { }
        public virtual void OnDefeated(EnemyBrain2D brain, DamagePacket packet) { }
    }
}
