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
        [Tooltip("Unity world units per second. Browser-reference pixel speeds are converted before authoring assets.")]
        [Min(0f)] public float ProjectileSpeed = 15.6f;
        public Color EnergyColor = Color.cyan;

        [Header("Damage by charge tier")]
        public float UnchargedDamage = 8f;
        public float Tier1Damage = 16f;
        public float Tier2Damage = 30f;
        public float Tier3Damage = 52f;

        [Header("Projectile radius by charge tier")]
        [Min(0.01f)] public float UnchargedRadius = 0.08f;
        [Min(0.01f)] public float Tier1Radius = 0.14f;
        [Min(0.01f)] public float Tier2Radius = 0.22f;
        [Min(0.01f)] public float Tier3Radius = 0.36f;

        public float DamageForTier(int tier) => tier switch
        {
            <= 0 => UnchargedDamage,
            1 => Tier1Damage,
            2 => Tier2Damage,
            _ => Tier3Damage
        };

        public float RadiusForTier(int tier) => tier switch
        {
            <= 0 => UnchargedRadius,
            1 => Tier1Radius,
            2 => Tier2Radius,
            _ => Tier3Radius
        };
    }
}
