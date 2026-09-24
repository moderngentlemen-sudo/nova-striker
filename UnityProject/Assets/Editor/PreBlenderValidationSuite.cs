using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NovaStriker.EditorTools
{
    /// <summary>
    /// One-command entry point for the source-side validation work that remains
    /// before production Blender assets become the primary dependency.
    ///
    /// This suite deliberately does not claim Play Mode, profiler, controller,
    /// platform-SDK, or production-asset validation. It can optionally rebuild
    /// the generated Mechanics Lab, then invokes every current editor validation
    /// layer in a deterministic order and records an aggregate pass/fail result.
    /// </summary>
    public static class PreBlenderValidationSuite
    {
        private const string Prefix = "[Pre-Blender Validation Suite] ";
        private const string ReportRelativePath =
            "Library/NovaStrikerValidation/pre-blender-validation.json";

        [Serializable]
        private sealed class ValidationStepResult
        {
            public string name;
            public bool passed;
            public int errorLogs;
            public int warningLogs;
            public double elapsedSeconds;
            public string exception;
        }

        [Serializable]
        private sealed class ValidationReport
        {
            public string unityVersion;
            public string startedUtc;
            public string finishedUtc;
            public bool rebuiltMechanicsLab;
            public bool rebuiltLevelVarietyLabs;
            public bool sourceValidationPassed;
            public bool playModeValidated;
            public bool productionAssetsValidated;
            public string validationScope;
            public string boundaryNote;
            public List<ValidationStepResult> steps =
                new List<ValidationStepResult>();
        }

        [MenuItem(
            "Nova Striker/Validation/Run Full Pre-Blender Validation",
            priority = 10)]
        public static void RunInteractive()
        {
            RunSuite(
                rebuildMechanicsLab: false,
                rebuildLevelVarietyLabs: false
            );
        }

        [MenuItem(
            "Nova Striker/Validation/Rebuild Mechanics Lab + Run Full Validation",
            priority = 11)]
        public static void RebuildAndRunInteractive()
        {
            RunSuite(
                rebuildMechanicsLab: true,
                rebuildLevelVarietyLabs: true
            );
        }

        /// <summary>
        /// Headless entry point for source-side validation without regenerating
        /// greybox assets. Exits Unity with code 0 on success and 1 on failure.
        /// This is not a substitute for Play Mode or profiler validation.
        /// </summary>
        public static void RunForCommandLine()
        {
            RunCommandLine(
                rebuildMechanicsLab: false,
                rebuildLevelVarietyLabs: false
            );
        }

        /// <summary>
        /// Headless entry point that regenerates the Mechanics Lab before running
        /// all source-side validators. Exits Unity with code 0 on success and 1
        /// on failure.
        /// </summary>
        public static void RebuildAndRunForCommandLine()
        {
            RunCommandLine(
                rebuildMechanicsLab: true,
                rebuildLevelVarietyLabs: true
            );
        }

        private static void RunCommandLine(
            bool rebuildMechanicsLab,
            bool rebuildLevelVarietyLabs)
        {
            bool passed =
                RunSuite(
                    rebuildMechanicsLab,
                    rebuildLevelVarietyLabs
                );

            if (Application.isBatchMode)
                EditorApplication.Exit(passed ? 0 : 1);
        }

        private static bool RunSuite(
            bool rebuildMechanicsLab,
            bool rebuildLevelVarietyLabs)
        {
            DateTime startedUtc = DateTime.UtcNow;
            ValidationReport report = new ValidationReport
            {
                unityVersion = Application.unityVersion,
                startedUtc = startedUtc.ToString("O"),
                rebuiltMechanicsLab = rebuildMechanicsLab,
                rebuiltLevelVarietyLabs = rebuildLevelVarietyLabs,
                playModeValidated = false,
                productionAssetsValidated = false,
                validationScope =
                    "Unity editor compile-time/source-contract validation",
                boundaryNote =
                    "A passing report confirms editor/source contracts and " +
                    "generated greybox scene structure only. It does not mark " +
                    "1-4 player Play Mode, controller, persistence behavior, " +
                    "profiler, platform SDK, or production Blender/presentation " +
                    "validation complete."
            };

            Debug.Log(
                Prefix +
                "Starting source-side validation on Unity " +
                Application.unityVersion + ". " +
                "A passing run does not constitute Play Mode, profiler, " +
                "controller, platform-SDK, or production-asset QA."
            );

            bool allPassed = true;

            if (rebuildMechanicsLab)
            {
                allPassed &=
                    ExecuteStep(
                        "Mechanics Lab rebuild",
                        NovaGreyboxBuilder.BuildGreybox,
                        report
                    );
            }

            if (rebuildLevelVarietyLabs)
            {
                allPassed &=
                    ExecuteStep(
                        "Level variety lab rebuild",
                        LevelVarietyGreyboxBuilder.BuildAllForValidation,
                        report
                    );
            }

            allPassed &=
                ExecuteStep(
                    "Asset database refresh",
                    () => AssetDatabase.Refresh(),
                    report
                );

            allPassed &=
                ExecuteStep(
                    "Gameplay preflight",
                    GameplayPreflightValidator.Run,
                    report
                );

            allPassed &=
                ExecuteStep(
                    "Structural batch validation",
                    GameplayBatchValidator.RunInteractive,
                    report
                );

            allPassed &=
                ExecuteStep(
                    "Asset + presentation contract validation",
                    GameplayAssetContractValidator.RunInteractive,
                    report
                );

            allPassed &=
                ExecuteStep(
                    "Generated level variety scene validation",
                    () =>
                    {
                        bool passed =
                            LevelVarietyGreyboxValidator.ValidateGeneratedLabs(
                                restoreOriginalScene: true
                            );

                        if (!passed)
                        {
                            Debug.LogError(
                                Prefix +
                                "Generated level variety scene validation failed."
                            );
                        }
                    },
                    report
                );

            report.sourceValidationPassed = allPassed;
            report.finishedUtc = DateTime.UtcNow.ToString("O");

            WriteReport(report);

            double elapsedSeconds =
                (DateTime.UtcNow - startedUtc).TotalSeconds;

            string summary =
                Prefix +
                (allPassed ? "PASS" : "FAIL") +
                ": source-side validation finished in " +
                elapsedSeconds.ToString("0.00") +
                "s. Report: " +
                ReportRelativePath +
                ". Manual topology-circuit Play Mode, 1-4 player/controller, " +
                "persistence/commerce behavior, profiler, and production-asset " +
                "passes remain separate.";

            if (allPassed)
                Debug.Log(summary);
            else
                Debug.LogError(summary);

            return allPassed;
        }

        private static bool ExecuteStep(
            string name,
            Action action,
            ValidationReport report)
        {
            int errorLogs = 0;
            int warningLogs = 0;
            string exceptionSummary = string.Empty;
            DateTime startedUtc = DateTime.UtcNow;

            Application.LogCallback capture =
                (condition, stackTrace, type) =>
                {
                    if (
                        type == LogType.Error ||
                        type == LogType.Exception ||
                        type == LogType.Assert
                    )
                    {
                        errorLogs++;
                    }
                    else if (type == LogType.Warning)
                    {
                        warningLogs++;
                    }
                };

            Application.logMessageReceived += capture;

            try
            {
                Debug.Log(Prefix + "Running: " + name + "...");
                action();
            }
            catch (Exception exception)
            {
                exceptionSummary =
                    exception.GetType().Name +
                    ": " +
                    exception.Message;

                Debug.LogException(exception);
            }
            finally
            {
                Application.logMessageReceived -= capture;
            }

            bool passed =
                errorLogs == 0 &&
                string.IsNullOrEmpty(exceptionSummary);

            ValidationStepResult result =
                new ValidationStepResult
                {
                    name = name,
                    passed = passed,
                    errorLogs = errorLogs,
                    warningLogs = warningLogs,
                    elapsedSeconds =
                        (DateTime.UtcNow - startedUtc).TotalSeconds,
                    exception = exceptionSummary
                };

            report.steps.Add(result);

            string stepSummary =
                Prefix +
                name +
                ": " +
                (passed ? "PASS" : "FAIL") +
                " (" +
                errorLogs +
                " error log(s), " +
                warningLogs +
                " warning log(s), " +
                result.elapsedSeconds.ToString("0.00") +
                "s).";

            if (passed)
                Debug.Log(stepSummary);
            else
                Debug.LogError(stepSummary);

            return passed;
        }

        private static void WriteReport(ValidationReport report)
        {
            try
            {
                string projectRoot =
                    Path.GetFullPath(
                        Path.Combine(
                            Application.dataPath,
                            ".."
                        )
                    );

                string reportPath =
                    Path.Combine(
                        projectRoot,
                        ReportRelativePath
                    );

                string directory =
                    Path.GetDirectoryName(reportPath);

                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(
                    reportPath,
                    JsonUtility.ToJson(report, true)
                );
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    Prefix +
                    "Unable to write machine-readable validation report: " +
                    exception.Message
                );
            }
        }
    }
}
