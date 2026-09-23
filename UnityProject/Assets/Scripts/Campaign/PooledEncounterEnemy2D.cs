using System.Collections.Generic;
using NovaStriker.Combat;
using NovaStriker.Enemies;
using UnityEngine;

namespace NovaStriker.Campaign
{
    /// <summary>
    /// Cached reset handles for a standard enemy managed by the encounter pool.
    /// The marker is added at runtime; production enemy prefabs do not need to
    /// reference pooling directly.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PooledEncounterEnemy2D : MonoBehaviour
    {
        private GameObject sourcePrefab;
        private Damageable2D[] damageables;
        private CombatState2D[] combatStates;
        private EnemyBrain2D[] brains;
        private EnemyArchetypeController2D[] archetypes;
        private Rigidbody2D[] bodies;

        public GameObject SourcePrefab => sourcePrefab;
        public bool IsInPool { get; private set; }

        internal void Initialize(GameObject source)
        {
            sourcePrefab = source;

            damageables =
                GetComponentsInChildren<Damageable2D>(true);
            combatStates =
                GetComponentsInChildren<CombatState2D>(true);
            brains =
                GetComponentsInChildren<EnemyBrain2D>(true);
            archetypes =
                GetComponentsInChildren<EnemyArchetypeController2D>(true);
            bodies =
                GetComponentsInChildren<Rigidbody2D>(true);

            for (int i = 0; i < damageables.Length; i++)
                damageables[i]?.SetDestroyOnDefeat(false);
        }

        internal void PrepareForSpawn(
            Vector3 position,
            Quaternion rotation)
        {
            transform.SetParent(null, false);
            transform.SetPositionAndRotation(
                position,
                rotation
            );

            for (int i = 0; i < bodies.Length; i++)
            {
                Rigidbody2D body = bodies[i];

                if (!body)
                    continue;

                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
            }

            for (int i = 0; i < brains.Length; i++)
                brains[i]?.PrepareForPoolSpawn();

            for (int i = 0; i < archetypes.Length; i++)
                archetypes[i]?.PrepareForPoolSpawn();

            for (int i = 0; i < damageables.Length; i++)
            {
                Damageable2D damageable = damageables[i];

                if (!damageable)
                    continue;

                damageable.SetDestroyOnDefeat(false);
                damageable.RestoreFullHealth();
            }

            for (int i = 0; i < combatStates.Length; i++)
                combatStates[i]?.RestoreLayers();

            IsInPool = false;
            gameObject.SetActive(true);
        }

        internal void PrepareForRelease()
        {
            if (IsInPool)
                return;

            for (int i = 0; i < bodies.Length; i++)
            {
                Rigidbody2D body = bodies[i];

                if (!body)
                    continue;

                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
            }

            IsInPool = true;
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Global pool for standard encounter enemies. Only spawned enemies carrying
    /// EnemyBrain2D are pooled; bosses and preplaced scene actors keep their
    /// authored lifetime.
    /// </summary>
    public static class EncounterEnemyPool2D
    {
        private sealed class Bucket
        {
            public Bucket(GameObject prefab)
            {
                Prefab = prefab;
            }

            public GameObject Prefab { get; }
            public readonly Stack<PooledEncounterEnemy2D> Inactive = new();
        }

        private static readonly Dictionary<EntityId, Bucket> Buckets = new();
        private static Transform poolRoot;

        public static int InactiveCount
        {
            get
            {
                int total = 0;

                foreach (Bucket bucket in Buckets.Values)
                    total += bucket.Inactive.Count;

                return total;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            Buckets.Clear();
            poolRoot = null;
        }

        public static bool CanPool(GameObject prefab)
        {
            return
                prefab &&
                prefab.GetComponentInChildren<EnemyBrain2D>(true);
        }

        public static GameObject Spawn(
            GameObject prefab,
            Vector3 position,
            Quaternion rotation)
        {
            if (!CanPool(prefab))
            {
                return prefab
                    ? Object.Instantiate(
                        prefab,
                        position,
                        rotation
                    )
                    : null;
            }

            Bucket bucket = GetBucket(prefab);
            PooledEncounterEnemy2D member = null;

            while (bucket.Inactive.Count > 0 && !member)
                member = bucket.Inactive.Pop();

            if (!member)
            {
                GameObject instance =
                    Object.Instantiate(
                        prefab,
                        position,
                        rotation
                    );

                member =
                    instance.GetComponent<PooledEncounterEnemy2D>();

                if (!member)
                    member = instance.AddComponent<PooledEncounterEnemy2D>();

                member.Initialize(prefab);
            }

            member.PrepareForSpawn(
                position,
                rotation
            );

            return member.gameObject;
        }

        public static bool TryRelease(
            Damageable2D enemy)
        {
            if (!enemy)
                return false;

            PooledEncounterEnemy2D member =
                enemy.GetComponentInParent<PooledEncounterEnemy2D>();

            if (!member || member.IsInPool)
                return false;

            GameObject source =
                member.SourcePrefab;

            if (!source)
            {
                Object.Destroy(member.gameObject);
                return true;
            }

            Bucket bucket = GetBucket(source);

            member.PrepareForRelease();
            member.transform.SetParent(
                GetPoolRoot(),
                false
            );

            bucket.Inactive.Push(member);
            return true;
        }

        public static void Prewarm(
            GameObject prefab,
            int count)
        {
            if (!CanPool(prefab) || count <= 0)
                return;

            Bucket bucket = GetBucket(prefab);
            count = Mathf.Max(0, count - bucket.Inactive.Count);

            for (int i = 0; i < count; i++)
            {
                GameObject instance =
                    Object.Instantiate(
                        prefab,
                        GetPoolRoot()
                    );

                PooledEncounterEnemy2D member =
                    instance.GetComponent<PooledEncounterEnemy2D>();

                if (!member)
                    member = instance.AddComponent<PooledEncounterEnemy2D>();

                member.Initialize(prefab);
                member.PrepareForRelease();
                bucket.Inactive.Push(member);
            }
        }

        private static Bucket GetBucket(GameObject prefab)
        {
            EntityId key = prefab.GetEntityId();

            if (!Buckets.TryGetValue(key, out Bucket bucket))
            {
                bucket = new Bucket(prefab);
                Buckets.Add(key, bucket);
            }

            return bucket;
        }

        private static Transform GetPoolRoot()
        {
            if (poolRoot)
                return poolRoot;

            GameObject root =
                new("__NovaEncounterEnemyPool");

            root.hideFlags = HideFlags.HideInHierarchy;
            Object.DontDestroyOnLoad(root);
            poolRoot = root.transform;
            return poolRoot;
        }
    }
}
