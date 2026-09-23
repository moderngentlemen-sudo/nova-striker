using NovaStriker.Core;
using UnityEngine;

namespace NovaStriker.Presentation
{
    /// <summary>
    /// Versioned, gameplay-facing presentation contract for production assets.
    ///
    /// Gameplay remains authoritative. Blender rigs, Animator Controllers,
    /// VFX, audio and haptic implementations consume these identifiers rather
    /// than introducing timing or hit authority of their own.
    /// </summary>
    public static class GameplayPresentationContract
    {
        public const string Version = "1.0.0-preblender";

        public static class AnimatorParameters
        {
            public const string Speed = "Speed";
            public const string VelocityX = "VelocityX";
            public const string VelocityY = "VelocityY";
            public const string AimX = "AimX";
            public const string AimY = "AimY";
            public const string Facing = "Facing";
            public const string Grounded = "Grounded";
            public const string Crouching = "Crouching";
            public const string WallSliding = "WallSliding";
            public const string Dashing = "Dashing";
            public const string Sliding = "Sliding";
            public const string GrappleTraversal = "GrappleTraversal";
            public const string Countering = "Countering";
            public const string FireCharge = "FireCharge";
            public const string DashTier = "DashTier";
            public const string MeleeStep = "MeleeStep";
            public const string CounterMode = "CounterMode";
            public const string Character = "Character";

            public const string Jump = "Jump";
            public const string WallJump = "WallJump";
            public const string Dash = "Dash";
            public const string Slide = "Slide";
            public const string FireChargeStart = "FireChargeStart";
            public const string FireRelease = "FireRelease";
            public const string Melee = "Melee";
            public const string Counter = "Counter";
            public const string Deflect = "Deflect";
            public const string PerfectDeflect = "PerfectDeflect";
            public const string Grapple = "Grapple";
            public const string GrappleTraverse = "GrappleTraverse";
            public const string DodgeCounter = "DodgeCounter";
            public const string Throw = "Throw";
            public const string Hurt = "Hurt";
            public const string Defeated = "Defeated";

            public static readonly int SpeedHash = Animator.StringToHash(Speed);
            public static readonly int VelocityXHash = Animator.StringToHash(VelocityX);
            public static readonly int VelocityYHash = Animator.StringToHash(VelocityY);
            public static readonly int AimXHash = Animator.StringToHash(AimX);
            public static readonly int AimYHash = Animator.StringToHash(AimY);
            public static readonly int FacingHash = Animator.StringToHash(Facing);
            public static readonly int GroundedHash = Animator.StringToHash(Grounded);
            public static readonly int CrouchingHash = Animator.StringToHash(Crouching);
            public static readonly int WallSlidingHash = Animator.StringToHash(WallSliding);
            public static readonly int DashingHash = Animator.StringToHash(Dashing);
            public static readonly int SlidingHash = Animator.StringToHash(Sliding);
            public static readonly int GrappleTraversalHash = Animator.StringToHash(GrappleTraversal);
            public static readonly int CounteringHash = Animator.StringToHash(Countering);
            public static readonly int FireChargeHash = Animator.StringToHash(FireCharge);
            public static readonly int DashTierHash = Animator.StringToHash(DashTier);
            public static readonly int MeleeStepHash = Animator.StringToHash(MeleeStep);
            public static readonly int CounterModeHash = Animator.StringToHash(CounterMode);
            public static readonly int CharacterHash = Animator.StringToHash(Character);

            public static readonly int JumpHash = Animator.StringToHash(Jump);
            public static readonly int WallJumpHash = Animator.StringToHash(WallJump);
            public static readonly int DashHash = Animator.StringToHash(Dash);
            public static readonly int SlideHash = Animator.StringToHash(Slide);
            public static readonly int FireChargeStartHash = Animator.StringToHash(FireChargeStart);
            public static readonly int FireReleaseHash = Animator.StringToHash(FireRelease);
            public static readonly int MeleeHash = Animator.StringToHash(Melee);
            public static readonly int CounterHash = Animator.StringToHash(Counter);
            public static readonly int DeflectHash = Animator.StringToHash(Deflect);
            public static readonly int PerfectDeflectHash = Animator.StringToHash(PerfectDeflect);
            public static readonly int GrappleHash = Animator.StringToHash(Grapple);
            public static readonly int GrappleTraverseHash = Animator.StringToHash(GrappleTraverse);
            public static readonly int DodgeCounterHash = Animator.StringToHash(DodgeCounter);
            public static readonly int ThrowHash = Animator.StringToHash(Throw);
            public static readonly int HurtHash = Animator.StringToHash(Hurt);
            public static readonly int DefeatedHash = Animator.StringToHash(Defeated);

