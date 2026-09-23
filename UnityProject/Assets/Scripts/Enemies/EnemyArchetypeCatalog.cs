namespace NovaStriker.Enemies
{
    public enum EnemyArchetype
    {
        Walker = 0,
        Turret = 1,
        Drone = 2,
        Shield = 3,
        Hopper = 4,
        Sniper = 5,
        Charger = 6,
        Orbiter = 7,
        Heavy = 8,
        WallHunter = 9,
        Guard = 10,
        Interceptor = 11
    }

    /// <summary>
    /// Browser-reference metadata for the twelve preserved enemy archetypes.
    /// Numeric movement/projectile values remain in browser-reference units and
    /// are not treated as Unity-world tuning values.
    /// </summary>
    public readonly struct EnemyArchetypeReference
    {
        public readonly EnemyArchetype Archetype;
        public readonly EnemyRole Role;
        public readonly float MaxHealth;
        public readonly float ShieldHp;
        public readonly float ArmorHp;
        public readonly float ReferenceMoveSpeed;
        public readonly float ReferenceProjectileSpeed;
        public readonly float ReferenceProjectileDamage;
        public readonly float FireInterval;
        public readonly float ContactDamage;
        public readonly bool PerfectOpportunityShots;
        public readonly bool DodgesChargedShots;
        public readonly bool ReflectsLowTierFrontShots;
        public readonly string MovementSummary;

        public EnemyArchetypeReference(
            EnemyArchetype archetype,
            EnemyRole role,
            float maxHealth,
            float shieldHp,
            float armorHp,
            float referenceMoveSpeed,
            float referenceProjectileSpeed,
            float referenceProjectileDamage,
            float fireInterval,
            float contactDamage,
            bool perfectOpportunityShots,
            bool dodgesChargedShots,
            bool reflectsLowTierFrontShots,
            string movementSummary)
        {
            Archetype = archetype;
            Role = role;
            MaxHealth = maxHealth;
            ShieldHp = shieldHp;
            ArmorHp = armorHp;
            ReferenceMoveSpeed = referenceMoveSpeed;
            ReferenceProjectileSpeed = referenceProjectileSpeed;
            ReferenceProjectileDamage = referenceProjectileDamage;
            FireInterval = fireInterval;
            ContactDamage = contactDamage;
            PerfectOpportunityShots = perfectOpportunityShots;
            DodgesChargedShots = dodgesChargedShots;
            ReflectsLowTierFrontShots = reflectsLowTierFrontShots;
            MovementSummary = movementSummary;
        }
    }

    /// <summary>
    /// Source-derived mapping from the preserved v0.10 browser reference.
    /// This catalog documents known behavior before Unity-specific tuning.
    /// </summary>
    public static class EnemyArchetypeCatalog
    {
        public static EnemyArchetypeReference Get(
            EnemyArchetype archetype)
        {
            return archetype switch
            {
                EnemyArchetype.Walker =>
                    Profile(
                        archetype,
                        EnemyRole.Skirmisher,
                        65f,
                        move: 55f,
                        movement:
                            "Direct horizontal pursuit toward the target."
                    ),

                EnemyArchetype.Turret =>
                    Profile(
                        archetype,
                        EnemyRole.Artillery,
                        65f,
                        fireInterval: 1.4f,
                        movement:
                            "No base locomotion in the preserved browser loop."
                    ),

                EnemyArchetype.Drone =>
                    Profile(
                        archetype,
                        EnemyRole.Aerial,
                        65f,
                        move: 28f,
                        movement:
                            "Vertical sine hover; advanced aerial role also tracks an offset above the target."
                    ),

                EnemyArchetype.Shield =>
                    Profile(
                        archetype,
                        EnemyRole.Anchor,
                        110f,
                        shield: 90f,
                        armor: 55f,
                        movement:
                            "Anchor role; braces against incoming frontal fire."
                    ),

                EnemyArchetype.Hopper =>
                    Profile(
                        archetype,
                        EnemyRole.Skirmisher,
                        65f,
                        dodgeCharged: true,
                        movement:
                            "Skirmisher role; preserved advanced reactions dodge high-tier incoming shots."
                    ),

                EnemyArchetype.Sniper =>
                    Profile(
                        archetype,
                        EnemyRole.Artillery,
                        75f,
                        projectileSpeed: 470f,
                        fireInterval: 2.05f,
                        perfectOpportunity: true,
                        movement:
                            "Artillery role with the fastest preserved standard-enemy projectile."
                    ),

                EnemyArchetype.Charger =>
                    Profile(
                        archetype,
                        EnemyRole.Flanker,
                        65f,
                        move: 150f,
                        dodgeCharged: true,
                        movement:
                            "Fast direct horizontal pursuit; advanced reactions dodge high-tier shots."
                    ),

                EnemyArchetype.Orbiter =>
                    Profile(
                        archetype,
                        EnemyRole.Aerial,
                        65f,
                        move: 38f,
                        movement:
                            "Circular X/Y orbit motion; advanced aerial role also tracks above/flank offset."
                    ),

                EnemyArchetype.Heavy =>
                    Profile(
                        archetype,
                        EnemyRole.Anchor,
                        150f,
                        armor: 90f,
                        move: 32f,
                        projectileDamage: 15f,
                        fireInterval: 2.65f,
                        contactDamage: 18f,
                        movement:
                            "Slow direct horizontal pursuit with the highest preserved standard-enemy health."
                    ),

                EnemyArchetype.WallHunter =>
                    Profile(
                        archetype,
                        EnemyRole.Flanker,
                        65f,
                        move: 88f,
                        movement:
                            "Vertical pursuit toward target Y plus flanker positioning."
                    ),

                EnemyArchetype.Guard =>
                    Profile(
                        archetype,
                        EnemyRole.Anchor,
                        105f,
                        shield: 40f,
                        armor: 65f,
                        reflectsLowTier: true,
                        movement:
                            "Anchor role; can reflect low-tier frontal shots and counter nearby melee."
                    ),

                EnemyArchetype.Interceptor =>
                    Profile(
                        archetype,
                        EnemyRole.Flanker,
                        65f,
                        move: 115f,
                        dodgeCharged: true,
                        movement:
                            "Predictive pursuit using target X plus target velocity lead; dodges high-tier shots."
                    ),

                _ => Profile(
                    EnemyArchetype.Walker,
                    EnemyRole.Skirmisher,
                    65f
                )
            };
        }

        private static EnemyArchetypeReference Profile(
            EnemyArchetype archetype,
            EnemyRole role,
            float health,
            float shield = 0f,
            float armor = 0f,
            float move = 0f,
            float projectileSpeed = 320f,
            float projectileDamage = 11f,
            float fireInterval = 2.2f,
            float contactDamage = 11f,
            bool perfectOpportunity = false,
            bool dodgeCharged = false,
            bool reflectsLowTier = false,
            string movement = "")
        {
            return new EnemyArchetypeReference(
                archetype,
                role,
                health,
                shield,
                armor,
                move,
                projectileSpeed,
                projectileDamage,
                fireInterval,
                contactDamage,
                perfectOpportunity,
                dodgeCharged,
                reflectsLowTier,
                movement
            );
        }
    }
}
