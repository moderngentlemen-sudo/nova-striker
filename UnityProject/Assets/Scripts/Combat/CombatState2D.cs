using System.Collections.Generic;
using NovaStriker.Core;
using UnityEngine;

namespace NovaStriker.Combat
{
    /// <summary>
    /// Layered combat state shared by players, standard enemies, mini-bosses,
    /// and Guardians. Health remains in Damageable2D; this component owns
    /// shields, armor, Break/stagger, and timed weapon statuses.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatState2D : MonoBehaviour
    {
        [SerializeField] private Damageable2D damageable;

        [Header("Shield")]
        [SerializeField, Min(0f)] private float maxShield;
        [SerializeField, Min(0f)] private float shieldRechargeDelay = 4.5f;
        [SerializeField, Min(0f)] private float shieldRechargePerSecond;

        [Header("Armor")]
        [SerializeField, Min(0f)] private float maxArmor;
        [SerializeField, Range(0f, 0.8f)] private float armorDamageReduction = 0.24f;

        [Header("Break")]
        [SerializeField, Min(0f)] private float breakMax = 80f;
        [SerializeField, Min(0f)] private float breakDecayPerSecond = 10f;
        [SerializeField, Min(0f)] private float breakStaggerDuration = 0.75f;

        [Header("Statuses")]
        [SerializeField, Range(0f, 1f)] private float cryoSlowMultiplier = 0.55f;

        [Header("Shock Chain")]
        [SerializeField, Min(0f)] private float shockChainRadius = 3.2f;
        [SerializeField, Range(0, 4)] private int shockChainMaxTargets = 2;
        [SerializeField, Min(0f)] private float shockChainBaseDamage = 5f;
        [SerializeField, Min(0f)] private float shockChainDamagePerTier = 2.5f;
        [SerializeField, Min(0f)] private float shockChainCooldown = 0.18f;

        private static readonly List<CombatState2D> ActiveStates = new(64);

        private readonly List<CombatState2D> shockChainScratch = new(4);

        private float shieldRechargeTimer;
        private float burnTimer;
        private float burnDamagePerSecond;
        private float markTimer;
        private int markStacks;
        private float vulnerableTimer;
        private float exposedTimer;
        private float shockTimer;
        private float shockChainTimer;
        private float staggerTimer;

        public float Shield { get; private set; }
        public float MaxShield => maxShield;
        public float Armor { get; private set; }
        public float MaxArmor => maxArmor;
        public float BreakGauge { get; private set; }
        public float BreakMax => breakMax;

        public bool ShieldBroken => maxShield > 0f && Shield <= 0f;
        public bool ArmorBroken => maxArmor > 0f && Armor <= 0f;
        public bool IsStaggered => staggerTimer > 0f;
        public bool IsBurning => burnTimer > 0f;
        public bool IsMarked => markTimer > 0f && markStacks > 0;
        public bool IsVulnerable => vulnerableTimer > 0f;
        public bool IsExposed => exposedTimer > 0f;
        public bool IsShocked => shockTimer > 0f;
        public bool IsSlowed => cryoSlowTimer > 0f;

        private float cryoSlowTimer;

        public int MarkStacks => markStacks;

        public float MovementMultiplier =>
            IsSlowed
                ? cryoSlowMultiplier
                : 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            ActiveStates.Clear();
        }

        private void Reset()
        {
            damageable = GetComponent<Damageable2D>();
        }

        private void OnEnable()
        {
            if (!ActiveStates.Contains(this))
                ActiveStates.Add(this);
        }

        private void OnDisable()
        {
            ActiveStates.Remove(this);
        }

        private void Awake()
        {
            if (!damageable)
                damageable = GetComponent<Damageable2D>();

            ResetLayers();
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            staggerTimer = Mathf.Max(0f, staggerTimer - dt);
            vulnerableTimer = Mathf.Max(0f, vulnerableTimer - dt);
            exposedTimer = Mathf.Max(0f, exposedTimer - dt);
            shockTimer = Mathf.Max(0f, shockTimer - dt);
            shockChainTimer = Mathf.Max(0f, shockChainTimer - dt);
            cryoSlowTimer = Mathf.Max(0f, cryoSlowTimer - dt);

            if (markTimer > 0f)
            {
                markTimer = Mathf.Max(0f, markTimer - dt);

                if (markTimer <= 0f)
                    markStacks = 0;
            }

            if (burnTimer > 0f)
            {
                burnTimer = Mathf.Max(0f, burnTimer - dt);

                if (
                    damageable &&
                    !damageable.IsDefeated &&
                    burnDamagePerSecond > 0f
                )
                {
                    damageable.ApplyDamage(
                        new DamagePacket(
                            burnDamagePerSecond * dt,
                            Vector2.zero,
                            transform.position,
                            CombatFaction.Neutral,
                            -1,
                            0,
                            "status-burn"
                        )
                    );
                }
            }

            if (!IsStaggered && BreakGauge > 0f)
            {
                BreakGauge =
                    Mathf.Max(
                        0f,
                        BreakGauge -
                        breakDecayPerSecond * dt
                    );
            }

            if (maxShield <= 0f || Shield >= maxShield)
                return;

            shieldRechargeTimer =
                Mathf.Max(0f, shieldRechargeTimer - dt);

            if (
                shieldRechargeTimer <= 0f &&
                shieldRechargePerSecond > 0f
            )
            {
                Shield =
                    Mathf.Min(
                        maxShield,
                        Shield +
                        shieldRechargePerSecond * dt
                    );
            }
        }

