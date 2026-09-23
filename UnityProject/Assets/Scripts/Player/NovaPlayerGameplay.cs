using NovaStriker.Input;
using UnityEngine;

namespace NovaStriker.Player
{
    /// <summary>
    /// Single input entry point for one local player.
    /// Platform-specific adapters submit frame input here. This component
    /// latches button edges until the next physics step and fans the resulting
    /// snapshot into movement and combat before their FixedUpdate methods run.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public sealed class NovaPlayerGameplay : MonoBehaviour
    {
        [SerializeField] private NovaMotor2D motor;
        [SerializeField] private NovaCombatController combat;
        [SerializeField] private StrikerPlayerIdentity identity;

        private PlayerInputState latest;
        private bool hasInput;

        private void Reset()
        {
            motor = GetComponent<NovaMotor2D>();
            combat = GetComponent<NovaCombatController>();
            identity = GetComponent<StrikerPlayerIdentity>();
        }

        private void Awake()
        {
            if (!motor)
                motor = GetComponent<NovaMotor2D>();

            if (!combat)
                combat = GetComponent<NovaCombatController>();

            if (!identity)
                identity = GetComponent<StrikerPlayerIdentity>();
        }

        /// <summary>
        /// Submit the latest frame input. Edge fields are OR-latched so a
        /// short press is not lost when rendering is faster than physics.
        /// </summary>
        public void SetInput(PlayerInputState state)
        {
            latest.Move = state.Move;
            latest.Aim = state.Aim;

            latest.JumpHeld = state.JumpHeld;
            latest.FireHeld = state.FireHeld;
            latest.DashHeld = state.DashHeld;
            latest.AbilityHeld = state.AbilityHeld;

            latest.JumpPressed |= state.JumpPressed;
            latest.FireReleased |= state.FireReleased;
            latest.DashReleased |= state.DashReleased;
            latest.MeleePressed |= state.MeleePressed;
            latest.CounterPressed |= state.CounterPressed;
            latest.AbilityPressed |= state.AbilityPressed;
            latest.WeaponCyclePressed |= state.WeaponCyclePressed;
            latest.GuardianCyclePressed |= state.GuardianCyclePressed;
            latest.SyncPressed |= state.SyncPressed;

            hasInput = true;
        }

        private void FixedUpdate()
        {
            PlayerInputState step = hasInput
                ? latest
                : PlayerInputState.Neutral;

            if (motor)
                motor.SetInput(step);

            if (combat)
                combat.SetInput(step);

            identity?.SubmitTeamInput(step);

            ClearTransientEdges();
        }

        public void ClearInput()
        {
            latest = PlayerInputState.Neutral;
            hasInput = false;

            if (motor)
                motor.SetInput(latest);

            if (combat)
                combat.SetInput(latest);
        }

        private void ClearTransientEdges()
        {
            latest.JumpPressed = false;
            latest.FireReleased = false;
            latest.DashReleased = false;
            latest.MeleePressed = false;
            latest.CounterPressed = false;
            latest.AbilityPressed = false;
            latest.WeaponCyclePressed = false;
            latest.GuardianCyclePressed = false;
            latest.SyncPressed = false;
        }
    }
}
