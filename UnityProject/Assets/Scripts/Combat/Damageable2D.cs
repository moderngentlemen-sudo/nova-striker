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
        [SerializeField] private CombatFaction faction = CombatFaction.Enemy;
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private bool destroyOnDefeat;

        public event Action<DamagePacket> Damaged;
        public event Action<DamagePacket> Defeated;

        public CombatFaction Faction => faction;
        public float MaxHealth => maxHealth;
        public float Health { get; private set; }
        public bool IsDefeated => Health <= 0f;
        public bool IsInvulnerable => invulnerabilityRemaining > 0f;

        private float invulnerabilityRemaining;

        private void Reset()
        {
            body = GetComponent<Rigidbody2D>();
        }

        private void Awake()
        {
            if (!body)
                body = GetComponent<Rigidbody2D>();

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

            Health = Mathf.Max(0f, Health - Mathf.Max(0f, packet.Damage));

            if (body && packet.Knockback.sqrMagnitude > 0f)
                body.linearVelocity += packet.Knockback;

            Damaged?.Invoke(packet);

            GameplayEventHub.Raise(new GameplayCue(
                GameplayCueType.DamageTaken,
                packet.SourcePlayerId,
                packet.HitPoint,
                packet.Knockback.normalized,
                packet.Tier,
                packet.Damage,
                packet.WeaponId
            ));

            if (!IsDefeated)
                return true;

            Defeated?.Invoke(packet);

            GameplayEventHub.Raise(new GameplayCue(
                GameplayCueType.Defeated,
                packet.SourcePlayerId,
                transform.position,
                Vector2.zero,
                packet.Tier,
                packet.Damage,
                packet.WeaponId
            ));

            if (destroyOnDefeat)
                Destroy(gameObject);

            return true;
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

        public void RestoreFullHealth()
        {
            Health = maxHealth;
            invulnerabilityRemaining = 0f;
        }
    }
}
