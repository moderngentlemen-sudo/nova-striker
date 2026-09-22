using System.Collections.Generic;
using NovaStriker.Combat;
using NovaStriker.Core;
using NovaStriker.Input;
using UnityEngine;

namespace NovaStriker.Player
{
    /// <summary>
    /// Greybox Nova movement controller.
    /// Graphics, animation, VFX, audio, and camera are intentionally external.
    /// Unity-world input convention: +X right, +Y up.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public sealed class NovaMotor2D : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private int playerId;

        [Header("References")]
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private CapsuleCollider2D capsule;
        [SerializeField] private LayerMask worldMask;
        [SerializeField] private LayerMask oneWayMask;

        [Header("Run / Crouch")]
        [SerializeField] private float runSpeed = 6.5f;
        [SerializeField] private float crouchSpeed = 2.5f;
        [SerializeField] private float groundAcceleration = 58f;
        [SerializeField] private float airAcceleration = 34f;
        [SerializeField, Range(0.35f, 0.9f)] private float crouchHeightScale = 0.58f;

        [Header("Jump")]
        [SerializeField] private float jumpSpeed = 12.5f;
        [SerializeField] private int airJumps = 1;

        [Header("One-Way Platforms")]
        [SerializeField] private float oneWayDropDuration = 0.24f;
        [SerializeField] private float oneWayDropVelocity = 1.5f;

        [Header("Wall")]
        [SerializeField] private float wallSlideSpeed = 1.85f;
        [SerializeField] private Vector2 wallJumpVelocity = new(7.2f, 11.8f);
        [SerializeField] private float wallRegrabLockout = 0.12f;

        [Header("Dash")]
        [SerializeField] private float quickDashSpeed = 14.4f;
        [SerializeField] private float burstDashSpeed = 17.8f;
        [SerializeField] private float velocityBreakSpeed = 21.8f;
        [SerializeField] private float quickDashDuration = 0.15f;
        [SerializeField] private float burstDashDuration = 0.22f;
        [SerializeField] private float velocityBreakDuration = 0.31f;

        [Header("Powerslide")]
        [SerializeField] private float quickSlideSpeed = 11.8f;
        [SerializeField] private float burstSlideSpeed = 14.7f;
        [SerializeField] private float velocitySlideSpeed = 18.2f;
        [SerializeField] private float quickSlideDuration = 0.52f;
        [SerializeField] private float burstSlideDuration = 0.67f;
        [SerializeField] private float velocitySlideDuration = 0.82f;

        [Header("Probes")]
        [SerializeField] private Vector2 groundProbeSize = new(0.55f, 0.08f);
        [SerializeField] private float groundProbeDistance = 0.06f;
        [SerializeField] private Vector2 wallProbeSize = new(0.08f, 0.8f);
        [SerializeField] private float wallProbeDistance = 0.06f;

        private readonly List<Collider2D> ignoredOneWayColliders = new();

        private PlayerInputState input;

        private int facing = 1;
        private int remainingAirJumps;
        private int remainingAirDashes = 1;

        private bool grounded;
        private Collider2D groundCollider;
        private int wallDirection;
        private bool crouching;

        private float wallLockout;
        private float oneWayDropTimer;

        private float dashCharge;
        private float dashTimer;
        private ChargeTier activeDashTier = ChargeTier.Quick;
        private Vector2 dashDirection = Vector2.right;

        private float slideTimer;
        private ChargeTier activeSlideTier = ChargeTier.Quick;

        private Vector2 standingColliderSize;
        private Vector2 standingColliderOffset;

        public int PlayerId => playerId;
        public Vector2 AimDirection { get; private set; } = Vector2.right;
        public int Facing => facing;
        public bool Grounded => grounded;
        public bool IsCrouching => crouching;
        public bool IsDashing => dashTimer > 0f;
        public bool IsSliding => slideTimer > 0f;
        public bool IsDroppingThrough => oneWayDropTimer > 0f;
        public float DashCharge => dashCharge;
        public ChargeTier ActiveDashTier => activeDashTier;
        public ChargeTier ActiveSlideTier => activeSlideTier;

        private void Reset()
        {
            body = GetComponent<Rigidbody2D>();
            capsule = GetComponent<CapsuleCollider2D>();
        }

