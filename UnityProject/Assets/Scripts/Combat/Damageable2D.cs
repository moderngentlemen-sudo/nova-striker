using System;
using NovaStriker.Core;
using UnityEngine;

namespace NovaStriker.Combat
{
    /// <summary>
    /// Minimal damage receiver for the greybox combat layer.
    /// Enemy/Guardian-specific armor, Break, status effects, and phase logic
    /// should compose around this component rather than be embedded here.
    /// </summary>
    public sealed class Damageable2D : MonoBehaviour
    {
        [SerializeField] private int actorId = -1;
        [SerializeField] private CombatFaction faction = CombatFaction.Enemy;
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private CombatState2D combatState;
        [SerializeField] private bool destroyOnDefeat;

        public event Action<DamagePacket> Damaged;
        public event Action<DamagePacket> Defeated;
        public event Action Revived;

        public int ActorId => actorId;
        public CombatFaction Faction => faction;
        public float MaxHealth => maxHealth;
        public float Health { get; private set; }
        public bool IsDefeated => Health <= 0f;
        public bool IsInvulnerable => invulnerabilityRemaining > 0f;

        private float invulnerabilityRemaining;

        private void Reset()
        {
            body = GetComponent<Rigidbody2D>();
            combatState = GetComponent<CombatState2D>();
        }

        private void Awake()
        {
            if (!body)
                body = GetComponent<Rigidbody2D>();

            if (!combatState)
                combatState = GetComponent<CombatState2D>();

            Health = maxHealth;
        }

        private void FixedUpdate()
        {
            if (invulnerabilityRemaining > 0f)
            {
                invulnerabilityRemaining =
                    Mathf.Max(
                        0f,
                        invulnerabilityRemaining - Time.fixedDeltaTime
                    );
            }
        }

        public bool ApplyDamage(DamagePacket packet)
        {
            if (IsDefeated || IsInvulnerable)
                return false;

            if (
                packet.SourceFaction != CombatFaction.Neutral &&
                packet.SourceFaction == faction
            )
            {
                return false;
            }

            bool contactedDefense =
                combatState &&
                combatState.ResolveIncomingDamage(
                    ref packet
                );

            if (packet.Damage <= 0f)
                return contactedDefense;

            Health =
                Mathf.Max(
                    0f,
                    Health - packet.Damage
                );

            if (body && packet.Knockback.sqrMagnitude > 0f)
                body.linearVelocity += packet.Knockback;

            Damaged?.Invoke(packet);

            GameplayEventHub.Raise(new GameplayCue(
                GameplayCueType.DamageTaken,
                actorId,
                packet.HitPoint,
                packet.Knockback.normalized,
                packet.Tier,
                packet.Damage,
                packet.WeaponId
            ));

            if (
                packet.SourceFaction == CombatFaction.Player &&
                packet.SourcePlayerId >= 0
            )
            {
                GameplayEventHub.Raise(new GameplayCue(
                    GameplayCueType.DamageDealt,
                    packet.SourcePlayerId,
                    packet.HitPoint,
                    packet.Knockback.normalized,
                    packet.Tier,
                    packet.Damage,
                    packet.WeaponId
                ));
            }

            if (!IsDefeated)
                return true;

            Defeated?.Invoke(packet);

            GameplayEventHub.Raise(new GameplayCue(
                GameplayCueType.Defeated,
                actorId,
                transform.position,
                Vector2.zero,
                packet.Tier,
                packet.Damage,
                packet.WeaponId
            ));

            if (
                packet.SourceFaction == CombatFaction.Player &&
                packet.SourcePlayerId >= 0
            )
            {
                GameplayEventHub.Raise(new GameplayCue(
                    GameplayCueType.DefeatDealt,
                    packet.SourcePlayerId,
                    transform.position,
                    Vector2.zero,
                    packet.Tier,
                    packet.Damage,
                    packet.WeaponId
                ));
            }

            if (destroyOnDefeat)
                Destroy(gameObject);

            return true;
        }

        public void SetDestroyOnDefeat(bool value)
        {
            destroyOnDefeat = value;
        }

        public void ConfigureMaxHealth(
            float value,
            bool refill = true)
        {
            maxHealth = Mathf.Max(1f, value);

            if (refill)
                Health = maxHealth;
            else
                Health = Mathf.Min(Health, maxHealth);
        }

        public void GrantInvulnerability(float seconds)
        {
            invulnerabilityRemaining =
                Mathf.Max(invulnerabilityRemaining, Mathf.Max(0f, seconds));
        }

        public void ApplyExternalVelocity(Vector2 velocity)
        {
            if (body)
                body.linearVelocity = velocity;
        }

        public void AddExternalVelocity(Vector2 velocity)
        {
            if (body)
                body.linearVelocity += velocity;
        }

        public float Heal(float amount)
        {
            if (IsDefeated || amount <= 0f)
                return 0f;

            float before = Health;

            Health =
                Mathf.Clamp(
                    Health + amount,
                    0f,
                    maxHealth
                );

            return Health - before;
        }

        public bool Revive(
            float healthFraction = 0.45f,
            float invulnerabilitySeconds = 1.0f)
        {
            if (!IsDefeated)
                return false;

            Health =
                Mathf.Clamp(
                    maxHealth *
                    Mathf.Clamp01(healthFraction),
                    1f,
                    maxHealth
                );

            invulnerabilityRemaining =
                Mathf.Max(
                    invulnerabilityRemaining,
                    Mathf.Max(0f, invulnerabilitySeconds)
                );

            combatState?.RestoreLayers();

            Revived?.Invoke();

            GameplayEventHub.Raise(
                new GameplayCue(
                    GameplayCueType.Revived,
                    actorId,
                    transform.position,
                    Vector2.up,
                    0,
                    Health,
                    "revive"
                )
            );

            return true;
        }

        public void RestoreFullHealth()
        {
            Health = maxHealth;
            invulnerabilityRemaining = 0f;
            combatState?.RestoreLayers();
        }
    }
}
