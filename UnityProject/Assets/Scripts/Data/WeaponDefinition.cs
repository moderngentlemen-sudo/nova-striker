using UnityEngine;

namespace NovaStriker.Data
{
    public enum WeaponBehavior
    {
        Standard,
        Spread,
        Pierce,
        Boomerang,
        Cryo,
        Beam,
        Spear,
        Gravity,
        Magma,
        Cyclone,
        Mine,
        Null
    }

    [CreateAssetMenu(menuName = "Nova Striker/Weapon Definition", fileName = "Weapon_")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string Id;
        public string DisplayName;
        public WeaponBehavior Behavior;

        [Header("Projectile")]
        [Min(0f)] public float ProjectileSpeed = 780f;
        public Color EnergyColor = Color.cyan;

        [Header("Damage by charge tier")]
        public float UnchargedDamage = 8f;
        public float Tier1Damage = 16f;
        public float Tier2Damage = 30f;
        public float Tier3Damage = 52f;

        public float DamageForTier(int tier) => tier switch
        {
            <= 0 => UnchargedDamage,
            1 => Tier1Damage,
            2 => Tier2Damage,
            _ => Tier3Damage
        };
    }
}