        private void Awake()
        {
            if (!body)
                body = GetComponent<Rigidbody2D>();

            if (!capsule)
                capsule = GetComponent<CapsuleCollider2D>();

            body.freezeRotation = true;

            standingColliderSize = capsule.size;
            standingColliderOffset = capsule.offset;

            remainingAirJumps = airJumps;
        }

        private void OnDisable()
        {
            RestoreIgnoredOneWayCollisions();
        }

        public void SetInput(PlayerInputState state)
        {
            input = state;

            if (state.Aim.sqrMagnitude > 0.0484f)
            {
                AimDirection = state.Aim.normalized;

                if (Mathf.Abs(AimDirection.x) > 0.12f)
                    facing = AimDirection.x < 0f ? -1 : 1;
            }
            else if (Mathf.Abs(state.Move.x) > 0.18f)
            {
                facing = state.Move.x < 0f ? -1 : 1;
                AimDirection = new Vector2(facing, 0f);
            }
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            UpdateOneWayDrop(dt);
            UpdateProbes();

            wallLockout = Mathf.Max(0f, wallLockout - dt);

            if (grounded)
            {
                remainingAirJumps = airJumps;
                remainingAirDashes = 1;
            }

            HandleCrouch();

            if (!TryBeginDropThrough())
                HandleJump();

            HandleDashCharge(dt);

            if (slideTimer > 0f)
            {
                UpdateSlide(dt);
                return;
            }

            if (dashTimer > 0f)
            {
                UpdateDash(dt);
                return;
            }

            UpdateWallSlide();
            UpdateRun(dt);
        }

        private void UpdateOneWayDrop(float dt)
        {
            if (oneWayDropTimer <= 0f)
                return;

            oneWayDropTimer = Mathf.Max(0f, oneWayDropTimer - dt);

            if (oneWayDropTimer <= 0f)
                RestoreIgnoredOneWayCollisions();
        }

        private void RestoreIgnoredOneWayCollisions()
        {
            if (!capsule)
                return;

            foreach (Collider2D ignored in ignoredOneWayColliders)
            {
                if (ignored)
                    Physics2D.IgnoreCollision(capsule, ignored, false);
            }

            ignoredOneWayColliders.Clear();
            oneWayDropTimer = 0f;
        }

        private void UpdateProbes()
        {
            Bounds bounds = capsule.bounds;
            Vector2 groundOrigin = new(bounds.center.x, bounds.min.y);

            int groundMask = oneWayDropTimer > 0f
                ? worldMask.value
                : worldMask.value | oneWayMask.value;

            RaycastHit2D groundHit = Physics2D.BoxCast(
                groundOrigin,
                groundProbeSize,
                0f,
                Vector2.down,
                groundProbeDistance,
                groundMask
            );

            groundCollider = groundHit.collider;
            grounded = groundCollider != null;

            wallDirection = 0;

            if (wallLockout > 0f)
                return;

            Vector2 center = bounds.center;

            bool left = Physics2D.BoxCast(
                center,
                wallProbeSize,
                0f,
                Vector2.left,
                wallProbeDistance,
                worldMask
            ).collider != null;

            bool right = Physics2D.BoxCast(
                center,
                wallProbeSize,
                0f,
                Vector2.right,
                wallProbeDistance,
                worldMask
            ).collider != null;

            if (left)
                wallDirection = -1;
            else if (right)
                wallDirection = 1;
        }

        private void HandleCrouch()
        {
            bool wantsCrouch =
                grounded &&
                input.Move.y < -0.55f &&
                dashTimer <= 0f &&
                slideTimer <= 0f;

            if (wantsCrouch && !crouching)
            {
                SetCrouched(true);

                GameplayEventHub.Raise(new GameplayCue(
                    GameplayCueType.CrouchStarted,
                    playerId,
                    transform.position,
                    Vector2.down
                ));
            }
            else if (
                !wantsCrouch &&
                crouching &&
                slideTimer <= 0f &&
                CanStand()
            )
            {
                SetCrouched(false);

                GameplayEventHub.Raise(new GameplayCue(
                    GameplayCueType.CrouchEnded,
                    playerId,
                    transform.position,
                    Vector2.up
                ));
            }
        }

