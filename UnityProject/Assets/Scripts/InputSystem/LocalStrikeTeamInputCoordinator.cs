using System;
using System.Collections.Generic;
using NovaStriker.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NovaStriker.InputSystemIntegration
{
    /// <summary>
    /// Binds up to four physical Gamepads to four local Striker slots.
    /// A Gamepad can belong to only one slot. Disconnecting does not erase the
    /// remembered device ID, allowing the same device to reclaim its slot when
    /// it reconnects during the current session.
    /// </summary>
    [DefaultExecutionOrder(-400)]
    [DisallowMultipleComponent]
    public sealed class LocalStrikeTeamInputCoordinator : MonoBehaviour
    {
        [SerializeField] private NovaInputSystemAdapter[] players =
            new NovaInputSystemAdapter[4];

        [SerializeField] private bool playerOneKeyboardEnabled = true;
        [SerializeField] private bool autoAssignConnectedGamepads = true;

        private readonly bool[] connectedState = new bool[4];
        private bool connectionStateInitialized;

        public event Action<int> PlayerJoined;
        public event Action<int> PlayerLeft;
        public event Action<int, bool, int> DeviceConnectionChanged;

        private void OnEnable()
        {
            InputSystem.onDeviceChange += OnDeviceChange;
            ConfigurePlayerSlots();
            ReconcileGamepads();
            CaptureConnectionState(false);
        }

        private void OnDisable()
        {
            InputSystem.onDeviceChange -= OnDeviceChange;
        }

        public void Configure(
            NovaInputSystemAdapter[] adapters)
        {
            players =
                adapters ??
                new NovaInputSystemAdapter[4];

            ConfigurePlayerSlots();
            ReconcileGamepads();
            CaptureConnectionState(false);
        }

        public bool RequestJoin(int slot)
        {
            NovaInputSystemAdapter adapter =
                AdapterAt(slot);

            if (!adapter)
                return false;

            bool hasInput =
                adapter.HasAssignedGamepad ||
                (
                    slot == 0 &&
                    playerOneKeyboardEnabled
                );

            if (!hasInput)
                return false;

            StrikerPlayerIdentity identity =
                adapter.GetComponent<StrikerPlayerIdentity>();

            if (!identity)
                return false;

            if (!identity.IsParticipating)
            {
                identity.SetParticipation(true);
                PlayerJoined?.Invoke(slot);
            }

            return true;
        }

        public bool RequestLeave(int slot)
        {
            NovaInputSystemAdapter adapter =
                AdapterAt(slot);

            if (!adapter)
                return false;

            StrikerPlayerIdentity identity =
                adapter.GetComponent<StrikerPlayerIdentity>();

            if (!identity)
                return false;

            if (identity.IsParticipating)
            {
                identity.SetParticipation(false);
                PlayerLeft?.Invoke(slot);
            }

            return true;
        }

        public bool IsDeviceConnected(int slot)
        {
            NovaInputSystemAdapter adapter =
                AdapterAt(slot);

            return adapter && adapter.HasAssignedGamepad;
        }

        private void ConfigurePlayerSlots()
        {
            if (players == null)
                return;

            for (int i = 0; i < players.Length; i++)
            {
                NovaInputSystemAdapter adapter =
                    players[i];

                if (!adapter)
                    continue;

                adapter.ConfigureSlot(
                    i,
                    playerOneKeyboardEnabled && i == 0
                );
            }
        }

        private void ReconcileGamepads()
        {
            if (!autoAssignConnectedGamepads || players == null)
                return;

            HashSet<int> claimed = new();

            // First preserve existing player-to-device identity.
            for (int i = 0; i < players.Length; i++)
            {
                NovaInputSystemAdapter adapter =
                    players[i];

                if (!adapter)
                    continue;

                adapter.RefreshAssignedDevice();

                if (adapter.HasAssignedGamepad)
                    claimed.Add(adapter.AssignedDeviceId);
            }

            // Then assign currently unclaimed connected pads to vacant slots.
            for (int i = 0; i < players.Length; i++)
            {
                NovaInputSystemAdapter adapter =
                    players[i];

                if (!adapter || adapter.HasAssignedGamepad)
                    continue;

                // A disconnected known device keeps its reservation rather
                // than being silently replaced by another controller.
                if (adapter.AssignedDeviceId >= 0)
                    continue;

                Gamepad available =
                    FindFirstUnclaimedGamepad(claimed);

                if (!available)
                    continue;

                adapter.AssignGamepad(available);
                claimed.Add(available.deviceId);
            }

            CaptureConnectionState(true);
        }

        private NovaInputSystemAdapter AdapterAt(int slot)
        {
            if (
                players == null ||
                slot < 0 ||
                slot >= players.Length
            )
            {
                return null;
            }

            return players[slot];
        }

        private void CaptureConnectionState(
            bool raiseChanges)
        {
            if (players == null)
                return;

            int count =
                Mathf.Min(
                    connectedState.Length,
                    players.Length
                );

            for (int i = 0; i < count; i++)
            {
                NovaInputSystemAdapter adapter =
                    players[i];

                bool connected =
                    adapter &&
                    adapter.HasAssignedGamepad;

                if (
                    raiseChanges &&
                    connectionStateInitialized &&
                    connectedState[i] != connected
                )
                {
                    DeviceConnectionChanged?.Invoke(
                        i,
                        connected,
                        adapter
                            ? adapter.AssignedDeviceId
                            : -1
                    );
                }

                connectedState[i] = connected;
            }

            connectionStateInitialized = true;
        }

        private static Gamepad FindFirstUnclaimedGamepad(
            HashSet<int> claimed)
        {
            for (int i = 0; i < Gamepad.all.Count; i++)
            {
                Gamepad candidate = Gamepad.all[i];

                if (
                    candidate &&
                    !claimed.Contains(candidate.deviceId)
                )
                {
                    return candidate;
                }
            }

            return null;
        }

        private void OnDeviceChange(
            InputDevice device,
            InputDeviceChange change)
        {
            if (device is not Gamepad)
                return;

            switch (change)
            {
                case InputDeviceChange.Added:
                case InputDeviceChange.Reconnected:
                case InputDeviceChange.Enabled:
                case InputDeviceChange.Disconnected:
                case InputDeviceChange.Removed:
                    ReconcileGamepads();
                    break;
            }
        }
    }
}
