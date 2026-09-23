using NovaStriker.Platform;
using UnityEngine;

namespace NovaStriker.Debugging
{
    /// <summary>
    /// Keeps the mechanics lab on a deterministic 60 Hz physics step.
    /// This is a greybox-only runtime helper.
    /// </summary>
    public sealed class GreyboxSessionBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            Time.fixedDeltaTime = 1f / 60f;

            if (!RuntimeScalabilityManager.Active)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = 120;
            }
        }
    }
}
