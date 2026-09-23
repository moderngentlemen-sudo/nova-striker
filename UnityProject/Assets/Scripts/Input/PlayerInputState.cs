using UnityEngine;

namespace NovaStriker.Input
{
    /// <summary>
    /// Presentation-agnostic per-player input snapshot.
    /// An Input System adapter can populate this without coupling gameplay code
    /// to a specific controller, keyboard, touch, or platform API.
    ///
    /// Coordinate convention follows Unity world space:
    /// +X = right, +Y = up. Therefore crouch/down is Move.y < 0.
    /// </summary>
    public struct PlayerInputState
    {
        public Vector2 Move;
        public Vector2 Aim;

        public bool JumpHeld;
        public bool JumpPressed;

        public bool FireHeld;
        public bool FireReleased;

        public bool DashHeld;
        public bool DashReleased;

        public bool MeleePressed;
        public bool CounterPressed;
        public bool AbilityPressed;

        public bool WeaponCyclePressed;
        public bool GuardianCyclePressed;
        public bool SyncPressed;

        public static PlayerInputState Neutral => new()
        {
            Move = Vector2.zero,
            Aim = Vector2.right
        };
    }
}
