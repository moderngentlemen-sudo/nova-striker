using System.Collections.Generic;
using NovaStriker.Core;
using NovaStriker.Data;
using NovaStriker.Player;
using UnityEngine;

namespace NovaStriker.Presentation
{
    /// <summary>
    /// Presentation-only bridge between authoritative gameplay state and
    /// character visuals / Animator parameters.
    ///
    /// Production Nova and Echo models can be dropped under their respective
    /// visual roots without changing movement or combat code.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StrikerPresentationBridge : MonoBehaviour
    {
        [Header("Gameplay")]
        [SerializeField] private NovaMotor2D motor;
        [SerializeField] private NovaCombatController combat;

        [Header("Character Visual Roots")]
        [SerializeField] private GameObject novaVisualRoot;
        [SerializeField] private GameObject echoVisualRoot;

        [Header("Animators (optional)")]
        [SerializeField] private Animator novaAnimator;
        [SerializeField] private Animator echoAnimator;

        [Header("Facing")]
        [SerializeField] private bool rotateVisualForFacing = true;
        [SerializeField] private float facingRightY = 0f;
        [SerializeField] private float facingLeftY = 180f;

        private StrikerCharacter activeCharacter;
        private GameObject activeVisualRoot;
        private Animator activeAnimator;
        private readonly HashSet<int> animatorParameters = new();

        // Continuous parameters.
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int VelocityXHash = Animator.StringToHash("VelocityX");
        private static readonly int VelocityYHash = Animator.StringToHash("VelocityY");
        private static readonly int AimXHash = Animator.StringToHash("AimX");
        private static readonly int AimYHash = Animator.StringToHash("AimY");
        private static readonly int FacingHash = Animator.StringToHash("Facing");
        private static readonly int GroundedHash = Animator.StringToHash("Grounded");
        private static readonly int CrouchingHash = Animator.StringToHash("Crouching");
        private static readonly int WallSlidingHash = Animator.StringToHash("WallSliding");
        private static readonly int DashingHash = Animator.StringToHash("Dashing");
        private static readonly int SlidingHash = Animator.StringToHash("Sliding");
        private static readonly int GrappleTraversalHash = Animator.StringToHash("GrappleTraversal");
        private static readonly int CounteringHash = Animator.StringToHash("Countering");
        private static readonly int FireChargeHash = Animator.StringToHash("FireCharge");
        private static readonly int DashTierHash = Animator.StringToHash("DashTier");
        private static readonly int MeleeStepHash = Animator.StringToHash("MeleeStep");
        private static readonly int CounterModeHash = Animator.StringToHash("CounterMode");
        private static readonly int CharacterHash = Animator.StringToHash("Character");

        // Cue-driven triggers.
        private static readonly int JumpHash = Animator.StringToHash("Jump");
        private static readonly int WallJumpHash = Animator.StringToHash("WallJump");
        private static readonly int DashHash = Animator.StringToHash("Dash");
        private static readonly int SlideHash = Animator.StringToHash("Slide");
        private static readonly int FireChargeStartHash = Animator.StringToHash("FireChargeStart");
        private static readonly int FireReleaseHash = Animator.StringToHash("FireRelease");
        private static readonly int MeleeHash = Animator.StringToHash("Melee");
        private static readonly int CounterHash = Animator.StringToHash("Counter");
        private static readonly int DeflectHash = Animator.StringToHash("Deflect");
        private static readonly int PerfectDeflectHash = Animator.StringToHash("PerfectDeflect");
        private static readonly int GrappleHash = Animator.StringToHash("Grapple");
        private static readonly int GrappleTraverseHash = Animator.StringToHash("GrappleTraverse");
        private static readonly int DodgeCounterHash = Animator.StringToHash("DodgeCounter");
        private static readonly int ThrowHash = Animator.StringToHash("Throw");
        private static readonly int HurtHash = Animator.StringToHash("Hurt");
        private static readonly int DefeatedHash = Animator.StringToHash("Defeated");

        public StrikerCharacter ActiveCharacter => activeCharacter;
        public GameObject ActiveVisualRoot => activeVisualRoot;
        public Animator ActiveAnimator => activeAnimator;

        private void Reset()
        {
            motor = GetComponent<NovaMotor2D>();
            combat = GetComponent<NovaCombatController>();
        }

        private void Awake()
        {
            if (!motor)
                motor = GetComponent<NovaMotor2D>();

            if (!combat)
                combat = GetComponent<NovaCombatController>();

            ResolveAnimators();
            SyncCharacter(true);
        }

        private void OnEnable()
        {
            GameplayEventHub.CueRaised += OnGameplayCue;
        }

        private void OnDisable()
        {
            GameplayEventHub.CueRaised -= OnGameplayCue;
        }

        private void Update()
        {
            SyncCharacter(false);
            UpdateFacing();
            UpdateAnimatorState();
        }

        private void ResolveAnimators()
        {
            if (!novaAnimator && novaVisualRoot)
            {
                novaAnimator =
                    novaVisualRoot.GetComponentInChildren<Animator>(true);
            }

            if (!echoAnimator && echoVisualRoot)
            {
                echoAnimator =
                    echoVisualRoot.GetComponentInChildren<Animator>(true);
            }
        }

        private void SyncCharacter(bool force)
        {
            StrikerCharacter desired =
                combat
                    ? combat.Character
                    : StrikerCharacter.Nova;

            if (!force && desired == activeCharacter)
                return;

            activeCharacter = desired;

            if (novaVisualRoot)
            {
                novaVisualRoot.SetActive(
                    activeCharacter == StrikerCharacter.Nova
                );
            }

            if (echoVisualRoot)
            {
                echoVisualRoot.SetActive(
                    activeCharacter == StrikerCharacter.Echo
                );
            }

            activeVisualRoot =
                activeCharacter == StrikerCharacter.Echo
                    ? echoVisualRoot
                    : novaVisualRoot;

            activeAnimator =
                activeCharacter == StrikerCharacter.Echo
                    ? echoAnimator
                    : novaAnimator;

            CacheAnimatorParameters();

            SetInteger(
                CharacterHash,
                (int)activeCharacter
            );
        }

        private void CacheAnimatorParameters()
        {
            animatorParameters.Clear();

            if (!activeAnimator)
                return;

            foreach (AnimatorControllerParameter parameter in
                     activeAnimator.parameters)
            {
                animatorParameters.Add(parameter.nameHash);
            }
        }

        private void UpdateFacing()
        {
            if (
                !rotateVisualForFacing ||
                !activeVisualRoot ||
                !motor
            )
            {
                return;
            }

            float y =
                motor.Facing < 0
                    ? facingLeftY
                    : facingRightY;

            activeVisualRoot.transform.localRotation =
                Quaternion.Euler(0f, y, 0f);
        }

        private void UpdateAnimatorState()
        {
            if (!activeAnimator || !motor)
                return;

            Vector2 velocity = motor.Velocity;
            Vector2 aim = motor.AimDirection;

            SetFloat(
                SpeedHash,
                Mathf.Abs(velocity.x)
            );
            SetFloat(VelocityXHash, velocity.x);
            SetFloat(VelocityYHash, velocity.y);
            SetFloat(AimXHash, aim.x);
            SetFloat(AimYHash, aim.y);
            SetFloat(FacingHash, motor.Facing);

            SetBool(GroundedHash, motor.Grounded);
            SetBool(CrouchingHash, motor.IsCrouching);
            SetBool(WallSlidingHash, motor.IsWallSliding);
            SetBool(DashingHash, motor.IsDashing);
            SetBool(SlidingHash, motor.IsSliding);
            SetBool(
                GrappleTraversalHash,
                motor.IsGrapplingTraversal
            );

            SetInteger(
                DashTierHash,
                motor.IsSliding
                    ? (int)motor.ActiveSlideTier
                    : (int)motor.ActiveDashTier
            );

            if (!combat)
                return;

            SetBool(
                CounteringHash,
                combat.IsCountering
            );
            SetFloat(
                FireChargeHash,
                combat.FireCharge
            );
            SetInteger(
                MeleeStepHash,
                combat.MeleeStep
            );
            SetInteger(
                CounterModeHash,
                (int)combat.CurrentCounterMode
            );
            SetInteger(
                CharacterHash,
                (int)activeCharacter
            );
        }

        private void OnGameplayCue(GameplayCue cue)
        {
            if (
                !motor ||
                cue.ActorId != motor.PlayerId
            )
            {
                return;
            }

            switch (cue.Type)
            {
                case GameplayCueType.Jump:
                    Trigger(JumpHash);
                    break;

                case GameplayCueType.WallJump:
                    Trigger(WallJumpHash);
                    break;

                case GameplayCueType.DashStarted:
                    SetInteger(DashTierHash, cue.Tier);
                    Trigger(DashHash);
                    break;

                case GameplayCueType.SlideStarted:
                    SetInteger(DashTierHash, cue.Tier);
                    Trigger(SlideHash);
                    break;

                case GameplayCueType.FireChargeStarted:
                    Trigger(FireChargeStartHash);
                    break;

                case GameplayCueType.FireChargeReleased:
                    Trigger(FireReleaseHash);
                    break;

                case GameplayCueType.MeleeStarted:
                    SetInteger(MeleeStepHash, cue.Tier);
                    Trigger(MeleeHash);
                    break;

                case GameplayCueType.CounterStarted:
                    SetInteger(CounterModeHash, cue.Tier);
                    Trigger(CounterHash);
                    break;

                case GameplayCueType.CounterDeflect:
                    Trigger(DeflectHash);
                    break;

                case GameplayCueType.PerfectParry:
                    Trigger(PerfectDeflectHash);
                    break;

                case GameplayCueType.CounterGrapple:
                    Trigger(GrappleHash);
                    break;

                case GameplayCueType.CounterGrappleTraversal:
                    Trigger(GrappleTraverseHash);
                    break;

                case GameplayCueType.CounterDodge:
                    Trigger(DodgeCounterHash);
                    break;

                case GameplayCueType.CounterThrow:
                    Trigger(ThrowHash);
                    break;

                case GameplayCueType.DamageTaken:
                    Trigger(HurtHash);
                    break;

                case GameplayCueType.Defeated:
                    Trigger(DefeatedHash);
                    break;
            }
        }

        private void SetBool(int hash, bool value)
        {
            if (
                activeAnimator &&
                animatorParameters.Contains(hash)
            )
            {
                activeAnimator.SetBool(hash, value);
            }
        }

        private void SetFloat(int hash, float value)
        {
            if (
                activeAnimator &&
                animatorParameters.Contains(hash)
            )
            {
                activeAnimator.SetFloat(hash, value);
            }
        }

        private void SetInteger(int hash, int value)
        {
            if (
                activeAnimator &&
                animatorParameters.Contains(hash)
            )
            {
                activeAnimator.SetInteger(hash, value);
            }
        }

        private void Trigger(int hash)
        {
            if (
                activeAnimator &&
                animatorParameters.Contains(hash)
            )
            {
                activeAnimator.SetTrigger(hash);
            }
        }
    }
}