        /// <summary>
        /// Applies defensive layers and damage multipliers. Returns true when
        /// the incoming hit contacted a defensive layer even if no health
        /// damage remains.
        /// </summary>
        public bool ResolveIncomingDamage(
            ref DamagePacket packet)
        {
            float incoming =
                Mathf.Max(0f, packet.Damage);

            bool contactedDefense = false;

            if (incoming <= 0f)
            {
                packet.Damage = 0f;
                return false;
            }

            shieldRechargeTimer = shieldRechargeDelay;

            if (Shield > 0f)
            {
                contactedDefense = true;

                float absorbed =
                    Mathf.Min(Shield, incoming);

                Shield -= absorbed;
                incoming -= absorbed;

                if (Shield <= 0f)
                {
                    RaiseLayerCue(
                        GameplayCueType.ShieldBroken,
                        packet,
                        maxShield,
                        "shield-break"
                    );
                }
            }

            if (incoming > 0f && Armor > 0f)
            {
                contactedDefense = true;

                bool bypass =
                    packet.WeaponId == "rail" ||
                    packet.WeaponId == "spear" ||
                    packet.WeaponId == "null";

                float raw = incoming;

                if (!bypass)
                {
                    incoming *=
                        1f -
                        Mathf.Clamp01(armorDamageReduction);
                }

                float armorDamage =
                    raw *
                    (bypass ? 0.85f : 0.42f);

                Armor =
                    Mathf.Max(
                        0f,
                        Armor - armorDamage
                    );

                if (Armor <= 0f)
                {
                    RaiseLayerCue(
                        GameplayCueType.ArmorBroken,
                        packet,
                        maxArmor,
                        "armor-break"
                    );
                }
            }

            if (markStacks > 0)
                incoming *= 1f + markStacks * 0.04f;

            if (IsVulnerable)
                incoming *= 1.15f;

            if (IsExposed)
                incoming *= 1.12f;

            packet.Damage = Mathf.Max(0f, incoming);

            if (packet.Damage > 0f && breakMax > 0f)
            {
                BreakGauge +=
                    packet.Damage * 0.72f;

                if (BreakGauge >= breakMax)
                    TriggerBreak(packet);
            }

            return contactedDefense;
        }

        public void ApplyWeaponStatus(
            string weaponId,
            int tier,
            int sourcePlayerId = -1,
            CombatFaction sourceFaction = CombatFaction.Neutral)
        {
            if (string.IsNullOrEmpty(weaponId))
                return;

            tier = Mathf.Clamp(tier, 0, 3);

            switch (weaponId)
            {
                case "pulse":
                    markTimer = 2.6f;
                    markStacks =
                        Mathf.Clamp(
                            markStacks + 1,
                            0,
                            3
                        );
                    break;

                case "arc":
                    shockTimer =
                        Mathf.Max(
                            shockTimer,
                            1.8f + tier * 0.25f
                        );
                    break;

                case "volt":
                    shockTimer =
                        Mathf.Max(shockTimer, 2.5f);
                    break;

                case "rail":
                    DamageArmorInternal(18f + tier * 14f);
                    exposedTimer =
                        Mathf.Max(exposedTimer, 1.3f);
                    break;

                case "cryo":
                    cryoSlowTimer =
                        Mathf.Max(
                            cryoSlowTimer,
                            2.2f + tier * 0.45f
                        );

                    if (tier >= 3)
                    {
                        staggerTimer =
                            Mathf.Max(staggerTimer, 0.7f);
                    }
                    break;

                case "nova":
                    exposedTimer =
                        Mathf.Max(
                            exposedTimer,
                            2f + tier * 0.3f
                        );
                    break;

                case "spear":
                    DamageArmorInternal(22f + tier * 16f);
                    break;

                case "gravity":
                    vulnerableTimer =
                        Mathf.Max(vulnerableTimer, 2.8f);
                    break;

                case "magma":
                    burnTimer =
                        Mathf.Max(
                            burnTimer,
                            2.8f + tier * 0.4f
                        );

                    burnDamagePerSecond =
                        Mathf.Max(
                            burnDamagePerSecond,
                            8f + tier * 5f
                        );
                    break;

                case "mines":
                    staggerTimer =
                        Mathf.Max(
                            staggerTimer,
                            0.5f + tier * 0.12f
                        );
                    break;

                case "null":
                    DamageArmorInternal(35f + tier * 22f);
                    vulnerableTimer =
                        Mathf.Max(vulnerableTimer, 1.5f);
                    break;
            }

            if (
                (weaponId == "arc" || weaponId == "volt") &&
                IsShocked &&
                shockChainTimer <= 0f
            )
            {
                TryChainShock(
                    tier,
                    sourcePlayerId,
                    sourceFaction
                );
            }

            GameplayEventHub.Raise(
                new GameplayCue(
                    GameplayCueType.StatusApplied,
                    damageable ? damageable.ActorId : -1,
                    transform.position,
                    Vector2.zero,
                    tier,
                    0f,
                    weaponId
                )
            );
        }