            public static readonly string[] AllNames =
            {
                Speed,
                VelocityX,
                VelocityY,
                AimX,
                AimY,
                Facing,
                Grounded,
                Crouching,
                WallSliding,
                Dashing,
                Sliding,
                GrappleTraversal,
                Countering,
                FireCharge,
                DashTier,
                MeleeStep,
                CounterMode,
                Character,
                Jump,
                WallJump,
                Dash,
                Slide,
                FireChargeStart,
                FireRelease,
                Melee,
                Counter,
                Deflect,
                PerfectDeflect,
                Grapple,
                GrappleTraverse,
                DodgeCounter,
                Throw,
                Hurt,
                Defeated
            };
        }

        public static class Sockets
        {
            public const string Root = "socket_root";
            public const string CameraFocus = "socket_camera_focus";
            public const string Head = "socket_head";
            public const string Chest = "socket_chest";
            public const string Back = "socket_back";
            public const string LeftHand = "socket_hand_l";
            public const string RightHand = "socket_hand_r";
            public const string PrimaryWeapon = "socket_weapon_primary";
            public const string PrimaryMuzzle = "socket_muzzle_primary";
            public const string SecondaryMuzzle = "socket_muzzle_secondary";
            public const string AbilityOrigin = "socket_ability_origin";
            public const string LeftFoot = "socket_foot_l";
            public const string RightFoot = "socket_foot_r";

            public static readonly string[] AllNames =
            {
                Root,
                CameraFocus,
                Head,
                Chest,
                Back,
                LeftHand,
                RightHand,
                PrimaryWeapon,
                PrimaryMuzzle,
                SecondaryMuzzle,
                AbilityOrigin,
                LeftFoot,
                RightFoot
            };
        }

        public static string EventId(GameplayCueType cue)
        {
            return "ns.gameplay." + cue;
        }

        public static string VfxId(GameplayCueType cue)
        {
            return "vfx." + EventId(cue);
        }

        public static string AudioId(GameplayCueType cue)
        {
            return "audio." + EventId(cue);
        }

        /// <summary>
        /// Stable semantic haptic route. Native platform adapters translate
        /// these ids to device-specific patterns without changing gameplay.
        /// Empty means no default haptic is required.
        /// </summary>
        public static string HapticId(GameplayCueType cue)
        {
            return cue switch
            {
                GameplayCueType.Jump => "haptic.movement.light",
                GameplayCueType.WallJump => "haptic.movement.medium",
                GameplayCueType.DashStarted => "haptic.movement.medium",
                GameplayCueType.SlideStarted => "haptic.movement.light",
                GameplayCueType.DashHit => "haptic.impact.medium",
                GameplayCueType.SlideHit => "haptic.impact.medium",
                GameplayCueType.FireChargeReleased => "haptic.weapon.release",
                GameplayCueType.MeleeHit => "haptic.impact.medium",
                GameplayCueType.CounterDeflect => "haptic.counter.medium",
                GameplayCueType.CounterGrapple => "haptic.counter.medium",
                GameplayCueType.CounterGrappleTraversal => "haptic.traversal.grapple",
                GameplayCueType.CounterDodge => "haptic.counter.light",
                GameplayCueType.CounterThrow => "haptic.impact.heavy",
                GameplayCueType.SuitAbilityResolved => "haptic.ability.heavy",
                GameplayCueType.SuitUltimateStarted => "haptic.ultimate.sustain",
                GameplayCueType.GuardianAbilityResolved => "haptic.ability.heavy",
                GameplayCueType.TeamSyncResolved => "haptic.team_sync.heavy",
                GameplayCueType.PickupCollected => "haptic.ui.reward",
                GameplayCueType.BossPhaseChanged => "haptic.boss.phase",
                GameplayCueType.GuardianWeakPointOpened => "haptic.boss.weakpoint",
                GameplayCueType.ParrySuccess => "haptic.counter.medium",
                GameplayCueType.PerfectParry => "haptic.counter.perfect",
                GameplayCueType.DamageTaken => "haptic.damage.medium",
                GameplayCueType.ShieldBroken => "haptic.break.shield",
                GameplayCueType.ArmorBroken => "haptic.break.armor",
                GameplayCueType.GuardBroken => "haptic.break.guard",
                GameplayCueType.Defeated => "haptic.damage.defeat",
                GameplayCueType.Revived => "haptic.revive.complete",
                _ => string.Empty
            };
        }
    }
}
