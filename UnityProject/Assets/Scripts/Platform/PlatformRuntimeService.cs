using System;
using UnityEngine;

namespace NovaStriker.Platform
{
    public interface IPlatformRuntimeProvider
    {
        bool Ready { get; }
        string ProviderId { get; }
        string LocalUserId { get; }

        bool SupportsAchievements { get; }
        bool SupportsPresence { get; }
        bool SupportsHaptics { get; }

        event Action<bool> ReadyChanged;

        void Initialize();
        void Shutdown();

        void SetAchievementProgress(
            string achievementId,
            float normalizedProgress);

        void SetPresence(
            string state,
            string details);

        void SetHaptics(
            int localPlayerSlot,
            float lowFrequency,
            float highFrequency,
            float durationSeconds);

        void StopHaptics(int localPlayerSlot);
    }

    /// <summary>
    /// Safe fallback used until a storefront/platform adapter is installed.
    /// </summary>
    public sealed class NullPlatformRuntimeProvider :
        IPlatformRuntimeProvider
    {
        public bool Ready { get; private set; }
        public string ProviderId => "null";
        public string LocalUserId => "local";

        public bool SupportsAchievements => false;
        public bool SupportsPresence => false;
        public bool SupportsHaptics => false;

        public event Action<bool> ReadyChanged;

        public void Initialize()
        {
            Ready = true;
            ReadyChanged?.Invoke(true);
        }

        public void Shutdown()
        {
            Ready = false;
            ReadyChanged?.Invoke(false);
        }

        public void SetAchievementProgress(
            string achievementId,
            float normalizedProgress)
        {
        }

        public void SetPresence(
            string state,
            string details)
        {
        }

        public void SetHaptics(
            int localPlayerSlot,
            float lowFrequency,
            float highFrequency,
            float durationSeconds)
        {
        }

        public void StopHaptics(int localPlayerSlot)
        {
        }
    }

    /// <summary>
    /// Storefront-neutral runtime facade for Steam, Xbox, PlayStation,
    /// Nintendo and mobile provider implementations.
    /// </summary>
    [DefaultExecutionOrder(-850)]
    [DisallowMultipleComponent]
    public sealed class PlatformRuntimeService : MonoBehaviour
    {
        private IPlatformRuntimeProvider provider;

        public static PlatformRuntimeService Active { get; private set; }

        public IPlatformRuntimeProvider Provider => provider;

        public bool Ready =>
            provider != null &&
            provider.Ready;

        public event Action<bool> ReadyChanged;

        private void Awake()
        {
            Active = this;
            DontDestroyOnLoad(gameObject);

            SetProvider(
                new NullPlatformRuntimeProvider()
            );
        }

        private void OnDestroy()
        {
            DetachProvider();

            if (Active == this)
                Active = null;
        }

        public void SetProvider(
            IPlatformRuntimeProvider replacement)
        {
            DetachProvider();

            provider =
                replacement ??
                new NullPlatformRuntimeProvider();

            provider.ReadyChanged +=
                OnProviderReadyChanged;

            provider.Initialize();
        }

        public void ReportAchievement(
            string achievementId,
            float normalizedProgress)
        {
            if (
                provider == null ||
                !provider.Ready ||
                !provider.SupportsAchievements ||
                string.IsNullOrEmpty(achievementId)
            )
            {
                return;
            }

            provider.SetAchievementProgress(
                achievementId,
                Mathf.Clamp01(normalizedProgress)
            );
        }

        public void SetPresence(
            string state,
            string details)
        {
            if (
                provider == null ||
                !provider.Ready ||
                !provider.SupportsPresence
            )
            {
                return;
            }

            provider.SetPresence(
                state ?? string.Empty,
                details ?? string.Empty
            );
        }

        public void SetHaptics(
            int localPlayerSlot,
            float lowFrequency,
            float highFrequency,
            float durationSeconds)
        {
            if (
                provider == null ||
                !provider.Ready ||
                !provider.SupportsHaptics
            )
            {
                return;
            }

            provider.SetHaptics(
                Mathf.Clamp(localPlayerSlot, 0, 3),
                Mathf.Clamp01(lowFrequency),
                Mathf.Clamp01(highFrequency),
                Mathf.Max(0f, durationSeconds)
            );
        }

        public void StopHaptics(int localPlayerSlot)
        {
            if (
                provider == null ||
                !provider.Ready ||
                !provider.SupportsHaptics
            )
            {
                return;
            }

            provider.StopHaptics(
                Mathf.Clamp(localPlayerSlot, 0, 3)
            );
        }

        private void OnProviderReadyChanged(bool ready)
        {
            ReadyChanged?.Invoke(ready);
        }

        private void DetachProvider()
        {
            if (provider == null)
                return;

            provider.ReadyChanged -=
                OnProviderReadyChanged;

            provider.Shutdown();
            provider = null;
        }
    }
}
