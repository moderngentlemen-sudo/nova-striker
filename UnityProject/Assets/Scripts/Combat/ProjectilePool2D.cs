using System.Collections.Generic;
using UnityEngine;

namespace NovaStriker.Combat
{
    /// <summary>
    /// Lightweight runtime pool for Projectile2D instances. Gameplay callers
    /// keep configuring projectiles through Projectile2D.Initialize; pooling
    /// only replaces Instantiate/Destroy churn and never owns combat timing.
    /// </summary>
    public static class ProjectilePool2D
    {
        private sealed class Bucket
        {
            public Bucket(Projectile2D prefab)
            {
                Prefab = prefab;
            }

            public Projectile2D Prefab { get; }
            public readonly Stack<Projectile2D> Inactive = new();
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

        public static Projectile2D Spawn(
            Projectile2D prefab,
            Vector3 position,
            Quaternion rotation)
        {
            if (!prefab)
                return null;

            Bucket bucket = GetBucket(prefab);
            Projectile2D instance = null;

            while (bucket.Inactive.Count > 0 && !instance)
                instance = bucket.Inactive.Pop();

            if (!instance)
            {
                instance = Object.Instantiate(
                    prefab,
                    position,
                    rotation
                );

                instance.MarkPooledSource(prefab);
            }

            instance.PrepareForPoolSpawn(
                prefab,
                position,
                rotation
            );

            return instance;
        }

        public static void Release(Projectile2D projectile)
        {
            if (!projectile)
                return;

            Projectile2D source =
                projectile.PoolSourcePrefab;

            if (!source)
            {
                Object.Destroy(projectile.gameObject);
                return;
            }

            Bucket bucket = GetBucket(source);

            projectile.PrepareForPoolRelease();
            projectile.transform.SetParent(
                GetPoolRoot(),
                false
            );

            bucket.Inactive.Push(projectile);
        }

        public static void Prewarm(
            Projectile2D prefab,
            int count)
        {
            if (!prefab || count <= 0)
                return;

            Bucket bucket = GetBucket(prefab);
            count = Mathf.Max(0, count - bucket.Inactive.Count);

            for (int i = 0; i < count; i++)
            {
                Projectile2D instance =
                    Object.Instantiate(
                        prefab,
                        GetPoolRoot()
                    );

                instance.MarkPooledSource(prefab);
                instance.PrepareForPoolRelease();
                bucket.Inactive.Push(instance);
            }
        }

        private static Bucket GetBucket(
            Projectile2D prefab)
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
                new("__NovaProjectilePool");

            root.hideFlags = HideFlags.HideInHierarchy;
            Object.DontDestroyOnLoad(root);
            poolRoot = root.transform;
            return poolRoot;
        }
    }
}
