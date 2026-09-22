using NovaStriker.Player;
using UnityEngine;

namespace NovaStriker.Input
{
    /// <summary>
    /// Development-only keyboard adapter matching the browser prototype:
    /// WASD move, arrows aim, J fire, K dash, Space jump,
    /// U melee, I context counter, F Guardian/assist, Q weapon, R Guardian, G Sync.
    ///
    /// This is intentionally separate from the eventual Unity Input System
    /// gamepad/touch adapter so mechanics can be tested before device setup.
    /// </summary>
    public sealed class NovaKeyboardDebugInput : MonoBehaviour
    {
        [SerializeField] private NovaPlayerGameplay player;

        private void Reset()
        {
            player = GetComponent<NovaPlayerGameplay>();
        }

        private void Awake()
        {
            if (!player)
                player = GetComponent<NovaPlayerGameplay>();
        }

        private void Update()
        {
            if (!player)
                return;

            Vector2 move = Vector2.zero;

            if (UnityEngine.Input.GetKey(KeyCode.A))
                move.x -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.D))
                move.x += 1f;
            if (UnityEngine.Input.GetKey(KeyCode.S))
                move.y -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.W))
                move.y += 1f;

            if (move.sqrMagnitude > 1f)
                move.Normalize();

            Vector2 aim = Vector2.zero;

            if (UnityEngine.Input.GetKey(KeyCode.LeftArrow))
                aim.x -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.RightArrow))
                aim.x += 1f;
            if (UnityEngine.Input.GetKey(KeyCode.DownArrow))
                aim.y -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.UpArrow))
                aim.y += 1f;

            if (aim.sqrMagnitude > 1f)
                aim.Normalize();

            player.SetInput(new PlayerInputState
            {
                Move = move,
                Aim = aim,

                JumpHeld = UnityEngine.Input.GetKey(KeyCode.Space),
                JumpPressed = UnityEngine.Input.GetKeyDown(KeyCode.Space),

                FireHeld = UnityEngine.Input.GetKey(KeyCode.J),
                FireReleased = UnityEngine.Input.GetKeyUp(KeyCode.J),

                DashHeld = UnityEngine.Input.GetKey(KeyCode.K),
                DashReleased = UnityEngine.Input.GetKeyUp(KeyCode.K),

                MeleePressed = UnityEngine.Input.GetKeyDown(KeyCode.U),
                CounterPressed = UnityEngine.Input.GetKeyDown(KeyCode.I),
                AbilityPressed = UnityEngine.Input.GetKeyDown(KeyCode.F),

                WeaponCyclePressed = UnityEngine.Input.GetKeyDown(KeyCode.Q),
                GuardianCyclePressed = UnityEngine.Input.GetKeyDown(KeyCode.R),
                SyncPressed = UnityEngine.Input.GetKeyDown(KeyCode.G)
            });
        }

        private void OnDisable()
        {
            if (player)
                player.ClearInput();
        }
    }
}
