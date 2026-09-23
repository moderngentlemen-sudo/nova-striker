using System;
using System.Collections.Generic;
using NovaStriker.Combat;
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
        [SerializeField] private Transform target;

        [Header("Perception")]
        [SerializeField] private float detectionRange = 12f;
        [SerializeField] private LayerMask lineOfSightMask;

        private readonly List<EnemyRoleModule2D> modules = new();
        private EnemyRoleModule2D activeModule;

        public int ActorId => actorId;
        public EnemyRole Role => role;
        public EnemyBrainState State { get; private set; } = EnemyBrainState.Idle;
        public Rigidbody2D Body => body;
        public Damageable2D Damageable => damageable;
        public Transform Target => target;
        public bool HasTarget => target;
        public bool IsDefeated => !damageable || damageable.IsDefeated;

        public Vector2 TargetPosition =>
            target ? (Vector2)target.position : (Vector2)transform.position;

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

        private void Reset()
        {
            body = GetComponent<Rigidbody2D>();
            damageable = GetComponent<Damageable2D>();
        }

        private void Awake()
        {
            if (!body)
                body = GetComponent<Rigidbody2D>();

            if (!damageable)
                damageable = GetComponent<Damageable2D>();

            ResolveModules();
        }

        private void OnEnable()
        {
            if (damageable)
            {
                damageable.Damaged += OnDamaged;
                damageable.Defeated += OnDefeated;
            }
        }

        private void OnDisable()
        {
            if (damageable)
            {
                damageable.Damaged -= OnDamaged;
                damageable.Defeated -= OnDefeated;
            }

            activeModule?.OnRoleExit(this);
        }

        private void FixedUpdate()
        {
            if (IsDefeated)
            {
                State = EnemyBrainState.Defeated;
                StopHorizontal();
                return;
            }

            if (!target || TargetDistance > detectionRange)
            {
                State = EnemyBrainState.Idle;
                activeModule?.TickIdle(this, Time.fixedDeltaTime);
                return;
            }

            State = EnemyBrainState.Engage;
            activeModule?.TickEngage(this, Time.fixedDeltaTime);
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

        public void MoveHorizontal(
            float desiredVelocity,
            float acceleration,
            float dt)
        {
            if (!body)
                return;

            float nextX = Mathf.MoveTowards(
                body.linearVelocity.x,
                desiredVelocity,
                Mathf.Max(0f, acceleration) * dt
            );

            body.linearVelocity = new Vector2(
                nextX,
                body.linearVelocity.y
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
