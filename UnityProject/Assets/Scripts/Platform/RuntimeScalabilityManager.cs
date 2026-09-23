using UnityEngine;

namespace NovaStriker.Platform
{
    public enum RuntimeQualityTier
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Ultra = 3
    }

    /// <summary>
    /// Hardware-scalable runtime budget. Gameplay remains fixed at 60 Hz while
    /// presentation systems can consume these budget values to scale visual
    /// density without changing mechanics.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    [DisallowMultipleComponent]
    public sealed class RuntimeScalabilityManager : MonoBehaviour
    {
        [SerializeField] private bool autoDetect = true;
        [SerializeField] private RuntimeQualityTier forcedTier =
            RuntimeQualityTier.High;

        [SerializeField] private bool preferHighRefresh = true;

        public static RuntimeScalabilityManager Active { get; private set; }

        public RuntimeQualityTier Tier { get; private set; }
        public int TargetFrameRate { get; private set; }
        public float VfxDensity { get; private set; }
        public float ShadowDistanceMultiplier { get; private set; }
        public int SuggestedDynamicLightBudget { get; private set; }
        public float SuggestedRenderScaleFloor { get; private set; }

        private void Awake()
        {
            Active = this;

            Tier =
                autoDetect
                    ? DetectTier()
                    : forcedTier;

            ApplyTier(Tier);
        }

        private void OnDestroy()
        {
            if (Active == this)
                Active = null;
        }

        public void ApplyTier(RuntimeQualityTier tier)
        {
            Tier = tier;

            // Gameplay timing never follows rendering frame rate.
            Time.fixedDeltaTime = 1f / 60f;
            QualitySettings.vSyncCount = 0;

            switch (tier)
            {
                case RuntimeQualityTier.Low:
                    TargetFrameRate = 60;
                    VfxDensity = 0.55f;
                    ShadowDistanceMultiplier = 0.55f;
                    SuggestedDynamicLightBudget = 2;
                    SuggestedRenderScaleFloor = 0.70f;
                    break;

                case RuntimeQualityTier.Medium:
                    TargetFrameRate = 60;
                    VfxDensity = 0.75f;
                    ShadowDistanceMultiplier = 0.75f;
                    SuggestedDynamicLightBudget = 4;
                    SuggestedRenderScaleFloor = 0.78f;
                    break;

                case RuntimeQualityTier.Ultra:
                    TargetFrameRate =
                        preferHighRefresh ? 120 : 60;
                    VfxDensity = 1.25f;
                    ShadowDistanceMultiplier = 1.20f;
                    SuggestedDynamicLightBudget = 12;
                    SuggestedRenderScaleFloor = 0.90f;
                    break;

                default:
                    TargetFrameRate =
                        preferHighRefresh ? 120 : 60;
                    VfxDensity = 1f;
                    ShadowDistanceMultiplier = 1f;
                    SuggestedDynamicLightBudget = 8;
                    SuggestedRenderScaleFloor = 0.85f;
                    break;
            }

            Application.targetFrameRate =
                TargetFrameRate;
        }

        private static RuntimeQualityTier DetectTier()
        {
            string platform =
                Application.platform.ToString();

            int memory =
                SystemInfo.systemMemorySize;

            int graphicsMemory =
                SystemInfo.graphicsMemorySize;

            bool mobile =
                Application.isMobilePlatform;

            if (
                platform.Contains("Switch") &&
                !platform.Contains("Switch2")
            )
            {
                return RuntimeQualityTier.Low;
            }

            if (
                platform.Contains("PS4") ||
                platform.Contains("XboxOne")
            )
            {
                return RuntimeQualityTier.Medium;
            }

            if (mobile)
            {
                if (
                    memory <= 4096 ||
                    graphicsMemory <= 2048
                )
                {
                    return RuntimeQualityTier.Low;
                }

                if (
                    memory <= 6144 ||
                    graphicsMemory <= 3072
                )
                {
                    return RuntimeQualityTier.Medium;
                }

                return RuntimeQualityTier.High;
            }

            if (
                platform.Contains("SteamDeck") ||
                platform.Contains("Linux")
            )
            {
                return RuntimeQualityTier.High;
            }

            if (
                memory >= 16000 &&
                graphicsMemory >= 8000
            )
            {
                return RuntimeQualityTier.Ultra;
            }

            if (
                memory >= 8000 &&
                graphicsMemory >= 4000
            )
            {
                return RuntimeQualityTier.High;
            }

            return RuntimeQualityTier.Medium;
        }
    }
}
