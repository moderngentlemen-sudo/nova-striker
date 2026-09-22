using UnityEngine;

namespace NovaStriker.Combat
{
    public enum CombatFaction
    {
        Neutral,
        Player,
        Enemy
    }

    public struct DamagePacket
    {
        public float Damage;
        public Vector2 Knockback;
        public Vector2 HitPoint;
        public CombatFaction SourceFaction;
        public int SourcePlayerId;
        public int Tier;
        public string WeaponId;

        public DamagePacket(
            float damage,
            Vector2 knockback,
            Vector2 hitPoint,
            CombatFaction sourceFaction,
            int sourcePlayerId,
            int tier = 0,
            string weaponId = null)
        {
            Damage = damage;
            Knockback = knockback;
            HitPoint = hitPoint;
            SourceFaction = sourceFaction;
            SourcePlayerId = sourcePlayerId;
            Tier = tier;
            WeaponId = weaponId;
        }
    }
}