        private void TryChainShock(
            int tier,
            int sourcePlayerId,
            CombatFaction sourceFaction)
        {
            if (
                shockChainMaxTargets <= 0 ||
                shockChainRadius <= 0f ||
                !damageable ||
                damageable.IsDefeated
            )
            {
                return;
            }

            shockChainScratch.Clear();

            int desiredTargets =
                Mathf.Clamp(
                    1 + tier / 2,
                    1,
                    shockChainMaxTargets
                );

            float radiusSq =
                shockChainRadius * shockChainRadius;

            for (int slot = 0; slot < desiredTargets; slot++)
            {
                CombatState2D best = null;
                float bestDistanceSq = float.PositiveInfinity;

                for (int i = 0; i < ActiveStates.Count; i++)
                {
                    CombatState2D candidate =
                        ActiveStates[i];

                    if (
                        !candidate ||
                        candidate == this ||
                        shockChainScratch.Contains(candidate) ||
                        !candidate.damageable ||
                        candidate.damageable.IsDefeated ||
                        candidate.damageable.Faction != damageable.Faction
                    )
                    {
                        continue;
                    }

                    float distanceSq =
                        (
                            candidate.transform.position -
                            transform.position
                        ).sqrMagnitude;

                    if (
                        distanceSq > radiusSq ||
                        distanceSq >= bestDistanceSq
                    )
                    {
                        continue;
                    }

                    bestDistanceSq = distanceSq;
                    best = candidate;
                }

                if (!best)
                    break;

                shockChainScratch.Add(best);
                best.ApplyChainedShock(
                    transform.position,
                    tier,
                    sourcePlayerId,
                    sourceFaction,
                    shockChainBaseDamage +
                    shockChainDamagePerTier * tier
                );
            }

            if (shockChainScratch.Count > 0)
                shockChainTimer = shockChainCooldown;
        }

        private void ApplyChainedShock(
            Vector3 sourcePosition,
            int tier,
            int sourcePlayerId,
            CombatFaction sourceFaction,
            float damage)
        {
            shockTimer =
                Mathf.Max(
                    shockTimer,
                    1.0f + tier * 0.20f
                );

            Vector2 direction =
                (
                    (Vector2)transform.position -
                    (Vector2)sourcePosition
                );

            if (direction.sqrMagnitude > 0.0001f)
                direction.Normalize();

            if (
                damageable &&
                !damageable.IsDefeated &&
                damage > 0f
            )
            {
                damageable.ApplyDamage(
                    new DamagePacket(
                        damage,
                        direction * (0.6f + tier * 0.2f),
                        transform.position,
                        sourceFaction,
                        sourcePlayerId,
                        tier,
                        "status-shock-chain"
                    )
                );
            }

            GameplayEventHub.Raise(
                new GameplayCue(
                    GameplayCueType.StatusApplied,
                    damageable ? damageable.ActorId : -1,
                    transform.position,
                    direction,
                    tier,
                    damage,
                    "shock-chain"
                )
            );
        }

        public void ApplyVulnerable(float seconds)
        {
            vulnerableTimer =
                Mathf.Max(
                    vulnerableTimer,
                    Mathf.Max(0f, seconds)
                );
        }

        public void ApplyExposed(float seconds)
        {
            exposedTimer =
                Mathf.Max(
                    exposedTimer,
                    Mathf.Max(0f, seconds)
                );
        }

        public void ApplyStagger(float seconds)
        {
            staggerTimer =
                Mathf.Max(
                    staggerTimer,
                    Mathf.Max(0f, seconds)
                );
        }

