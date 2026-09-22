using NovaStriker.Input;
using UnityEngine;

namespace NovaStriker.Player
{
    /// <summary>
    /// Single input entry point for one local player.
    /// Platform-specific input adapters feed this component; it fans the same
    /// per-player snapshot into movement and combat.
    /// </summary>
    public sealed class NovaPlayerGameplay : MonoBehaviour
    {
        [SerializeField] private NovaMotor2D motor;
        [SerializeField] private NovaCombatController combat;

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
        }

        public void SetInput(PlayerInputState state)
        {
            if (motor)
                motor.SetInput(state);

            if (combat)
                combat.SetInput(state);
        }

        public void ClearInput()
        {
            SetInput(PlayerInputState.Neutral);
        }
    }
}
