using NovaStriker.Combat;
using NovaStriker.Core;
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

        private GameplayCue lastPlayerCue;
        private bool hasLastPlayerCue;

        public void Configure(
            NovaMotor2D motorController,
            NovaCombatController combatController,
            Damageable2D health)
        {
            motor = motorController;
            combat = combatController;
            playerHealth = health;
        }

        private void OnEnable()
        {
            GameplayEventHub.CueRaised += OnGameplayCue;
        }

        private void OnDisable()
        {
            GameplayEventHub.CueRaised -= OnGameplayCue;
        }

        private void OnGameplayCue(GameplayCue cue)
        {
            if (motor && cue.ActorId != motor.PlayerId)
                return;

            lastPlayerCue = cue;
            hasLastPlayerCue = true;
        }

        private void OnGUI()
        {
            EnsureStyles();

            GUI.Box(new Rect(14, 14, 500, 354), GUIContent.none);

            GUI.Label(
                new Rect(28, 24, 455, 28),
                "NOVA STRIKER — UNITY GREYBOX",
                header
            );

            float dashCharge = motor ? motor.DashCharge : 0f;
            float fireCharge = combat ? combat.FireCharge : 0f;
            float health = playerHealth ? playerHealth.Health : 0f;
            string dashTier = motor
                ? motor.ActiveDashTier.ToString()
                : "-";
            string chargingDashTier =
                motor && dashCharge > 0f
                    ? motor.ChargingDashTier.ToString()
                    : "-";
            string slideTier = motor
                ? motor.ActiveSlideTier.ToString()
                : "-";

            Vector2 velocity =
                motor ? motor.Velocity : Vector2.zero;

            float grappleDistance =
                combat && combat.HasGrappleLock
                    ? Vector2.Distance(
                        combat.transform.position,
                        combat.GrappleLockPosition
                    )
                    : 0f;

            string lastCue =
                hasLastPlayerCue
                    ? $"{lastPlayerCue.Type}" +
                      $"{(string.IsNullOrEmpty(lastPlayerCue.Id) ? "" : $" / {lastPlayerCue.Id}")}" +
                      $"  T{lastPlayerCue.Tier}  V:{lastPlayerCue.Value:0.##}"
                    : "-";

            string character =
                combat
                    ? combat.Character.ToString()
                    : "-";

            string state =
                $"Character: {character}    " +
                $"Counter: {(combat ? combat.CurrentCounterMode.ToString() : "-")}\n" +
                $"Grapple lock: {(combat ? combat.CurrentGrappleLockKind.ToString() : "-")}    " +
                $"Distance: {grappleDistance:0.00}\n" +
                $"Up+Counter: {(combat && combat.EchoTraversalGrappleRequested)}    " +
                $"Velocity: {velocity.x:0.0}, {velocity.y:0.0}\n" +
                $"Grounded: {(motor && motor.Grounded)}    " +
                $"Wall slide: {(motor && motor.IsWallSliding)}    " +
                $"Wall: {(motor ? motor.WallDirection : 0)}\n" +
                $"Grapple travel: {(motor && motor.IsGrapplingTraversal)}\n" +
                $"Crouch: {(motor && motor.IsCrouching)}    " +
                $"Dash: {(motor && motor.IsDashing)} / {dashTier}\n" +
                $"Slide: {(motor && motor.IsSliding)} / {slideTier}\n" +
                $"Dash charge: {dashCharge:0.00}s → {chargingDashTier}    " +
                $"thresholds .30 / .85\n" +
                $"Fire charge: {fireCharge:0.00}s\n" +
                $"Deflect active: {(combat && combat.IsParryActive)}    " +
                $"Perfect: {(combat && combat.IsPerfectParryWindow)}\n" +
                $"Melee: {(combat ? combat.MeleeStep : 0)}    " +
                $"Health: {health:0}\n" +
                $"Last cue: {lastCue}";

            GUI.Label(
                new Rect(28, 58, 455, 230),
                state,
                body
            );

            GUI.Label(
                new Rect(28, 292, 455, 48),
                "WASD / Left Stick: move    Arrows / Right Stick: aim\n" +
                "Space / Cross: jump    J / R2: fire    K / L2: dash\n" +
                "U / Square: melee    I / Circle: contextual Counter/Grapple\n" +
                "Echo: Up/Up-diagonal + Counter = traversal grapple",
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