        public void ApplySlow(float seconds)
        {
            cryoSlowTimer =
                Mathf.Max(
                    cryoSlowTimer,
                    Mathf.Max(0f, seconds)
                );
        }

        public void AddBreak(
            float amount,
            DamagePacket source)
        {
            if (breakMax <= 0f || amount <= 0f)
                return;

            BreakGauge += amount;

            if (BreakGauge >= breakMax)
                TriggerBreak(source);
        }

        public void ConfigureDefense(
            float shield,
            float armor,
            float breakCapacity,
            float damageReduction = 0.24f,
            bool refill = true)
        {
            maxShield = Mathf.Max(0f, shield);
            maxArmor = Mathf.Max(0f, armor);
            breakMax = Mathf.Max(0f, breakCapacity);
            armorDamageReduction =
                Mathf.Clamp(damageReduction, 0f, 0.8f);

            if (refill)
                ResetLayers();
            else
            {
                Shield = Mathf.Min(Shield, maxShield);
                Armor = Mathf.Min(Armor, maxArmor);
                BreakGauge = Mathf.Min(BreakGauge, breakMax);
            }
        }

        public float DamageShield(float amount)
        {
            if (Shield <= 0f || amount <= 0f)
                return 0f;

            float before = Shield;
            Shield = Mathf.Max(0f, Shield - amount);

            if (before > 0f && Shield <= 0f)
            {
                GameplayEventHub.Raise(
                    new GameplayCue(
                        GameplayCueType.ShieldBroken,
                        damageable ? damageable.ActorId : -1,
                        transform.position,
                        Vector2.zero,
                        0,
                        maxShield,
                        "shield-break-reaction"
                    )
                );
            }

            return before - Shield;
        }

        public float DamageArmor(float amount)
        {
            if (Armor <= 0f || amount <= 0f)
                return 0f;

            float before = Armor;
            DamageArmorInternal(amount);
            return before - Armor;
        }

        public float RestoreShield(float amount)
        {
            if (maxShield <= 0f || amount <= 0f)
                return 0f;

            float before = Shield;

            Shield =
                Mathf.Clamp(
                    Shield + amount,
                    0f,
                    maxShield
                );

            return Shield - before;
        }

        public float RestoreArmor(float amount)
        {
            if (maxArmor <= 0f || amount <= 0f)
                return 0f;

            float before = Armor;

            Armor =
                Mathf.Clamp(
                    Armor + amount,
                    0f,
                    maxArmor
                );

            return Armor - before;
        }

        public void RestoreLayers()
        {
            ResetLayers();
        }

        private void ResetLayers()
        {
            Shield = Mathf.Max(0f, maxShield);
            Armor = Mathf.Max(0f, maxArmor);
            BreakGauge = 0f;
            shieldRechargeTimer = 0f;
            burnTimer = 0f;
            burnDamagePerSecond = 0f;
            markTimer = 0f;
            markStacks = 0;
            vulnerableTimer = 0f;
            exposedTimer = 0f;
            shockTimer = 0f;
            shockChainTimer = 0f;
            staggerTimer = 0f;
            cryoSlowTimer = 0f;
        }

        private void DamageArmorInternal(float amount)
        {
            if (Armor <= 0f || amount <= 0f)
                return;

            Armor =
                Mathf.Max(
                    0f,
                    Armor - amount
                );

            if (Armor <= 0f && maxArmor > 0f)
            {
                GameplayEventHub.Raise(
                    new GameplayCue(
                        GameplayCueType.ArmorBroken,
                        damageable ? damageable.ActorId : -1,
                        transform.position,
                        Vector2.zero,
                        0,
                        maxArmor,
                        "armor-break-status"
                    )
                );
            }
        }

        private void TriggerBreak(DamagePacket packet)
        {
            BreakGauge = 0f;
            staggerTimer =
                Mathf.Max(
                    staggerTimer,
                    breakStaggerDuration
                );

            GameplayEventHub.Raise(
                new GameplayCue(
                    GameplayCueType.GuardBroken,
                    damageable ? damageable.ActorId : -1,
                    transform.position,
                    packet.Knockback.normalized,
                    packet.Tier,
                    breakStaggerDuration,
                    "guard-break"
                )
            );
        }

        private void RaiseLayerCue(
            GameplayCueType type,
            DamagePacket packet,
            float value,
            string id)
        {
            GameplayEventHub.Raise(
                new GameplayCue(
                    type,
                    damageable ? damageable.ActorId : -1,
                    transform.position,
                    packet.Knockback.normalized,
                    packet.Tier,
                    value,
                    id
                )
            );
        }
    }
}
