using System;
using UnityEditor;
using UnityEngine;

namespace NovaStriker.EditorTools
{
    /// <summary>
    /// One-command entry point for the source-side validation work that remains
    /// before production Blender assets become the primary dependency.
    ///
    /// This suite deliberately does not claim Play Mode, profiler, controller,
    /// platform-SDK, or production-asset validation. It rebuilds the generated
    /// Mechanics Lab when requested, then invokes every currently available
    /// editor validation layer in a deterministic order.
    /// </summary>
    public static class PreBlenderValidationSuite
    {
        private const string Prefix = "[Pre-Blender Validation Suite] ";

        [MenuItem(
            "Nova Striker/Validation/Run Full Pre-Blender Validation",
            priority = 10)]
        public static void RunInteractive()
        {
            RunSuite(rebuildMechanicsLab: false);
        }

        [MenuItem(
            "Nova Striker/Validation/Rebuild Mechanics Lab + Run Full Validation",
            priority = 11)]
        public static void RebuildAndRunInteractive()
        {
            RunSuite(rebuildMechanicsLab: true);
        }

        private static void RunSuite(bool rebuildMechanicsLab)
        {
            string unityVersion = Application.unityVersion;
            DateTime startedUtc = DateTime.UtcNow;

            Debug.Log(
                Prefix +
                "Starting source-side validation on Unity " +
                unityVersion + ". " +
                "This run does not constitute Play Mode or production-asset QA."
            );

            try
            {
                if (rebuildMechanicsLab)
                {
                    Debug.Log(Prefix + "Rebuilding generated Mechanics Lab...");
                    NovaGreyboxBuilder.BuildGreybox();
                }

                AssetDatabase.Refresh();

                Debug.Log(Prefix + "1/3 Gameplay preflight...");
                GameplayPreflightValidator.Run();

                Debug.Log(Prefix + "2/3 Structural batch validation...");
                GameplayBatchValidator.RunInteractive();

                Debug.Log(Prefix + "3/3 Asset + presentation contract validation...");
                GameplayAssetContractValidator.RunInteractive();

                double elapsedSeconds =
                    (DateTime.UtcNow - startedUtc).TotalSeconds;

                Debug.Log(
                    Prefix +
                    "All validation layers were invoked in " +
                    elapsedSeconds.ToString("0.00") +
                    "s. Review Console errors/warnings before marking any item " +
                    "Unity-validated. Manual 1-4 player Play Mode, controller, " +
                    "save/commerce, and profiler passes are still required."
                );
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError(
                    Prefix +
                    "Validation suite aborted before all layers completed."
                );
            }
        }
    }
}
