using NovaStriker.Combat;
using UnityEngine;

namespace NovaStriker.Player
{
    /// <summary>
    /// Four-player co-op downed/revive state. Gameplay components are disabled
    /// while downed, but the player object remains present for teammate rescue.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StrikerDownedState : MonoBehaviour
    {
        [SerializeField] private Damageable2D damageable;
        [SerializeField] private NovaMotor2D motor;
        [SerializeField] private NovaCombatController combat;
        [SerializeField] private Rigidbody2D body;

        [Header("Revive")]
        [SerializeField] private float reviveDuration = 2.5f;
        [SerializeField, Range(0.05f, 1f)] private float reviveHealthFraction = 0.45f;
        [SerializeField] private float reviveInvulnerability = 1.0f;
        [SerializeField] private float reviveProgressDecayPerSecond = 0.70f;

        private float reviveProgress;
        private bool receivedProgressThisStep;

        public bool IsDowned { get; private set; }
        public float ReviveProgress => reviveProgress;
        public float ReviveProgressNormalized =>
            reviveDuration > 0f
                ? Mathf.Clamp01(reviveProgress / reviveDuration)
                : 1f;

        private void Reset()
        {
            damageable = GetComponent<Damageable2D>();
            motor = GetComponent<NovaMotor2D>();
            combat = GetComponent<NovaCombatController>();
            body = GetComponent<Rigidbody2D>();
        }

        private void Awake()
        {
            if (!damageable)
                damageable = GetComponent<Damageable2D>();

            if (!motor)
                motor = GetComponent<NovaMotor2D>();

            if (!combat)
                combat = GetComponent<NovaCombatController>();

            if (!body)
                body = GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            if (damageable)
            {
                damageable.Defeated += OnDefeated;
                damageable.Revived += OnRevived;
            }
        }

        private void OnDisable()
        {
            if (damageable)
            {
                damageable.Defeated -= OnDefeated;
                damageable.Revived -= OnRevived;
            }
        }

        private void FixedUpdate()
        {
            if (!IsDowned)
            {
                reviveProgress = 0f;
                receivedProgressThisStep = false;
                return;
            }

            if (!receivedProgressThisStep)
            {
                reviveProgress =
                    Mathf.Max(
                        0f,
                        reviveProgress -
                        reviveProgressDecayPerSecond *
                        Time.fixedDeltaTime
                    );
            }

            receivedProgressThisStep = false;
        }

        public void AddReviveProgress(float seconds)
        {
            if (!IsDowned || seconds <= 0f)
                return;

            receivedProgressThisStep = true;

            reviveProgress += seconds;

            if (reviveProgress + 0.0001f < reviveDuration)
                return;

            damageable?.Revive(
                reviveHealthFraction,
                reviveInvulnerability
            );
        }

        private void OnDefeated(DamagePacket packet)
        {
            IsDowned = true;
            reviveProgress = 0f;
            receivedProgressThisStep = false;

            if (body)
                body.linearVelocity = Vector2.zero;

            if (motor)
                motor.enabled = false;

            if (combat)
                combat.enabled = false;
        }

        private void OnRevived()
        {
            IsDowned = false;
            reviveProgress = 0f;
            receivedProgressThisStep = false;

            if (motor)
                motor.enabled = true;

            if (combat)
                combat.enabled = true;
        }
    }
}
