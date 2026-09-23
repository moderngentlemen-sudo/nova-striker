using NovaStriker.Campaign;
using UnityEngine;

namespace NovaStriker.Debugging
{
    /// <summary>
    /// Starts a representative level-variety plan in a generated greybox scene.
    /// This helper exists only for pre-Blender topology/objective validation.
    /// </summary>
    public sealed class GreyboxLevelVarietyBootstrap : MonoBehaviour
    {
        [SerializeField, Range(0, 5)] private int sectorIndex;
        [SerializeField, Range(0, 2)] private int actIndex;
        [SerializeField] private ActObjectiveController2D objective;
        [SerializeField] private ActLevelVariationController2D variation;

        public void Configure(
            int sector,
            int act,
            ActObjectiveController2D objectiveController,
            ActLevelVariationController2D variationController)
        {
            sectorIndex = Mathf.Clamp(sector, 0, 5);
            actIndex = Mathf.Clamp(act, 0, 2);
            objective = objectiveController;
            variation = variationController;
        }

        private void Start()
        {
            ActLevelVarietyReference plan =
                ActLevelVarietyCatalog.Get(
                    sectorIndex,
                    actIndex
                );

            variation?.ConfigurePlan(plan);
            objective?.ConfigureObjective(plan.Objective);
        }
    }
}
