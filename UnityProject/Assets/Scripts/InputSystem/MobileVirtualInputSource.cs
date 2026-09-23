using NovaStriker.Input;
using UnityEngine;

namespace NovaStriker.InputSystemIntegration
{
    /// <summary>
    /// UI-agnostic mobile/touch input buffer. Final on-screen controls can call
    /// these public methods from Unity UI/EventSystem events without gameplay
    /// knowing anything about the visual control layout.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MobileVirtualInputSource : MonoBehaviour
    {
        private Vector2 move;
        private Vector2 aim;

        private bool jumpHeld;
        private bool fireHeld;
        private bool dashHeld;
        private bool abilityHeld;

        private bool jumpPressed;
        private bool fireReleased;
        private bool dashReleased;
        private bool meleePressed;
        private bool counterPressed;
        private bool abilityPressed;
        private bool ability1Pressed;
        private bool ability2Pressed;
        private bool ability3Pressed;
        private bool ultimatePressed;
        private bool weaponCyclePressed;
        private bool guardianCyclePressed;
        private bool guardianActivatePressed;
        private bool syncPressed;

        public Vector2 Move => move;
        public Vector2 Aim => aim;

        public void SetMove(Vector2 value)
        {
            move =
                Vector2.ClampMagnitude(
                    value,
                    1f
                );
        }

        public void SetAim(Vector2 value)
        {
            aim =
                Vector2.ClampMagnitude(
                    value,
                    1f
                );
        }

        public void SetJumpHeld(bool value)
        {
            if (value && !jumpHeld)
                jumpPressed = true;

            jumpHeld = value;
        }

        public void SetFireHeld(bool value)
        {
            if (!value && fireHeld)
                fireReleased = true;

            fireHeld = value;
        }

        public void SetDashHeld(bool value)
        {
            if (!value && dashHeld)
                dashReleased = true;

            dashHeld = value;
        }

        public void SetAbilityHeld(bool value)
        {
            if (value && !abilityHeld)
                abilityPressed = true;

            abilityHeld = value;
        }

        public void PressMelee() => meleePressed = true;
        public void PressCounter() => counterPressed = true;
        public void PressAbility1() => ability1Pressed = true;
        public void PressAbility2() => ability2Pressed = true;
        public void PressAbility3() => ability3Pressed = true;
        public void PressUltimate() => ultimatePressed = true;
        public void PressWeaponCycle() => weaponCyclePressed = true;
        public void PressGuardianCycle() => guardianCyclePressed = true;
        public void PressGuardianActivate() => guardianActivatePressed = true;
        public void PressSync() => syncPressed = true;

        public PlayerInputState ConsumeSnapshot()
        {
            PlayerInputState state = new()
            {
                Move = move,
                Aim = aim,

                JumpHeld = jumpHeld,
                JumpPressed = jumpPressed,

                FireHeld = fireHeld,
                FireReleased = fireReleased,

                DashHeld = dashHeld,
                DashReleased = dashReleased,

                MeleePressed = meleePressed,
                CounterPressed = counterPressed,
                AbilityHeld = abilityHeld,
                AbilityPressed = abilityPressed,
                Ability1Pressed = ability1Pressed,
                Ability2Pressed = ability2Pressed,
                Ability3Pressed = ability3Pressed,
                UltimatePressed = ultimatePressed,

                WeaponCyclePressed = weaponCyclePressed,
                GuardianCyclePressed = guardianCyclePressed,
                GuardianActivatePressed = guardianActivatePressed,
                SyncPressed = syncPressed
            };

            ClearEdges();
            return state;
        }

        public void ClearAll()
        {
            move = Vector2.zero;
            aim = Vector2.zero;
            jumpHeld = false;
            fireHeld = false;
            dashHeld = false;
            abilityHeld = false;
            ClearEdges();
        }

        private void ClearEdges()
        {
            jumpPressed = false;
            fireReleased = false;
            dashReleased = false;
            meleePressed = false;
            counterPressed = false;
            abilityPressed = false;
            ability1Pressed = false;
            ability2Pressed = false;
            ability3Pressed = false;
            ultimatePressed = false;
            weaponCyclePressed = false;
            guardianCyclePressed = false;
            guardianActivatePressed = false;
            syncPressed = false;
        }
    }
}
