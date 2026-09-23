using NovaStriker.Combat;
using UnityEngine;

namespace NovaStriker.Campaign
{
    /// <summary>
    /// Source-derived sector hazard timing. The collider defines the gameplay
    /// volume; production environment art can later provide its presentation.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class SectorHazard2D : MonoBehaviour
    {
        [SerializeField] private HazardId hazard;
        [SerializeField] private float phaseOffset;
        [SerializeField] private float cycleDuration = 2.8f;
        [SerializeField] private float damageCooldown = 0.45f;

        private float clock;
        private bool active;
        private readonly System.Collections.Generic.Dictionary<int, float>
            actorCooldowns = new();

        public bool Active => active;

        private void Reset()
        {
            Collider2D collider =
                GetComponent<Collider2D>();

            if (collider)
                collider.isTrigger = true;
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            clock += dt;
            UpdateCooldowns(dt);

            float c =
                Mathf.Repeat(
                    clock + phaseOffset,
                    Mathf.Max(0.1f, cycleDuration)
                );

            active =
                hazard switch
                {
                    HazardId.Laser => c < 1.20f,
                    HazardId.Vent => c > 0.90f && c < 1.55f,
                    HazardId.Spore => c < 1.85f,
                    HazardId.Ice => c > 0.60f && c < 1.15f,
                    HazardId.Lightning => c > 1.40f && c < 1.72f,
                    HazardId.NullGrid => c < 0.95f,
                    _ => false
                };
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!active)
                return;

            Damageable2D target =
                other.GetComponentInParent<Damageable2D>();

            if (
                !target ||
                target.Faction != CombatFaction.Player ||
                target.IsDefeated
            )
            {
                return;
            }

            if (hazard == HazardId.Spore)
            {
                target
                    .GetComponent<CombatState2D>()
                    ?.ApplySlow(0.25f);
                return;
            }

            if (
                actorCooldowns.TryGetValue(
                    target.ActorId,
                    out float remaining
                ) &&
                remaining > 0f
            )
            {
                return;
            }

            float damage =
                hazard == HazardId.Lightning
                    ? 18f
                    : 12f;

            target.ApplyDamage(
                new DamagePacket(
                    damage,
                    Vector2.zero,
                    target.transform.position,
                    CombatFaction.Neutral,
                    -1,
                    0,
                    "hazard-" +
                    hazard.ToString().ToLowerInvariant()
                )
            );

            actorCooldowns[target.ActorId] =
                damageCooldown;
        }

        private void UpdateCooldowns(float dt)
        {
            if (actorCooldowns.Count == 0)
                return;

            int[] keys =
                new int[actorCooldowns.Count];

            actorCooldowns.Keys.CopyTo(keys, 0);

            for (int i = 0; i < keys.Length; i++)
            {
                int key = keys[i];

                actorCooldowns[key] =
                    Mathf.Max(
                        0f,
                        actorCooldowns[key] - dt
                    );
            }
        }
    }
}