        private void SetCrouched(bool value)
        {
            crouching = value;

            if (!value)
            {
                capsule.size = standingColliderSize;
                capsule.offset = standingColliderOffset;
                return;
            }

            capsule.size = new Vector2(
                standingColliderSize.x,
                standingColliderSize.y * crouchHeightScale
            );

            float offsetDrop =
                standingColliderSize.y *
                (1f - crouchHeightScale) *
                0.5f;

            capsule.offset =
                standingColliderOffset +
                Vector2.down * offsetDrop;
        }

        private bool CanStand()
        {
            float scaleX = Mathf.Abs(transform.lossyScale.x);
            float scaleY = Mathf.Abs(transform.lossyScale.y);

            Vector2 worldSize = new(
                standingColliderSize.x * scaleX * 0.96f,
                standingColliderSize.y * scaleY * 0.98f
            );

            float bottom = capsule.bounds.min.y;
            Vector2 worldCenter = new(
                capsule.bounds.center.x,
                bottom + worldSize.y * 0.5f + 0.01f
            );

            Collider2D blocker = Physics2D.OverlapCapsule(
                worldCenter,
                worldSize,
                capsule.direction,
                transform.eulerAngles.z,
                worldMask
            );

            return blocker == null;
        }

        private bool TryBeginDropThrough()
        {
            if (
                !input.JumpPressed ||
                !crouching ||
                !grounded ||
                !groundCollider ||
                !IsOneWay(groundCollider)
            )
            {
                return false;
            }

            if (!ignoredOneWayColliders.Contains(groundCollider))
            {
                Physics2D.IgnoreCollision(capsule, groundCollider, true);
                ignoredOneWayColliders.Add(groundCollider);
            }

            oneWayDropTimer = oneWayDropDuration;
            grounded = false;
            groundCollider = null;

            body.position += Vector2.down * 0.08f;
            body.linearVelocity = new Vector2(
                body.linearVelocity.x,
                Mathf.Min(body.linearVelocity.y, -oneWayDropVelocity)
            );

            GameplayEventHub.Raise(new GameplayCue(
                GameplayCueType.DropThrough,
                playerId,
                transform.position,
                Vector2.down
            ));

            return true;
        }

        private bool IsOneWay(Collider2D target)
        {
            if (!target)
                return false;

            int layerBit = 1 << target.gameObject.layer;
            return (oneWayMask.value & layerBit) != 0;
        }

        private void HandleJump()
        {
            if (!input.JumpPressed)
                return;

            if (grounded)
            {
                body.linearVelocity = new Vector2(
                    body.linearVelocity.x,
                    jumpSpeed
                );

                GameplayEventHub.Raise(new GameplayCue(
                    GameplayCueType.Jump,
                    playerId,
                    transform.position,
                    Vector2.up
                ));

                return;
            }

            if (wallDirection != 0)
            {
                body.linearVelocity = new Vector2(
                    -wallDirection * wallJumpVelocity.x,
                    wallJumpVelocity.y
                );

                facing = -wallDirection;
                AimDirection = new Vector2(facing, 0f);
                wallLockout = wallRegrabLockout;
                remainingAirDashes = 1;

                GameplayEventHub.Raise(new GameplayCue(
                    GameplayCueType.WallJump,
                    playerId,
                    transform.position,
                    new Vector2(-wallDirection, 1f).normalized
                ));

                return;
            }

            if (remainingAirJumps > 0)
            {
                remainingAirJumps--;

                body.linearVelocity = new Vector2(
                    body.linearVelocity.x,
                    jumpSpeed * 0.92f
                );

                GameplayEventHub.Raise(new GameplayCue(
                    GameplayCueType.Jump,
                    playerId,
                    transform.position,
                    Vector2.up,
                    1
                ));
            }
        }

        private void HandleDashCharge(float dt)
        {
            if (
                input.DashHeld &&
                dashTimer <= 0f &&
                slideTimer <= 0f
            )
            {
                dashCharge += dt;
            }

            if (
                !input.DashReleased ||
                dashCharge <= 0f
            )
            {
                return;
            }

            ChargeTier tier =
                ChargeTierRules.FromDashCharge(dashCharge);

            dashCharge = 0f;

            if (crouching && grounded)
            {
                BeginSlide(tier);
                return;
            }

            BeginDash(tier);
        }

