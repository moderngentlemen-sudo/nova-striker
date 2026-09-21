using NovaStriker.Combat;
using NovaStriker.Input;
using UnityEngine;

namespace NovaStriker.Player
{
    /// <summary>
    /// Greybox Nova movement controller.
    /// Graphics, animation, VFX, audio, and camera are intentionally external.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public sealed class NovaMotor2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private CapsuleCollider2D capsule;
        [SerializeField] private LayerMask worldMask;

        [Header("Run")]
        [SerializeField] private float runSpeed = 6.5f;
        [SerializeField] private float groundAcceleration = 58f;
        [SerializeField] private float airAcceleration = 34f;

        [Header("Jump")]
        [SerializeField] private float jumpSpeed = 12.5f;
        [SerializeField] private int airJumps = 1;

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
        [SerializeField] private float slideDuration = 0.55f;

        [Header("Probes")]
        [SerializeField] private Vector2 groundProbeSize = new(0.55f, 0.08f);
        [SerializeField] private float groundProbeDistance = 0.06f;
        [SerializeField] private Vector2 wallProbeSize = new(0.08f, 0.8f);
        [SerializeField] private float wallProbeDistance = 0.06f;

        private PlayerInputState input;

        private int facing = 1;
        private int remainingAirJumps;
        private int remainingAirDashes = 1;

        private bool grounded;
        private int wallDirection;

        private float wallLockout;
        private float dashCharge;
        private float dashTimer;
        private ChargeTier activeDashTier = ChargeTier.Quick;
        private Vector2 dashDirection = Vector2.right;

        private float slideTimer;
        private ChargeTier activeSlideTier = ChargeTier.Quick;
        private Vector2 standingColliderSize;
        private Vector2 standingColliderOffset;

        public Vector2 AimDirection { get; private set; } = Vector2.right;
        public int Facing => facing;
        public bool Grounded => grounded;
        public bool IsDashing => dashTimer > 0f;
        public bool IsSliding => slideTimer > 0f;
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
            if (!body) body = GetComponent<Rigidbody2D>();
            if (!capsule) capsule = GetComponent<CapsuleCollider2D>();

            body.freezeRotation = true;

            standingColliderSize = capsule.size;
            standingColliderOffset = capsule.offset;

            remainingAirJumps = airJumps;
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
            }
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            UpdateProbes();

            wallLockout = Mathf.Max(0f, wallLockout - dt);

            if (grounded)
            {
                remainingAirJumps = airJumps;
                remainingAirDashes = 1;
            }

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

        private void UpdateProbes()
        {
            Bounds bounds = capsule.bounds;

            Vector2 groundOrigin = new(bounds.center.x, bounds.min.y);
            grounded = Physics2D.BoxCast(
                groundOrigin,
                groundProbeSize,
                0f,
                Vector2.down,
                groundProbeDistance,
                worldMask
            ).collider != null;

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

            if (left) wallDirection = -1;
            else if (right) wallDirection = 1;
        }

        private void HandleJump()
        {
            if (!input.JumpPressed)
                return;

            if (grounded)
            {
                body.linearVelocity = new Vector2(body.linearVelocity.x, jumpSpeed);
                return;
            }

            if (wallDirection != 0)
            {
                body.linearVelocity = new Vector2(
                    -wallDirection * wallJumpVelocity.x,
                    wallJumpVelocity.y
                );

                facing = -wallDirection;
                wallLockout = wallRegrabLockout;
                remainingAirDashes = 1;
                return;
            }

            if (remainingAirJumps > 0)
            {
                remainingAirJumps--;
                body.linearVelocity = new Vector2(
                    body.linearVelocity.x,
                    jumpSpeed * 0.92f
                );
            }
        }

        private void HandleDashCharge(float dt)
        {
            if (input.DashHeld && dashTimer <= 0f && slideTimer <= 0f)
                dashCharge += dt;

            if (!input.DashReleased || dashCharge <= 0f)
                return;

            ChargeTier tier = ChargeTierRules.FromDashCharge(dashCharge);
            dashCharge = 0f;

            bool crouching = grounded && input.Move.y > 0.55f;

            if (crouching)
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

            Vector2 direction = input.Move.sqrMagnitude > 0.04f
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

            // Preserve the original dash direction and tier for the full burst.
            body.linearVelocity = dashDirection * speed;
        }

        private void BeginSlide(ChargeTier tier)
        {
            activeSlideTier = tier;
            slideTimer = slideDuration + ((int)tier - 1) * 0.12f;
            dashDirection = new Vector2(facing, 0f);

            capsule.size = new Vector2(
                standingColliderSize.x,
                standingColliderSize.y * 0.58f
            );

            capsule.offset = standingColliderOffset +
                Vector2.down * standingColliderSize.y * 0.21f;
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
                body.linearVelocity.y
            );

            if (slideTimer <= 0f)
            {
                capsule.size = standingColliderSize;
                capsule.offset = standingColliderOffset;
            }
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
            float desired = input.Move.x * runSpeed;
            float acceleration = grounded ? groundAcceleration : airAcceleration;

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
    }
}
