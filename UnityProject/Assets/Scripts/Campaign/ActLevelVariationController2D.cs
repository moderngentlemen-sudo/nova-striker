using System;
using UnityEngine;

namespace NovaStriker.Campaign
{
    [Serializable]
    public sealed class LevelVariationModuleBinding
    {
        public string Tag;
        public GameObject[] Targets;
        public bool AlwaysActive;
    }

    /// <summary>
    /// Scene-side adapter for the art-independent act variety contract. Greybox
    /// and production scenes can bind modular geometry/object groups to stable
    /// module tags without moving gameplay authority into presentation assets.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ActLevelVariationController2D : MonoBehaviour
    {
        [SerializeField] private LevelVariationModuleBinding[] modules;
        [SerializeField] private SectorHazard2D[] hazards;
        [SerializeField] private bool disableUnmatchedModules = true;

        public bool Configured { get; private set; }
        public ActLevelVarietyReference CurrentPlan { get; private set; }

        public event Action<ActLevelVarietyReference> PlanConfigured;

        public void ConfigureBindings(
            LevelVariationModuleBinding[] moduleBindings,
            SectorHazard2D[] hazardBindings)
        {
            modules =
                moduleBindings ??
                Array.Empty<LevelVariationModuleBinding>();

            hazards =
                hazardBindings ??
                Array.Empty<SectorHazard2D>();
        }

        public void ConfigurePlan(
            ActLevelVarietyReference plan)
        {
            CurrentPlan = plan;

            ConfigureModules(plan);
            ConfigureHazardPhases(plan);

            Configured = true;
            PlanConfigured?.Invoke(plan);
        }

        private void ConfigureModules(
            ActLevelVarietyReference plan)
        {
            if (modules == null)
                return;

            for (int i = 0; i < modules.Length; i++)
            {
                LevelVariationModuleBinding binding =
                    modules[i];

                if (binding == null)
                    continue;

                bool active =
                    binding.AlwaysActive ||
                    plan.UsesModule(binding.Tag);

                if (
                    !active &&
                    !disableUnmatchedModules
                )
                {
                    continue;
                }

                SetTargetsActive(
                    binding.Targets,
                    active
                );
            }
        }

        private void ConfigureHazardPhases(
            ActLevelVarietyReference plan)
        {
            if (hazards == null)
                return;

            float stride =
                Mathf.Max(
                    0f,
                    plan.HazardPhaseStride
                );

            for (int i = 0; i < hazards.Length; i++)
            {
                SectorHazard2D hazard =
                    hazards[i];

                if (!hazard)
                    continue;

                hazard.ResetCycle(
                    stride * i
                );
            }
        }

        private static void SetTargetsActive(
            GameObject[] targets,
            bool active)
        {
            if (targets == null)
                return;

            for (int i = 0; i < targets.Length; i++)
            {
                GameObject target =
                    targets[i];

                if (
                    target &&
                    target.activeSelf != active
                )
                {
                    target.SetActive(active);
                }
            }
        }
    }
}