        private void BeginDash(ChargeTier tier)
        {
            if (!grounded && remainingAirDashes <= 0)
                return;

            Vector2 direction =
                input.Move.sqrMagnitude > 0.04f
                    ? input.Move.normalized
                    : new Vector2(facing, 0f);

            dashDirection = direction;
            activeDashTier = tier;

            if (!grounded)
                remainingAirDashes--;

            dashTimer = tier switch
            {
                ChargeTier.Quick => quickDashDuration,
                ChargeTier.Burst => burstDashDuration,
                _ => velocityBreakDuration
            };

            GameplayEventHub.Raise(new GameplayCue(
                GameplayCueType.DashStarted,
                playerId,
                transform.position,
                dashDirection,
                (int)tier,
                dashTimer
            ));
        }

        private void UpdateDash(float dt)
        {
            dashTimer = Mathf.Max(0f, dashTimer - dt);

            float speed = activeDashTier switch
            {
                ChargeTier.Quick => quickDashSpeed,
                ChargeTier.Burst => burstDashSpeed,
                _ => velocityBreakSpeed
            };

            body.linearVelocity = dashDirection * speed;

            if (dashTimer > 0f)
                return;

            GameplayEventHub.Raise(new GameplayCue(
                GameplayCueType.DashEnded,
                playerId,
                transform.position,
                dashDirection,
                (int)activeDashTier
            ));
        }

        private void BeginSlide(ChargeTier tier)
        {
            activeSlideTier = tier;
            dashDirection = new Vector2(facing, 0f);

            if (!crouching)
                SetCrouched(true);

            slideTimer = tier switch
            {
                ChargeTier.Quick => quickSlideDuration,
                ChargeTier.Burst => burstSlideDuration,
                _ => velocitySlideDuration
            };

            GameplayEventHub.Raise(new GameplayCue(
                GameplayCueType.SlideStarted,
                playerId,
                transform.position,
                dashDirection,
                (int)tier,
                slideTimer
            ));
        }

        private void UpdateSlide(float dt)
        {
            slideTimer = Mathf.Max(0f, slideTimer - dt);

            float speed = activeSlideTier switch
            {
                ChargeTier.Quick => quickSlideSpeed,
                ChargeTier.Burst => burstSlideSpeed,
                _ => velocitySlideSpeed
            };

            body.linearVelocity = new Vector2(
                dashDirection.x * speed,
                0f
            );

            if (slideTimer > 0f)
                return;

            bool stillHoldingDown = input.Move.y < -0.55f;

            if (
                !stillHoldingDown &&
                CanStand()
            )
            {
                SetCrouched(false);
            }

            GameplayEventHub.Raise(new GameplayCue(
                GameplayCueType.SlideEnded,
                playerId,
                transform.position,
                dashDirection,
                (int)activeSlideTier
            ));
        }

        private void UpdateWallSlide()
        {
            if (
                grounded ||
                wallDirection == 0 ||
                Mathf.Sign(input.Move.x) != wallDirection ||
                body.linearVelocity.y >= 0f
            )
            {
                return;
            }

            if (body.linearVelocity.y < -wallSlideSpeed)
            {
                body.linearVelocity = new Vector2(
                    body.linearVelocity.x,
                    -wallSlideSpeed
                );
            }
        }

        private void UpdateRun(float dt)
        {
            float speed = crouching ? crouchSpeed : runSpeed;
            float desired = input.Move.x * speed;
            float acceleration = grounded
                ? groundAcceleration
                : airAcceleration;

            float nextX = Mathf.MoveTowards(
                body.linearVelocity.x,
                desired,
                acceleration * dt
            );

            body.linearVelocity = new Vector2(
                nextX,
                body.linearVelocity.y
            );
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!capsule)
                capsule = GetComponent<CapsuleCollider2D>();

            if (!capsule)
                return;

            Bounds bounds = capsule.bounds;

            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(
                new Vector2(bounds.center.x, bounds.min.y) +
                Vector2.down * groundProbeDistance,
                groundProbeSize
            );

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(
                (Vector2)bounds.center + Vector2.left * wallProbeDistance,
                wallProbeSize
            );
            Gizmos.DrawWireCube(
                (Vector2)bounds.center + Vector2.right * wallProbeDistance,
                wallProbeSize
            );
        }
#endif
    }
}
