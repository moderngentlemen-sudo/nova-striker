using System;
using NovaStriker.Combat;
using NovaStriker.Core;
using NovaStriker.Player;
using NovaStriker.Session;
using UnityEngine;

namespace NovaStriker.Campaign
{
    /// <summary>
    /// Runtime implementation for the six sector setpiece families. Final
    /// environment meshes and animation can bind to the exposed transforms
    /// while gameplay timing remains independent.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SectorSetpieceController2D : MonoBehaviour
    {
        [SerializeField] private SetpieceId setpieceId;
        [SerializeField] private float duration = 7f;
        [SerializeField] private bool startOnEnable;

        [Header("Shared References")]
        [SerializeField] private SectorHazard2D[] hazards;
        [SerializeField] private Transform[] movingParts;
        [SerializeField] private Collider2D[] collapsingPlatforms;

        [Header("Pulse")]
        [SerializeField] private float pulseInterval = 1.1f;
        [SerializeField] private float hazardForceDuration = 0.32f;

        [Header("Motion")]
        [SerializeField] private float motionAmplitude = 0.55f;
        [SerializeField] private float motionSpeed = 2f;

        [Header("Null Warp")]
        [SerializeField] private float nullImpulse = 2.2f;

        private Vector3[] initialPositions;
        private bool[] initialColliderStates;
        private float timer;
        private float pulseTimer;
        private float phase;

        public bool Active { get; private set; }
        public SetpieceId SetpieceId => setpieceId;

        public event Action Started;
        public event Action Ended;

        private void Awake()
        {
            CacheInitialState();
        }

        private void OnEnable()
        {
            if (startOnEnable)
                StartSetpiece();
        }

        private void OnDisable()
        {
            RestoreInitialState();
        }

        private void Update()
        {
            if (!Active)
                return;

            float dt = Time.deltaTime;

            timer =
                Mathf.Max(0f, timer - dt);

            phase += dt * motionSpeed;
            pulseTimer -= dt;

            if (pulseTimer <= 0f)
            {
                pulseTimer =
                    Mathf.Max(0.1f, pulseInterval);

                RunPulse();
            }

            UpdateContinuousMotion();

            if (timer <= 0f)
                StopSetpiece();
        }

        public void StartSetpiece()
        {
            if (Active)
                return;

            CacheInitialState();

            Active = true;
            timer = Mathf.Max(0.1f, duration);
            pulseTimer = 0f;
            phase = 0f;

            Started?.Invoke();

            GameplayEventHub.Raise(
                new GameplayCue(
                    GameplayCueType.SetpieceStarted,
                    -1,
                    transform.position,
                    Vector2.up,
                    (int)setpieceId,
                    duration,
                    setpieceId.ToString()
                )
            );
        }

        public void StopSetpiece()
        {
            if (!Active)
                return;

            Active = false;
            RestoreInitialState();

            Ended?.Invoke();

            GameplayEventHub.Raise(
                new GameplayCue(
                    GameplayCueType.SetpieceEnded,
                    -1,
                    transform.position,
                    Vector2.up,
                    (int)setpieceId,
                    0f,
                    setpieceId.ToString()
                )
            );
        }

        private void RunPulse()
        {
            switch (setpieceId)
            {
                case SetpieceId.TrainRush:
                case SetpieceId.FurnaceSurge:
                case SetpieceId.LightningChase:
                    ForceHazards();
                    break;

                case SetpieceId.IceCollapse:
                    ToggleCollapsePlatforms();
                    break;

                case SetpieceId.NullWarp:
                    ApplyNullWarpImpulse();
                    break;
            }
        }

        private void UpdateContinuousMotion()
        {
            if (
                setpieceId != SetpieceId.VineBridge ||
                movingParts == null ||
                initialPositions == null
            )
            {
                return;
            }

            for (int i = 0; i < movingParts.Length; i++)
            {
                Transform part = movingParts[i];

                if (!part || i >= initialPositions.Length)
                    continue;

                float offset =
                    Mathf.Sin(phase + i * 0.65f) *
                    motionAmplitude;

                part.position =
                    initialPositions[i] +
                    Vector3.up * offset;
            }
        }

        private void ForceHazards()
        {
            if (hazards == null)
                return;

            for (int i = 0; i < hazards.Length; i++)
            {
                hazards[i]?.ForceActive(
                    hazardForceDuration
                );
            }
        }

        private void ToggleCollapsePlatforms()
        {
            if (collapsingPlatforms == null)
                return;

            for (int i = 0; i < collapsingPlatforms.Length; i++)
            {
                Collider2D platform =
                    collapsingPlatforms[i];

                if (!platform)
                    continue;

                platform.enabled = !platform.enabled;
            }
        }

        private void ApplyNullWarpImpulse()
        {
            StrikeTeamSession session =
                StrikeTeamSession.Active;

            if (!session)
                return;

            float direction =
                Mathf.Sin(phase) >= 0f
                    ? 1f
                    : -1f;

            for (int i = 0; i < StrikeTeamSession.MaxPlayers; i++)
            {
                StrikerPlayerIdentity player =
                    session.GetPlayer(i);

                if (!player || !player.IsCombatReady)
                    continue;

                Damageable2D health =
                    player.GetComponent<Damageable2D>();

                health?.AddExternalVelocity(
                    new Vector2(
                        direction * nullImpulse,
                        0f
                    )
                );
            }
        }

        private void CacheInitialState()
        {
            if (movingParts != null)
            {
                initialPositions =
                    new Vector3[movingParts.Length];

                for (int i = 0; i < movingParts.Length; i++)
                {
                    if (movingParts[i])
                    {
                        initialPositions[i] =
                            movingParts[i].position;
                    }
                }
            }

            if (collapsingPlatforms != null)
            {
                initialColliderStates =
                    new bool[collapsingPlatforms.Length];

                for (int i = 0; i < collapsingPlatforms.Length; i++)
                {
                    initialColliderStates[i] =
                        collapsingPlatforms[i] &&
                        collapsingPlatforms[i].enabled;
                }
            }
        }

        private void RestoreInitialState()
        {
            if (
                movingParts != null &&
                initialPositions != null
            )
            {
                for (int i = 0; i < movingParts.Length; i++)
                {
                    if (
                        movingParts[i] &&
                        i < initialPositions.Length
                    )
                    {
                        movingParts[i].position =
                            initialPositions[i];
                    }
                }
            }

            if (
                collapsingPlatforms != null &&
                initialColliderStates != null
            )
            {
                for (int i = 0; i < collapsingPlatforms.Length; i++)
                {
                    if (
                        collapsingPlatforms[i] &&
                        i < initialColliderStates.Length
                    )
                    {
                        collapsingPlatforms[i].enabled =
                            initialColliderStates[i];
                    }
                }
            }
        }
    }
}
