using NovaStriker.Input;
using NovaStriker.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NovaStriker.InputSystemIntegration
{
    /// <summary>
    /// Unity Input System adapter for the Nova Striker greybox.
    ///
    /// Keyboard:
    /// WASD move, arrows aim, Space jump, J fire, K dash,
    /// U melee, I parry, F ability, Q weapon, R Guardian, G Sync.
    ///
    /// Gamepad / DualShock / DualSense:
    /// Left stick move, right stick aim,
    /// Cross/South jump, R2 fire, L2 dash,
    /// Square/West melee, Circle/East parry,
    /// L1 ability, R1 or Triangle/North weapon cycle,
    /// L3 or D-pad Up Guardian cycle, R3 Sync.
    /// </summary>
    [DefaultExecutionOrder(-300)]
    public sealed class NovaInputSystemAdapter : MonoBehaviour
    {
        [SerializeField] private NovaPlayerGameplay player;

        private InputActionMap map;
        private InputAction move;
        private InputAction aim;
        private InputAction jump;
        private InputAction fire;
        private InputAction dash;
        private InputAction melee;
        private InputAction parry;
        private InputAction ability;
        private InputAction weaponCycle;
        private InputAction guardianCycle;
        private InputAction sync;

        private void Reset()
        {
            player = GetComponent<NovaPlayerGameplay>();
        }

        private void Awake()
        {
            if (!player)
                player = GetComponent<NovaPlayerGameplay>();

            BuildActions();
        }

        private void OnEnable()
        {
            if (map == null)
                BuildActions();

            map.Enable();
        }

        private void OnDisable()
        {
            map?.Disable();

            if (player)
                player.ClearInput();
        }

        private void OnDestroy()
        {
            map?.Dispose();
        }

        private void Update()
        {
            if (!player || map == null)
                return;

            Vector2 moveValue = move.ReadValue<Vector2>();
            Vector2 aimValue = aim.ReadValue<Vector2>();

            if (moveValue.sqrMagnitude > 1f)
                moveValue.Normalize();

            if (aimValue.sqrMagnitude > 1f)
                aimValue.Normalize();

            player.SetInput(new PlayerInputState
            {
                Move = moveValue,
                Aim = aimValue,

                JumpHeld = jump.IsPressed(),
                JumpPressed = jump.WasPressedThisFrame(),

                FireHeld = fire.IsPressed(),
                FireReleased = fire.WasReleasedThisFrame(),

                DashHeld = dash.IsPressed(),
                DashReleased = dash.WasReleasedThisFrame(),

                MeleePressed = melee.WasPressedThisFrame(),
                ParryPressed = parry.WasPressedThisFrame(),
                AbilityPressed = ability.WasPressedThisFrame(),

                WeaponCyclePressed = weaponCycle.WasPressedThisFrame(),
                GuardianCyclePressed = guardianCycle.WasPressedThisFrame(),
                SyncPressed = sync.WasPressedThisFrame()
            });
        }

        private void BuildActions()
        {
            map?.Dispose();
            map = new InputActionMap("Nova");

            move = map.AddAction(
                "Move",
                InputActionType.Value,
                expectedControlType: "Vector2"
            );

            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            move.AddBinding("<Gamepad>/leftStick");

            aim = map.AddAction(
                "Aim",
                InputActionType.Value,
                expectedControlType: "Vector2"
            );

            aim.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");

            aim.AddBinding("<Gamepad>/rightStick");

            jump = AddButton(
                "Jump",
                "<Keyboard>/space",
                "<Gamepad>/buttonSouth"
            );

            fire = AddButton(
                "Fire",
                "<Keyboard>/j",
                "<Gamepad>/rightTrigger"
            );

            dash = AddButton(
                "Dash",
                "<Keyboard>/k",
                "<Gamepad>/leftTrigger"
            );

            melee = AddButton(
                "Melee",
                "<Keyboard>/u",
                "<Gamepad>/buttonWest"
            );

            parry = AddButton(
                "Parry",
                "<Keyboard>/i",
                "<Gamepad>/buttonEast"
            );

            ability = AddButton(
                "Ability",
                "<Keyboard>/f",
                "<Gamepad>/leftShoulder"
            );

            weaponCycle = AddButton(
                "Weapon Cycle",
                "<Keyboard>/q",
                "<Gamepad>/rightShoulder"
            );
            weaponCycle.AddBinding("<Gamepad>/buttonNorth");

            guardianCycle = AddButton(
                "Guardian Cycle",
                "<Keyboard>/r",
                "<Gamepad>/leftStickPress"
            );
            guardianCycle.AddBinding("<Gamepad>/dpad/up");

            sync = AddButton(
                "Sync",
                "<Keyboard>/g",
                "<Gamepad>/rightStickPress"
            );
        }

        private InputAction AddButton(
            string name,
            string keyboardBinding,
            string gamepadBinding)
        {
            InputAction action = map.AddAction(
                name,
                InputActionType.Button,
                expectedControlType: "Button"
            );

            action.AddBinding(keyboardBinding);
            action.AddBinding(gamepadBinding);

            return action;
        }
    }
}
