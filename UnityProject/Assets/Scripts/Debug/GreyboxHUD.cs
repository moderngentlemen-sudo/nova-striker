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
            EnsureStyles();

            GUI.Box(new Rect(14, 14, 455, 268), GUIContent.none);

            GUI.Label(
                new Rect(28, 24, 390, 28),
                "NOVA STRIKER — UNITY GREYBOX",
                header
            );

            float dashCharge = motor ? motor.DashCharge : 0f;
            float fireCharge = combat ? combat.FireCharge : 0f;
            float health = playerHealth ? playerHealth.Health : 0f;
            string dashTier = motor
                ? motor.ActiveDashTier.ToString()
                : "-";

            string character =
                combat
                    ? combat.Character.ToString()
                    : "-";

            string state =
                $"Character: {character}    " +
                $"Counter: {(combat ? combat.CurrentCounterMode.ToString() : "-")}\n" +
                $"Grapple lock: {(combat ? combat.CurrentGrappleLockKind.ToString() : "-")}\n" +
                $"Grounded: {(motor && motor.Grounded)}    " +
                $"Wall slide: {(motor && motor.IsWallSliding)}    " +
                $"Wall: {(motor ? motor.WallDirection : 0)}\n" +
                $"Crouch: {(motor && motor.IsCrouching)}    " +
                $"Dash tier: {dashTier}\n" +
                $"Dash charge: {dashCharge:0.00}s    " +
                $"Fire charge: {fireCharge:0.00}s\n" +
                $"Deflect active: {(combat && combat.IsParryActive)}    " +
                $"Perfect: {(combat && combat.IsPerfectParryWindow)}\n" +
                $"Melee: {(combat ? combat.MeleeStep : 0)}\n" +
                $"Health: {health:0}";

            GUI.Label(
                new Rect(28, 58, 415, 148),
                state,
                body
            );

            GUI.Label(
                new Rect(28, 210, 420, 58),
                "WASD / Left Stick: move    Arrows / Right Stick: aim\n" +
                "Space / Cross: jump    J / R2: fire    K / L2: dash\n" +
                "U / Square: melee    I / Circle: contextual Counter\n" +
                "Toward + Counter: Throw    Close: Dodge Counter",
                body
            );
        }

        private void EnsureStyles()
        {
            if (header != null && body != null)
                return;

            header = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };
            header.normal.textColor = Color.white;

            body = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13
            };
            body.normal.textColor =
                new Color(0.88f, 0.94f, 1f);
        }
    }
}
