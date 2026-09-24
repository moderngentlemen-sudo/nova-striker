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

        public static void RegisterAttempt(int index)
        {
            if (!ValidIndex(index))
                return;

            attempts[index] =
                Mathf.Max(
                    1,
                    attempts[index] + 1
                );
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
        }

        private static bool ValidIndex(int index)
        {
            return
                index >= 0 &&
                index < MaxLabs;
        }
    }
}
