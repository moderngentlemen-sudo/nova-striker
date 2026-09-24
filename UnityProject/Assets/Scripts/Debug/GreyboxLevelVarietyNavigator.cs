using UnityEngine;
using UnityEngine.SceneManagement;

namespace NovaStriker.Debugging
{
    /// <summary>
    /// Greybox-only scene navigator for the generated level-variety test circuit.
    /// Scene paths are injected by the editor builder and are never gameplay
    /// progression authority.
    /// </summary>
    public sealed class GreyboxLevelVarietyNavigator : MonoBehaviour
    {
        [SerializeField] private int labIndex;
        [SerializeField] private string[] scenePaths;

        public int LabIndex => labIndex;
        public int LabCount =>
            scenePaths != null
                ? scenePaths.Length
                : 0;

        public bool CanNavigate =>
            LabCount > 0 &&
            labIndex >= 0 &&
            labIndex < LabCount;

        public void Configure(
            int index,
            string[] paths)
        {
            scenePaths =
                paths != null
                    ? (string[])paths.Clone()
                    : System.Array.Empty<string>();

            labIndex =
                scenePaths.Length > 0
                    ? Mathf.Clamp(index, 0, scenePaths.Length - 1)
                    : 0;
        }

        public void RestartCurrent()
        {
            if (!CanNavigate)
                return;

            GreyboxLevelVarietyTelemetry.RegisterAttempt(
                labIndex
            );

            LoadLab(labIndex);
        }

        public void LoadPrevious()
        {
            if (!CanNavigate)
                return;

            int previous =
                (labIndex - 1 + LabCount) %
                LabCount;

            GreyboxLevelVarietyTelemetry.RegisterAttempt(
                previous
            );

            LoadLab(previous);
        }

        public void LoadNext()
        {
            if (!CanNavigate)
                return;

            int next =
                (labIndex + 1) %
                LabCount;

            GreyboxLevelVarietyTelemetry.RegisterAttempt(
                next
            );

            LoadLab(next);
        }

        private void LoadLab(int index)
        {
            if (
                scenePaths == null ||
                index < 0 ||
                index >= scenePaths.Length ||
                string.IsNullOrWhiteSpace(scenePaths[index])
            )
            {
                return;
            }

            SceneManager.LoadScene(
                scenePaths[index],
                LoadSceneMode.Single
            );
        }
    }
}
