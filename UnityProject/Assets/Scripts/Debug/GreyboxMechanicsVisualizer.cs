using NovaStriker.Combat;
using NovaStriker.Core;
using NovaStriker.Player;
using UnityEngine;

namespace NovaStriker.Debugging
{
    /// <summary>
    /// Lightweight presentation layer for the mechanics lab.
    /// It visualizes traversal velocity and Echo grapple targeting without
    /// owning or changing gameplay timing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GreyboxMechanicsVisualizer : MonoBehaviour
    {
        [SerializeField] private NovaMotor2D motor;
        [SerializeField] private NovaCombatController combat;

        [Header("Movement Trail")]
        [SerializeField] private float quickTrailWidth = 0.12f;
        [SerializeField] private float burstTrailWidth = 0.18f;
        [SerializeField] private float velocityTrailWidth = 0.26f;
        [SerializeField] private float grappleTrailWidth = 0.20f;

        [Header("Grapple Tether")]
        [SerializeField] private float tetherWidth = 0.065f;
        [SerializeField] private float lockHoldDuration = 0.18f;
        [SerializeField] private float enemyGrappleHoldDuration = 0.28f;
        [SerializeField] private float traversalHoldDuration = 0.72f;

        private TrailRenderer movementTrail;
        private LineRenderer grappleLine;
        private Material runtimeMaterial;

        private Vector2 rememberedGrappleTarget;
        private float rememberedGrappleTimer;

        private void Reset()
        {
            motor = GetComponent<NovaMotor2D>();
            combat = GetComponent<NovaCombatController>();
        }

        private void Awake()
        {
            if (!motor)
                motor = GetComponent<NovaMotor2D>();

            if (!combat)
                combat = GetComponent<NovaCombatController>();

            CreateRuntimePresentation();
        }

        private void OnEnable()
        {
            GameplayEventHub.CueRaised += OnGameplayCue;
        }

        private void OnDisable()
        {
            GameplayEventHub.CueRaised -= OnGameplayCue;

            if (movementTrail)
                movementTrail.emitting = false;

            if (grappleLine)
                grappleLine.enabled = false;
        }

        private void OnDestroy()
        {
            if (runtimeMaterial)
                Destroy(runtimeMaterial);
        }

        private void LateUpdate()
        {
            UpdateMovementTrail();
            UpdateGrappleTether();
        }

        private void CreateRuntimePresentation()
        {
            Shader shader =
                Shader.Find("Sprites/Default") ??
                Shader.Find("Unlit/Color") ??
                Shader.Find("Standard");

            if (shader)
            {
                runtimeMaterial = new Material(shader)
                {
                    name = "MAT_Runtime_GreyboxMechanics"
                };
            }

            movementTrail =
                GetComponent<TrailRenderer>();

            if (!movementTrail)
                movementTrail = gameObject.AddComponent<TrailRenderer>();

            movementTrail.time = 0.16f;
            movementTrail.minVertexDistance = 0.05f;
            movementTrail.autodestruct = false;
            movementTrail.emitting = false;
            movementTrail.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            movementTrail.receiveShadows = false;
            movementTrail.textureMode =
                LineTextureMode.Stretch;
            movementTrail.startColor =
                new Color(0.35f, 0.88f, 1f, 0.72f);
            movementTrail.endColor =
                new Color(0.35f, 0.88f, 1f, 0f);

            if (runtimeMaterial)
                movementTrail.sharedMaterial = runtimeMaterial;

            GameObject tetherObject =
                new("Greybox_GrappleTether");

            tetherObject.transform.SetParent(
                transform,
                false
            );

            grappleLine =
                tetherObject.AddComponent<LineRenderer>();

            grappleLine.useWorldSpace = true;
            grappleLine.positionCount = 2;
            grappleLine.startWidth = tetherWidth;
            grappleLine.endWidth = tetherWidth * 0.62f;
            grappleLine.numCapVertices = 3;
            grappleLine.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            grappleLine.receiveShadows = false;
            grappleLine.startColor =
                new Color(0.72f, 0.94f, 1f, 0.90f);
            grappleLine.endColor =
                new Color(0.30f, 0.82f, 1f, 0.62f);
            grappleLine.enabled = false;

            if (runtimeMaterial)
                grappleLine.sharedMaterial = runtimeMaterial;
        }

        private void UpdateMovementTrail()
        {
            if (!movementTrail || !motor)
                return;

            bool active =
                motor.IsDashing ||
                motor.IsSliding ||
                motor.IsGrapplingTraversal;

            movementTrail.emitting = active;

            if (!active)
                return;

            float width;

            if (motor.IsGrapplingTraversal)
            {
                width = grappleTrailWidth;
                movementTrail.time = 0.20f;
            }
            else
            {
                ChargeTier tier =
                    motor.IsSliding
                        ? motor.ActiveSlideTier
                        : motor.ActiveDashTier;

                width = tier switch
                {
                    ChargeTier.Quick => quickTrailWidth,
                    ChargeTier.Burst => burstTrailWidth,
                    _ => velocityTrailWidth
                };

                movementTrail.time = tier switch
                {
                    ChargeTier.Quick => 0.12f,
                    ChargeTier.Burst => 0.18f,
                    _ => 0.26f
                };
            }

            movementTrail.startWidth = width;
            movementTrail.endWidth = width * 0.12f;
        }

        private void UpdateGrappleTether()
        {
            if (!grappleLine || !combat)
                return;

            Vector2 target;
            bool show;

            if (combat.HasGrappleLock)
            {
                target = combat.GrappleLockPosition;
                rememberedGrappleTarget = target;
                show = true;
            }
            else if (rememberedGrappleTimer > 0f)
            {
                rememberedGrappleTimer =
                    Mathf.Max(
                        0f,
                        rememberedGrappleTimer -
                        Time.deltaTime
                    );

                target = rememberedGrappleTarget;
                show = true;
            }
            else
            {
                target = default;
                show = false;
            }

            grappleLine.enabled = show;

            if (!show)
                return;

            grappleLine.SetPosition(
                0,
                transform.position
            );

            grappleLine.SetPosition(
                1,
                target
            );
        }

        private void OnGameplayCue(GameplayCue cue)
        {
            if (!motor || cue.ActorId != motor.PlayerId)
                return;

            switch (cue.Type)
            {
                case GameplayCueType.CounterGrappleLock:
                    RememberGrappleTarget(
                        cue.Position,
                        lockHoldDuration
                    );
                    break;

                case GameplayCueType.CounterGrapple:
                    RememberGrappleTarget(
                        cue.Position,
                        enemyGrappleHoldDuration
                    );
                    break;

                case GameplayCueType.CounterGrappleTraversal:
                    RememberGrappleTarget(
                        cue.Position,
                        traversalHoldDuration
                    );
                    break;
            }
        }

        private void RememberGrappleTarget(
            Vector2 position,
            float duration)
        {
            rememberedGrappleTarget = position;
            rememberedGrappleTimer =
                Mathf.Max(
                    rememberedGrappleTimer,
                    duration
                );
        }
    }
}
