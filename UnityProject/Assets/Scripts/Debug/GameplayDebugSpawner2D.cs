using System;
using NovaStriker.Bosses;
using NovaStriker.Campaign;
using NovaStriker.Enemies;
using NovaStriker.Session;
using UnityEngine;

namespace NovaStriker.Debugging
{
    /// <summary>
    /// Runtime debug spawning API for mechanics validation. It deliberately
    /// consumes the same pooled enemy path and boss controllers as campaign
    /// gameplay so test spawns exercise production code.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameplayDebugSpawner2D : MonoBehaviour
    {
        [SerializeField] private EnemyPrefabBinding[] enemyPrefabs;
        [SerializeField] private MiniBossController2D miniBossPrefab;
        [SerializeField] private GuardianBossController2D guardianPrefab;
        [SerializeField] private Transform spawnOrigin;
        [SerializeField] private float rosterSpacing = 2.4f;

        public event Action<GameObject> Spawned;

        public GameObject SpawnEnemy(
            EnemyArchetype archetype,
            Vector3 position)
        {
            GameObject prefab =
                FindEnemyPrefab(archetype);

            if (!prefab)
                return null;

            GameObject instance =
                EncounterEnemyPool2D.Spawn(
                    prefab,
                    position,
                    Quaternion.identity
                );

            EnemyArchetypeController2D controller =
                instance
                    ? instance.GetComponent<EnemyArchetypeController2D>()
                    : null;

            controller?.ConfigureArchetype(archetype);
            Spawned?.Invoke(instance);
            return instance;
        }

        public MiniBossController2D SpawnMiniBoss(
            MiniBossId id,
            Vector3 position)
        {
            if (!miniBossPrefab)
                return null;

            MiniBossController2D instance =
                Instantiate(
                    miniBossPrefab,
                    position,
                    Quaternion.identity
                );

            instance.ConfigureIdentity(id);
            Spawned?.Invoke(instance.gameObject);
            return instance;
        }

        public GuardianBossController2D SpawnGuardian(
            NovaStriker.Data.GuardianId id,
            Vector3 position)
        {
            if (!guardianPrefab)
                return null;

            GuardianBossController2D instance =
                Instantiate(
                    guardianPrefab,
                    position,
                    Quaternion.identity
                );

            instance.ConfigureIdentity(id);
            Spawned?.Invoke(instance.gameObject);
            return instance;
        }

        public void SpawnFullEnemyRoster()
        {
            Vector3 origin =
                spawnOrigin
                    ? spawnOrigin.position
                    : transform.position;

            int count =
                Enum.GetValues(
                    typeof(EnemyArchetype)
                ).Length;

            float half =
                (count - 1) * 0.5f;

            for (int i = 0; i < count; i++)
            {
                Vector3 position =
                    origin +
                    Vector3.right *
                    ((i - half) * rosterSpacing);

                SpawnEnemy(
                    (EnemyArchetype)i,
                    position
                );
            }
        }

        public void RestoreStrikeTeam()
        {
            StrikeTeamSession session =
                StrikeTeamSession.Active;

            if (!session)
                return;

            for (
                int i = 0;
                i < StrikeTeamSession.MaxPlayers;
                i++
            )
            {
                NovaStriker.Player.StrikerPlayerIdentity player =
                    session.GetPlayer(i);

                if (
                    !player ||
                    !player.IsParticipating
                )
                {
                    continue;
                }

                NovaStriker.Combat.Damageable2D health =
                    player.GetComponent<NovaStriker.Combat.Damageable2D>();

                if (!health)
                    continue;

                if (health.IsDefeated)
                    health.Revive(1f, 1f);
                else
                    health.RestoreFullHealth();

                NovaStriker.Player.StrikeSuitAbilityController suit =
                    player.GetComponent<NovaStriker.Player.StrikeSuitAbilityController>();

                suit?.RestoreSuitEnergy(
                    suit.MaxSuitEnergy
                );

                suit?.AddUltimateCharge(
                    suit.UltimateChargeRequired
                );
            }

            session.SetSynergy(session.MaxSynergy);
        }

        private GameObject FindEnemyPrefab(
            EnemyArchetype archetype)
        {
            if (enemyPrefabs == null)
                return null;

            for (int i = 0; i < enemyPrefabs.Length; i++)
            {
                EnemyPrefabBinding binding =
                    enemyPrefabs[i];

                if (
                    binding != null &&
                    binding.Archetype == archetype
                )
                {
                    return binding.Prefab;
                }
            }

            return null;
        }
    }
}
