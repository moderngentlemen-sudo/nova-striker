using NovaStriker.Campaign;
using NovaStriker.Combat;
using NovaStriker.Platform;
using NovaStriker.Session;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NovaStriker.Debugging
{
    /// <summary>
    /// Lightweight greybox-only runtime telemetry used to prepare profiler and
    /// scalability passes without making performance data part of gameplay
    /// authority. The probe auto-installs only in the Mechanics Lab while
    /// running in the Editor or a Development Build.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameplayPerformanceProbe : MonoBehaviour
    {
        private const string MechanicsLabSceneName =
            "NovaMechanicsGreybox";

        [SerializeField, Min(0.1f)]
        private float smoothingSeconds = 1.5f;

        [SerializeField, Min(1f)]
        private float warningPersistenceSeconds = 3f;

        [SerializeField, Range(1.02f, 2f)]
        private float warningFrameBudgetMultiplier = 1.20f;

        private float smoothedFrameSeconds;
        private float overBudgetSeconds;
        private float nextWarningTime;
        private GUIStyle body;
        private GUIStyle header;

        public float SmoothedFrameMilliseconds =>
            smoothedFrameSeconds * 1000f;

        public float ApproximateFramesPerSecond =>
            smoothedFrameSeconds > 0.00001f
                ? 1f / smoothedFrameSeconds
                : 0f;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallInMechanicsLab()
        {
            if (!UnityEngine.Debug.isDebugBuild)
                return;

            Scene scene = SceneManager.GetActiveScene();

            if (scene.name != MechanicsLabSceneName)
                return;

            if (Object.FindFirstObjectByType<GameplayPerformanceProbe>())
                return;

            GameObject root = new("__GameplayPerformanceProbe");
            root.AddComponent<GameplayPerformanceProbe>();
        }

        private void Awake()
        {
            smoothedFrameSeconds =
                Mathf.Max(0.00001f, Time.unscaledDeltaTime);
        }

        private void Update()
        {
            float frameSeconds =
                Mathf.Max(0.00001f, Time.unscaledDeltaTime);

            float blend =
                1f - Mathf.Exp(
                    -frameSeconds /
                    Mathf.Max(0.1f, smoothingSeconds)
                );

            smoothedFrameSeconds =
                Mathf.Lerp(
                    smoothedFrameSeconds,
                    frameSeconds,
                    blend
                );

            RuntimeScalabilityManager scalability =
                RuntimeScalabilityManager.Active;

            int targetFps =
                scalability
                    ? Mathf.Max(1, scalability.TargetFrameRate)
                    : 60;

            float budgetSeconds = 1f / targetFps;
            bool overBudget =
                smoothedFrameSeconds >
                budgetSeconds * warningFrameBudgetMultiplier;

            if (overBudget)
                overBudgetSeconds += frameSeconds;
            else
                overBudgetSeconds = 0f;

            if (
                overBudgetSeconds >= warningPersistenceSeconds &&
                Time.unscaledTime >= nextWarningTime
            )
            {
                nextWarningTime =
                    Time.unscaledTime +
                    Mathf.Max(5f, warningPersistenceSeconds);

                Debug.LogWarning(
                    "[Gameplay Performance Probe] Sustained frame-time " +
                    "pressure detected in the Mechanics Lab: " +
                    SmoothedFrameMilliseconds.ToString("0.0") +
                    " ms average against a " +
                    (budgetSeconds * 1000f).ToString("0.0") +
                    " ms target. Capture a Unity Profiler trace before " +
                    "changing gameplay or presentation budgets.",
                    this
                );
            }
        }

        private void OnGUI()
        {
            if (!UnityEngine.Debug.isDebugBuild)
                return;

            EnsureStyles();

            RuntimeScalabilityManager scalability =
                RuntimeScalabilityManager.Active;
            StrikeTeamSession session =
                StrikeTeamSession.Active;

            int targetFps =
                scalability
                    ? scalability.TargetFrameRate
                    : 60;

            string tier =
                scalability
                    ? scalability.Tier.ToString()
                    : "Unresolved";

            int activePlayers =
                session
                    ? session.ActivePlayerCount
                    : 0;

            const float width = 336f;
            const float height = 132f;
            float x = Screen.width - width - 14f;
            float y = 194f;

            GUI.Box(
                new Rect(x, y, width, height),
                GUIContent.none
            );

            GUI.Label(
                new Rect(x + 14f, y + 10f, 306f, 24f),
                "PRE-BLENDER PERFORMANCE PROBE",
                header
            );

            string text =
                "Frame: " +
                SmoothedFrameMilliseconds.ToString("0.0") +
                " ms / " +
                ApproximateFramesPerSecond.ToString("0") +
                " fps   Target: " +
                targetFps +
                " fps\n" +
                "Tier: " + tier +
                "   Active players: " + activePlayers +
                "/4\n" +
                "Projectile pool inactive: " +
                ProjectilePool2D.InactiveCount +
                "   Enemy pool inactive: " +
                EncounterEnemyPool2D.InactiveCount +
                "\nProfiler capture remains authoritative for allocation/performance QA.";

            GUI.Label(
                new Rect(x + 14f, y + 38f, 306f, 84f),
                text,
                body
            );
#endif
        }

        private void EnsureStyles()
        {
            if (body != null && header != null)
                return;

            header = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 13
            };

            body = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                wordWrap = true
            };
        }
    }
}
