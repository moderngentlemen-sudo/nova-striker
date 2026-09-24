using System;
using System.IO;
using NovaStriker.Campaign;
using UnityEngine;

namespace NovaStriker.Debugging
{
    /// <summary>
    /// Play-session-only telemetry for topology validation. Data intentionally
    /// remains transient and never enters campaign save/progression state.
    /// </summary>
    public sealed class GreyboxLevelVarietyTelemetry : MonoBehaviour
    {
        [Serializable]
        private sealed class LabReport
        {
            public int labIndex;
            public bool completed;
            public int attempts;
            public float bestSeconds;
        }

        [Serializable]
        private sealed class CircuitReport
        {
            public string unityVersion;
            public string updatedUtc;
            public int labCount;
            public int completedCount;
            public bool circuitComplete;
            public string validationScope;
            public LabReport[] labs;
        }

        private const int MaxLabs = 8;

        private static readonly bool[] completed =
            new bool[MaxLabs];

        private static readonly int[] attempts =
            new int[MaxLabs];

        private static readonly float[] bestSeconds =
            new float[MaxLabs];

        [SerializeField] private GreyboxLevelVarietyNavigator navigator;
        [SerializeField] private ActObjectiveController2D objective;

        private float startedAt;
        private bool recordedCompletion;

        public float ElapsedSeconds =>
            Mathf.Max(
                0f,
                Time.realtimeSinceStartup - startedAt
            );

        public int CurrentAttempts =>
            ValidIndex(CurrentLabIndex)
                ? Mathf.Max(1, attempts[CurrentLabIndex])
                : 0;

        public float CurrentBestSeconds =>
            ValidIndex(CurrentLabIndex)
                ? bestSeconds[CurrentLabIndex]
                : 0f;

        public int CompletedLabCount
        {
            get
            {
                int count = 0;

                for (int i = 0; i < completed.Length; i++)
                {
                    if (completed[i])
                        count++;
                }

                return count;
            }
        }

        private int CurrentLabIndex =>
            navigator
                ? navigator.LabIndex
                : -1;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            Array.Clear(
                completed,
                0,
                completed.Length
            );

            Array.Clear(
                attempts,
                0,
                attempts.Length
            );

            Array.Clear(
                bestSeconds,
                0,
                bestSeconds.Length
            );

            TryDeletePreviousReport();
        }

        public static void RegisterAttempt(int index)
        {
            if (!ValidIndex(index))
                return;

            attempts[index] =
                Mathf.Max(
                    1,
                    attempts[index] + 1
                );

            WriteReport();
        }

        public void Configure(
            GreyboxLevelVarietyNavigator labNavigator,
            ActObjectiveController2D objectiveController)
        {
            navigator = labNavigator;
            objective = objectiveController;
        }

        private void Awake()
        {
            if (!navigator)
            {
                navigator =
                    FindAnyObjectByType<
                        GreyboxLevelVarietyNavigator
                    >();
            }

            if (!objective)
            {
                objective =
                    FindAnyObjectByType<
                        ActObjectiveController2D
                    >();
            }

            startedAt = Time.realtimeSinceStartup;

            int index = CurrentLabIndex;

            if (
                ValidIndex(index) &&
                attempts[index] <= 0
            )
            {
                attempts[index] = 1;
            }
        }

        private void OnEnable()
        {
            if (objective)
            {
                objective.ObjectiveCompleted +=
                    OnObjectiveCompleted;
            }
        }

        private void Start()
        {
            if (
                objective &&
                !recordedCompletion
            )
            {
                objective.ObjectiveCompleted -=
                    OnObjectiveCompleted;

                objective.ObjectiveCompleted +=
                    OnObjectiveCompleted;
            }
        }

        private void OnDisable()
        {
            if (objective)
            {
                objective.ObjectiveCompleted -=
                    OnObjectiveCompleted;
            }
        }

        private void OnObjectiveCompleted(
            EncounterObjectiveKind kind)
        {
            if (recordedCompletion)
                return;

            int index = CurrentLabIndex;

            if (!ValidIndex(index))
                return;

            recordedCompletion = true;
            completed[index] = true;

            float elapsed = ElapsedSeconds;

            if (
                bestSeconds[index] <= 0f ||
                elapsed < bestSeconds[index]
            )
            {
                bestSeconds[index] = elapsed;
            }

            WriteReport();
        }

        private static void WriteReport()
        {
            try
            {
                LabReport[] labs =
                    new LabReport[MaxLabs];

                int completedCount = 0;

                for (int i = 0; i < MaxLabs; i++)
                {
                    if (completed[i])
                        completedCount++;

                    labs[i] =
                        new LabReport
                        {
                            labIndex = i,
                            completed = completed[i],
                            attempts = attempts[i],
                            bestSeconds = bestSeconds[i]
                        };
                }

                CircuitReport report =
                    new()
                    {
                        unityVersion = Application.unityVersion,
                        updatedUtc =
                            DateTime.UtcNow.ToString("O"),
                        labCount = MaxLabs,
                        completedCount = completedCount,
                        circuitComplete =
                            completedCount == MaxLabs,
                        validationScope =
                            "Greybox level-topology Play Mode circuit only; " +
                            "not comprehensive gameplay, controller, profiler, " +
                            "platform, or production-asset validation.",
                        labs = labs
                    };

                string path =
                    ReportPath();

                string directory =
                    Path.GetDirectoryName(path);

                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(
                    path,
                    JsonUtility.ToJson(
                        report,
                        true
                    )
                );
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "[Level Variety Telemetry] Could not write circuit report: " +
                    exception.Message
                );
            }
        }

        private static void TryDeletePreviousReport()
        {
            try
            {
                string path =
                    ReportPath();

                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // Stale report cleanup is best-effort and never gameplay authority.
            }
        }

        private static string ReportPath()
        {
            if (Application.isEditor)
            {
                return Path.GetFullPath(
                    Path.Combine(
                        Application.dataPath,
                        "..",
                        "Library",
                        "NovaStrikerValidation",
                        "level-variety-circuit.json"
                    )
                );
            }

            return Path.Combine(
                Application.persistentDataPath,
                "level-variety-circuit.json"
            );
        }

        private static bool ValidIndex(int index)
        {
            return
                index >= 0 &&
                index < MaxLabs;
        }
    }
}
