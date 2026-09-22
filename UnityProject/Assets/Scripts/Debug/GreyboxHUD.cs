using NovaStriker.Combat;
using NovaStriker.Player;
using UnityEngine;

namespace NovaStriker.Debugging
{
    /// <summary>
    /// Temporary immediate-mode HUD for mechanics validation.
    /// Production UI should replace this later.
    /// </summary>
    public sealed class GreyboxHUD : MonoBehaviour
    {
        [SerializeField] private NovaMotor2D motor;
        [SerializeField] private NovaCombatController combat;
        [SerializeField] private Damageable2D playerHealth;

        private GUIStyle header;
        private GUIStyle body;

        private void Awake()
        {
            header = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            body = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                normal = { textColor = new Color(0.88f, 0.94f, 1f) }
            };
        }

        public void Configure(
            NovaMotor2D motorController,
            NovaCombatController combatController,
            Damageable2D health)
        {
            motor = motorController;
            combat = combatController;
            playerHealth = health;
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(14, 14, 430, 212), GUIContent.none);

            GUI.Label(
                new Rect(28, 24, 390, 28),
                "NOVA STRIKER — UNITY GREYBOX",
                header
            );

            string state =
                $"Grounded: {motor?.Grounded ?? false}    " +
                $"Crouch: {motor?.IsCrouching ?? false}    " +
                $"Dash: {motor?.ActiveDashTier.ToString() ?? "-"}\n" +
                $"Dash charge: {motor?.DashCharge ?? 0f:0.00}s    " +
                $"Fire charge: {combat?.FireCharge ?? 0f:0.00}s\n" +
                $"Parry active: {combat?.IsParryActive ?? false}    " +
                $"Perfect: {combat?.IsPerfectParryWindow ?? false}    " +
                $"Melee: {combat?.MeleeStep ?? 0}\n" +
                $"Health: {(playerHealth ? playerHealth.Health : 0f):0}";

            GUI.Label(
                new Rect(28, 58, 390, 82),
                state,
                body
            );

            GUI.Label(
                new Rect(28, 142, 400, 72),
                "WASD / Left Stick: move    Arrows / Right Stick: aim\n" +
                "Space / Cross: jump    J / R2: fire    K / L2: dash\n" +
                "U / Square: melee    I / Circle: parry\n" +
                "Crouch + Jump: drop through one-way platform",
                body
            );
        }
    }
}
