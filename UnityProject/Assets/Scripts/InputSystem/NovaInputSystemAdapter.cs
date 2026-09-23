using NovaStriker.Input;
using NovaStriker.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NovaStriker.InputSystemIntegration
{
    /// <summary>
    /// Per-player local input adapter.
    ///
    /// Each adapter reads at most one assigned Gamepad. Player 1 may also use
    /// the keyboard. This prevents one physical controller from driving more
    /// than one Striker and keeps device identity stable across reconnects.
    /// </summary>
    [DefaultExecutionOrder(-300)]
    [DisallowMultipleComponent]
    public sealed class NovaInputSystemAdapter : MonoBehaviour
    {
        [SerializeField] private NovaPlayerGameplay player;

        [Header("Local Player")]
        [SerializeField, Range(0, 3)] private int playerSlot;
        [SerializeField] private bool keyboardEnabled = true;

        [Header("Assigned Gamepad")]
        [SerializeField] private int assignedDeviceId = -1;

        private Gamepad assignedGamepad;

        public int PlayerSlot => playerSlot;
        public bool KeyboardEnabled => keyboardEnabled;
        public int AssignedDeviceId => assignedDeviceId;
        public bool HasAssignedGamepad =>
            ResolveAssignedGamepad() != null;

        private void Reset()
        {
            player = GetComponent<NovaPlayerGameplay>();
        }

        private void Awake()
        {
            if (!player)
                player = GetComponent<NovaPlayerGameplay>();

            ResolveAssignedGamepad();
        }

        private void OnDisable()
        {
            if (player)
                player.ClearInput();
        }

        public void ConfigureSlot(
            int slot,
            bool allowKeyboard)
        {
            playerSlot = Mathf.Clamp(slot, 0, 3);
            keyboardEnabled = allowKeyboard;
        }

        public void AssignGamepad(Gamepad gamepad)
        {
            assignedGamepad = gamepad;
            assignedDeviceId =
                gamepad ? gamepad.deviceId : -1;
        }

        public void ClearGamepadAssignment()
        {
            assignedGamepad = null;
            assignedDeviceId = -1;
        }

        public void RefreshAssignedDevice()
        {
            assignedGamepad = null;
            ResolveAssignedGamepad();
        }

        private void Update()
        {
            if (!player)
                return;

            PlayerInputState state = new()
            {
                Move = Vector2.zero,
                Aim = Vector2.zero
            };

            if (keyboardEnabled)
                ReadKeyboard(ref state);

            Gamepad gamepad =
                ResolveAssignedGamepad();

            if (gamepad)
                ReadGamepad(gamepad, ref state);

            if (state.Move.sqrMagnitude > 1f)
                state.Move.Normalize();

            if (state.Aim.sqrMagnitude > 1f)
                state.Aim.Normalize();

            player.SetInput(state);
        }

        private Gamepad ResolveAssignedGamepad()
        {
            if (
                assignedGamepad &&
                assignedGamepad.added &&
                (
                    assignedDeviceId < 0 ||
                    assignedGamepad.deviceId == assignedDeviceId
                )
            )
            {
                return assignedGamepad;
            }

            assignedGamepad = null;

            if (assignedDeviceId < 0)
                return null;

            for (int i = 0; i < Gamepad.all.Count; i++)
            {
                Gamepad candidate = Gamepad.all[i];

                if (
                    candidate &&
                    candidate.deviceId == assignedDeviceId
                )
                {
                    assignedGamepad = candidate;
                    break;
                }
            }

            return assignedGamepad;
        }

        private static void ReadKeyboard(
            ref PlayerInputState state)
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
                return;

            Vector2 move = new(
                Axis(
                    keyboard.aKey.isPressed,
                    keyboard.dKey.isPressed
                ),
                Axis(
                    keyboard.sKey.isPressed,
                    keyboard.wKey.isPressed
                )
            );

            Vector2 aim = new(
                Axis(
                    keyboard.leftArrowKey.isPressed,
                    keyboard.rightArrowKey.isPressed
                ),
                Axis(
                    keyboard.downArrowKey.isPressed,
                    keyboard.upArrowKey.isPressed
                )
            );

            if (move.sqrMagnitude >= state.Move.sqrMagnitude)
                state.Move = move;

            if (aim.sqrMagnitude >= state.Aim.sqrMagnitude)
                state.Aim = aim;

            state.JumpHeld |= keyboard.spaceKey.isPressed;
            state.JumpPressed |= keyboard.spaceKey.wasPressedThisFrame;

            state.FireHeld |= keyboard.jKey.isPressed;
            state.FireReleased |= keyboard.jKey.wasReleasedThisFrame;

            state.DashHeld |= keyboard.kKey.isPressed;
            state.DashReleased |= keyboard.kKey.wasReleasedThisFrame;

            state.MeleePressed |= keyboard.uKey.wasPressedThisFrame;
            state.CounterPressed |= keyboard.iKey.wasPressedThisFrame;
            state.AbilityPressed |= keyboard.fKey.wasPressedThisFrame;

            state.WeaponCyclePressed |= keyboard.qKey.wasPressedThisFrame;
            state.GuardianCyclePressed |= keyboard.rKey.wasPressedThisFrame;
            state.SyncPressed |= keyboard.gKey.wasPressedThisFrame;
        }

        private static void ReadGamepad(
            Gamepad gamepad,
            ref PlayerInputState state)
        {
            Vector2 move =
                gamepad.leftStick.ReadValue();

            Vector2 aim =
                gamepad.rightStick.ReadValue();

            if (move.sqrMagnitude >= state.Move.sqrMagnitude)
                state.Move = move;

            if (aim.sqrMagnitude >= state.Aim.sqrMagnitude)
                state.Aim = aim;

            state.JumpHeld |=
                gamepad.buttonSouth.isPressed;
            state.JumpPressed |=
                gamepad.buttonSouth.wasPressedThisFrame;

            state.FireHeld |=
                gamepad.rightTrigger.isPressed;
            state.FireReleased |=
                gamepad.rightTrigger.wasReleasedThisFrame;

            state.DashHeld |=
                gamepad.leftTrigger.isPressed;
            state.DashReleased |=
                gamepad.leftTrigger.wasReleasedThisFrame;

            state.MeleePressed |=
                gamepad.buttonWest.wasPressedThisFrame;

            state.CounterPressed |=
                gamepad.buttonEast.wasPressedThisFrame;

            state.AbilityPressed |=
                gamepad.leftShoulder.wasPressedThisFrame;

            state.WeaponCyclePressed |=
                gamepad.rightShoulder.wasPressedThisFrame ||
                gamepad.buttonNorth.wasPressedThisFrame;

            state.GuardianCyclePressed |=
                gamepad.leftStickButton.wasPressedThisFrame ||
                gamepad.dpad.up.wasPressedThisFrame;

            state.SyncPressed |=
                gamepad.rightStickButton.wasPressedThisFrame;
        }

        private static float Axis(
            bool negative,
            bool positive)
        {
            return
                (positive ? 1f : 0f) -
                (negative ? 1f : 0f);
        }
    }
}
