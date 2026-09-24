using UnityEngine;
using UnityEngine.InputSystem;

namespace NovaStriker.InputSystemIntegration
{
    /// <summary>
    /// Development-only guard for unusually bursty editor/device input while
    /// running generated greybox scenes. Unity's default event-byte ceiling is
    /// retained for normal builds; this component gives validation scenes
    /// bounded headroom without disabling the safety limit.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class GreyboxInputEventBudgetGuard : MonoBehaviour
    {
        private const int ValidationEventBudgetBytes =
            16 * 1024 * 1024;

        private int previousBudgetBytes;
        private bool applied;

        public int ActiveBudgetBytes =>
            InputSystem.settings != null
                ? InputSystem.settings.maxEventBytesPerUpdate
                : 0;

        private void Awake()
        {
            if (
                !Application.isEditor &&
                !Debug.isDebugBuild
            )
            {
                return;
            }

            InputSettings settings =
                InputSystem.settings;

            if (settings == null)
                return;

            previousBudgetBytes =
                settings.maxEventBytesPerUpdate;

            if (
                previousBudgetBytes == 0 ||
                previousBudgetBytes >=
                ValidationEventBudgetBytes
            )
            {
                return;
            }

            settings.maxEventBytesPerUpdate =
                ValidationEventBudgetBytes;

            applied = true;

            Debug.Log(
                "[Greybox Input] Raised Input System event budget from " +
                previousBudgetBytes +
                " to " +
                ValidationEventBudgetBytes +
                " bytes/update for validation scenes. " +
                "The safety limit remains enabled.",
                this
            );
        }

        private void OnDestroy()
        {
            if (
                !applied ||
                InputSystem.settings == null
            )
            {
                return;
            }

            if (
                InputSystem.settings.maxEventBytesPerUpdate ==
                ValidationEventBudgetBytes
            )
            {
                InputSystem.settings.maxEventBytesPerUpdate =
                    previousBudgetBytes;
            }

            applied = false;
        }
    }
}
