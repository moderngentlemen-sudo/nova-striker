using NovaStriker.Campaign;
using UnityEngine;

namespace NovaStriker.Debugging
{
    /// <summary>
    /// Immediate-mode HUD for level-variety greyboxes. It makes the current
    /// topology contract and objective state explicit without becoming
    /// production UI.
    /// </summary>
    public sealed class GreyboxLevelVarietyHUD : MonoBehaviour
    {
        [SerializeField] private GreyboxLevelVarietyBootstrap bootstrap;
        [SerializeField] private ActObjectiveController2D objective;
        [SerializeField] private GreyboxLevelVarietyNavigator navigator;
        [SerializeField] private GreyboxLevelVarietyTelemetry telemetry;

        private GUIStyle header;
        private GUIStyle body;
        private GUIStyle completeStyle;

        public void Configure(
            GreyboxLevelVarietyBootstrap labBootstrap,
            ActObjectiveController2D objectiveController,
            GreyboxLevelVarietyNavigator labNavigator,
            GreyboxLevelVarietyTelemetry labTelemetry)
        {
            bootstrap = labBootstrap;
            objective = objectiveController;
            navigator = labNavigator;
            telemetry = labTelemetry;
        }

        private void Awake()
        {
            if (!bootstrap)
                bootstrap = FindAnyObjectByType<GreyboxLevelVarietyBootstrap>();

            if (!objective)
                objective = FindAnyObjectByType<ActObjectiveController2D>();

            if (!navigator)
            {
                navigator =
                    FindAnyObjectByType<
                        GreyboxLevelVarietyNavigator
                    >();
            }

            if (!telemetry)
            {
                telemetry =
                    FindAnyObjectByType<
                        GreyboxLevelVarietyTelemetry
                    >();
            }
        }

        private void OnGUI()
        {
            if (!bootstrap || !objective)
                return;

            EnsureStyles();

            ActLevelVarietyReference plan =
                bootstrap.CurrentPlan;

            float width = 500f;
            float height = 252f;
            float x = 14f;
            float y = Screen.height - height - 14f;

            GUI.Box(
                new Rect(x, y, width, height),
                GUIContent.none
            );

            GUI.Label(
                new Rect(x + 14f, y + 10f, width - 28f, 26f),
                "LEVEL VARIETY LAB",
                header
            );

            string status =
                objective.Completed
                    ? "COMPLETE"
                    : objective.Started
                        ? "ACTIVE"
                        : "READY";

            int labNumber =
                navigator
                    ? navigator.LabIndex + 1
                    : 0;

            int labCount =
                navigator
                    ? navigator.LabCount
                    : 0;

            string telemetryText =
                telemetry
                    ? "    Time: " +
                      telemetry.ElapsedSeconds.ToString("0.0") +
                      "s    Attempts: " +
                      telemetry.CurrentAttempts +
                      "\nCircuit: " +
                      telemetry.CompletedLabCount +
                      "/" +
                      Mathf.Max(1, labCount) +
                      " complete    Best: " +
                      (
                          telemetry.CurrentBestSeconds > 0f
                              ? telemetry.CurrentBestSeconds.ToString("0.0") + "s"
                              : "-"
                      )
                    : string.Empty;

            string text =
                "Lab " + labNumber + "/" + Mathf.Max(1, labCount) +
                "    " + plan.Key + "\n" +
                "Topology: " + plan.Topology +
                "    Traversal: " + plan.Traversal + "\n" +
                "Objective: " + plan.Objective +
                "    Route: " + plan.RouteChoice + "\n" +
                "Hazards: " + plan.HazardPattern +
                "    Pair split: " +
                (plan.SupportsPairSplit ? "supported" : "not required") +
                "\nProgress: " +
                Mathf.RoundToInt(objective.Progress01 * 100f) +
                "%    Status: " + status +
                telemetryText;

            GUI.Label(
                new Rect(x + 14f, y + 42f, width - 28f, 132f),
                text,
                body
            );

            if (objective.Completed)
            {
                GUI.Label(
                    new Rect(x + 14f, y + 176f, width - 28f, 24f),
                    "OBJECTIVE COMPLETE — topology pass may continue.",
                    completeStyle
                );
            }
            else
            {
                GUI.Label(
                    new Rect(x + 14f, y + 176f, width - 28f, 24f),
                    ObjectiveHint(plan.Objective),
                    body
                );
            }

            bool previousEnabled = GUI.enabled;
            GUI.enabled =
                navigator &&
                navigator.CanNavigate;

            float buttonY = y + 208f;
            float buttonWidth = 146f;

            if (
                GUI.Button(
                    new Rect(x + 14f, buttonY, buttonWidth, 30f),
                    "Previous Lab"
                )
            )
            {
                navigator.LoadPrevious();
            }

            if (
                GUI.Button(
                    new Rect(x + 177f, buttonY, buttonWidth, 30f),
                    "Restart Lab"
                )
            )
            {
                navigator.RestartCurrent();
            }

            if (
                GUI.Button(
                    new Rect(x + 340f, buttonY, buttonWidth, 30f),
                    "Next Lab"
                )
            )
            {
                navigator.LoadNext();
            }

            GUI.enabled = previousEnabled;
        }

        private static string ObjectiveHint(
            EncounterObjectiveKind kind)
        {
            return kind switch
            {
                EncounterObjectiveKind.Advance =>
                    "Reach the marked goal volume.",
                EncounterObjectiveKind.Pursuit =>
                    "Reach the pursuit endpoint.",
                EncounterObjectiveKind.HoldZone =>
                    "Remain inside the marked hold volume.",
                EncounterObjectiveKind.DisableNodes =>
                    "Touch both marked objective nodes.",
                EncounterObjectiveKind.MultiFront =>
                    "Clear both marked fronts; solo may do them sequentially.",
                EncounterObjectiveKind.Survival =>
                    "Stay alive until the survival timer completes.",
                EncounterObjectiveKind.Eliminate =>
                    "Clear the representative encounter.",
                EncounterObjectiveKind.ProtectAsset =>
                    "Keep the protected asset alive through the encounter.",
                _ =>
                    "Complete the active greybox objective."
            };
        }

        private void EnsureStyles()
        {
            if (
                header != null &&
                body != null &&
                completeStyle != null
            )
            {
                return;
            }

            header = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };

            body = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true
            };

            completeStyle = new GUIStyle(body)
            {
                fontStyle = FontStyle.Bold
            };

            header.normal.textColor = Color.white;
            body.normal.textColor = new Color(0.88f, 0.94f, 1f);
            completeStyle.normal.textColor = new Color(0.75f, 1f, 0.75f);
        }
    }
}
