using System;
using System.Collections;
using System.Collections.Generic;
using NovaStriker.CameraSystem;
using NovaStriker.Combat;
using NovaStriker.Core;
using NovaStriker.Enemies;
using NovaStriker.Player;
using NovaStriker.Session;
using UnityEngine;

namespace NovaStriker.Campaign
{
    [Serializable]
    public sealed class EncounterSpawnEntry
    {
        public GameObject EnemyPrefab;
        public Transform SpawnPoint;
        [Min(1)] public int BaseCount = 1;
        [Min(0)] public int ExtraPerAdditionalPlayer = 1;
        [Min(0f)] public float Interval = 0.18f;
        public Vector2 SpawnSpread = new(0.7f, 0f);
    }

    [Serializable]
    public sealed class EncounterWave
    {
        public string Id = "wave";
        [Min(0f)] public float StartDelay = 0.45f;
        public List<EncounterSpawnEntry> Spawns = new();
    }

    /// <summary>
    /// Data-driven one-to-four-player encounter authority. Supports preplaced
    /// enemies and scalable spawned waves, camera arena locking, completion
    /// events, and deterministic cleanup without depending on final art.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EncounterController2D : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string encounterId = "encounter";

        [Header("Activation")]
        [SerializeField] private bool autoStartOnPlayerContact = true;
        [SerializeField] private Collider2D activationTrigger;

        [Header("Content")]
        [SerializeField] private Damageable2D[] preplacedEnemies;
        [SerializeField] private List<EncounterWave> waves = new();
        [SerializeField] private int maxScaledCountPerSpawnEntry = 8;
        [SerializeField] private float betweenWaveDelay = 0.75f;
        [SerializeField] private bool useSpawnPooling = true;

        [Header("Arena")]
        [SerializeField] private bool lockCamera;
        [SerializeField] private Rect cameraArenaBounds =
            new(-8f, -4f, 16f, 8f);
        [SerializeField] private StrikeTeamCamera2D teamCamera;

        [Header("Rewards")]
        [SerializeField] private float completionSynergy = 8f;

        private readonly HashSet<Damageable2D> activeEnemies = new();
        private readonly List<Damageable2D> defeatedScratch = new(16);

        private StrikeTeamSession session;
        private Coroutine sequence;
        private int currentWaveIndex = -1;
        private bool waveSpawnFinished;

        public bool Started { get; private set; }
        public bool Completed { get; private set; }
        public int ActiveEnemyCount => activeEnemies.Count;
        public int CurrentWaveIndex => currentWaveIndex;

        public event Action EncounterStarted;
        public event Action<int> WaveStarted;
        public event Action EncounterCompleted;

        public void ConfigureContent(
            string id,
            List<EncounterWave> configuredWaves,
            float synergyReward = -1f)
        {
            if (Started)
                return;

            if (!string.IsNullOrEmpty(id))
                encounterId = id;

            waves =
                configuredWaves ??
                new List<EncounterWave>();

            if (synergyReward >= 0f)
                completionSynergy = synergyReward;

            Completed = false;
            currentWaveIndex = -1;
            waveSpawnFinished = false;
        }

        private void Reset()
        {
            activationTrigger = GetComponent<Collider2D>();

            if (activationTrigger)
                activationTrigger.isTrigger = true;
        }

        private void Awake()
        {
            session = StrikeTeamSession.Active;

            if (!teamCamera)
                teamCamera = FindFirstObjectByType<StrikeTeamCamera2D>();
        }

        private void OnDisable()
        {
            if (sequence != null)
            {
                StopCoroutine(sequence);
                sequence = null;
            }

            UnsubscribeAll();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (
                !autoStartOnPlayerContact ||
                Started ||
                Completed
            )
            {
                return;
            }

            StrikerPlayerIdentity player =
                other.GetComponentInParent<StrikerPlayerIdentity>();

            if (player && player.IsCombatReady)
                StartEncounter();
        }

        public void StartEncounter()
        {
            if (Started || Completed)
                return;

            Started = true;

            if (!session)
                session = StrikeTeamSession.Active;

            RegisterPreplacedEnemies();

            if (lockCamera)
            {
                if (!teamCamera)
                    teamCamera = FindFirstObjectByType<StrikeTeamCamera2D>();

                teamCamera?.SetArenaBounds(cameraArenaBounds);
            }

            EncounterStarted?.Invoke();

            GameplayEventHub.Raise(
                new GameplayCue(
                    GameplayCueType.EncounterStarted,
                    -1,
                    transform.position,
                    Vector2.zero,
                    0,
                    waves.Count,
                    encounterId
                )
            );

            sequence = StartCoroutine(RunEncounter());
        }

        public void ResetForRetry()
        {
            if (sequence != null)
            {
                StopCoroutine(sequence);
                sequence = null;
            }

            defeatedScratch.Clear();

            foreach (Damageable2D enemy in activeEnemies)
            {
                if (enemy)
                    defeatedScratch.Add(enemy);
            }

            for (int i = 0; i < defeatedScratch.Count; i++)
            {
                Damageable2D enemy = defeatedScratch[i];

                if (!enemy)
                    continue;

                enemy.Defeated -= OnEnemyDefeated;

                if (IsPreplaced(enemy))
                {
                    ResetEnemyRuntime(enemy);
                }
                else if (
                    !useSpawnPooling ||
                    !EncounterEnemyPool2D.TryRelease(enemy)
                )
                {
                    Destroy(enemy.transform.root.gameObject);
                }
            }

            activeEnemies.Clear();
            defeatedScratch.Clear();

            if (preplacedEnemies != null)
            {
                for (int i = 0; i < preplacedEnemies.Length; i++)
                    ResetEnemyRuntime(preplacedEnemies[i]);
            }

            Started = false;
            Completed = false;
            currentWaveIndex = -1;
            waveSpawnFinished = false;

            if (lockCamera)
                teamCamera?.ClearArenaBounds();
        }

        public void ForceComplete()
        {
            if (Completed)
                return;

            if (sequence != null)
            {
                StopCoroutine(sequence);
                sequence = null;
            }

            CompleteEncounter();
        }

        private IEnumerator RunEncounter()
        {
            for (int i = 0; i < waves.Count; i++)
            {
                currentWaveIndex = i;
                EncounterWave wave = waves[i];

                if (wave != null && wave.StartDelay > 0f)
                    yield return new WaitForSeconds(wave.StartDelay);

                waveSpawnFinished = false;

                WaveStarted?.Invoke(i);

                GameplayEventHub.Raise(
                    new GameplayCue(
                        GameplayCueType.EncounterWaveStarted,
                        -1,
                        transform.position,
                        Vector2.zero,
                        i,
                        activeEnemies.Count,
                        wave != null && !string.IsNullOrEmpty(wave.Id)
                            ? wave.Id
                            : encounterId + "-wave-" + i
                    )
                );

                if (wave != null)
                    yield return SpawnWave(wave);

                waveSpawnFinished = true;

                while (activeEnemies.Count > 0)
                    yield return null;

                if (
                    i < waves.Count - 1 &&
                    betweenWaveDelay > 0f
                )
                {
                    yield return new WaitForSeconds(
                        betweenWaveDelay
                    );
                }
            }

            waveSpawnFinished = true;

            while (activeEnemies.Count > 0)
                yield return null;

            CompleteEncounter();
        }

        private IEnumerator SpawnWave(EncounterWave wave)
        {
            if (wave.Spawns == null)
                yield break;

            for (int i = 0; i < wave.Spawns.Count; i++)
            {
                EncounterSpawnEntry entry =
                    wave.Spawns[i];

                if (
                    entry == null ||
                    !entry.EnemyPrefab
                )
                {
                    continue;
                }

                int activePlayers =
                    session
                        ? Mathf.Max(1, session.ActivePlayerCount)
                        : 1;

                int count =
                    entry.BaseCount +
                    Mathf.Max(0, activePlayers - 1) *
                    entry.ExtraPerAdditionalPlayer;

                count =
                    Mathf.Clamp(
                        count,
                        1,
                        Mathf.Max(1, maxScaledCountPerSpawnEntry)
                    );

                Vector3 center =
                    entry.SpawnPoint
                        ? entry.SpawnPoint.position
                        : transform.position;

                for (int n = 0; n < count; n++)
                {
                    float centered =
                        count <= 1
                            ? 0f
                            : n - (count - 1) * 0.5f;

                    Vector3 position =
                        center +
                        new Vector3(
                            entry.SpawnSpread.x * centered,
                            entry.SpawnSpread.y * centered,
                            0f
                        );

                    GameObject instance =
                        useSpawnPooling
                            ? EncounterEnemyPool2D.Spawn(
                                entry.EnemyPrefab,
                                position,
                                Quaternion.identity
                            )
                            : Instantiate(
                                entry.EnemyPrefab,
                                position,
                                Quaternion.identity
                            );

                    RegisterEnemy(
                        instance
                            ? instance.GetComponentInChildren<Damageable2D>()
                            : null
                    );

                    if (
                        entry.Interval > 0f &&
                        n < count - 1
                    )
                    {
                        yield return new WaitForSeconds(
                            entry.Interval
                        );
                    }
                }
            }
        }

        private void RegisterPreplacedEnemies()
        {
            if (preplacedEnemies == null)
                return;

            for (int i = 0; i < preplacedEnemies.Length; i++)
                RegisterEnemy(preplacedEnemies[i]);
        }

        public void RegisterEnemy(Damageable2D enemy)
        {
            if (
                !enemy ||
                enemy.Faction != CombatFaction.Enemy ||
                enemy.IsDefeated ||
                !activeEnemies.Add(enemy)
            )
            {
                return;
            }

            enemy.Defeated += OnEnemyDefeated;
        }

        private void OnEnemyDefeated(DamagePacket packet)
        {
            RemoveDefeatedEntries();

            if (
                Started &&
                waveSpawnFinished &&
                activeEnemies.Count == 0 &&
                currentWaveIndex >= waves.Count - 1
            )
            {
                // Coroutine also observes the count. This direct check makes
                // no-wave / preplaced-only encounters complete immediately.
                if (waves.Count == 0)
                    CompleteEncounter();
            }
        }

        private void RemoveDefeatedEntries()
        {
            defeatedScratch.Clear();

            foreach (Damageable2D enemy in activeEnemies)
            {
                if (enemy && !enemy.IsDefeated)
                    continue;

                defeatedScratch.Add(enemy);
            }

            for (int i = 0; i < defeatedScratch.Count; i++)
            {
                Damageable2D enemy =
                    defeatedScratch[i];

                if (enemy)
                    enemy.Defeated -= OnEnemyDefeated;

                activeEnemies.Remove(enemy);

                if (useSpawnPooling && enemy)
                    EncounterEnemyPool2D.TryRelease(enemy);
            }

            defeatedScratch.Clear();
        }

        private void CompleteEncounter()
        {
            if (Completed)
                return;

            Started = false;
            Completed = true;
            sequence = null;

            UnsubscribeAll();

            if (lockCamera)
                teamCamera?.ClearArenaBounds();

            if (completionSynergy > 0f)
                session?.AddSynergy(completionSynergy);

            EncounterCompleted?.Invoke();

            GameplayEventHub.Raise(
                new GameplayCue(
                    GameplayCueType.EncounterCompleted,
                    -1,
                    transform.position,
                    Vector2.up,
                    currentWaveIndex + 1,
                    completionSynergy,
                    encounterId
                )
            );
        }

        private bool IsPreplaced(Damageable2D enemy)
        {
            if (!enemy || preplacedEnemies == null)
                return false;

            for (int i = 0; i < preplacedEnemies.Length; i++)
            {
                if (preplacedEnemies[i] == enemy)
                    return true;
            }

            return false;
        }

        private static void ResetEnemyRuntime(
            Damageable2D enemy)
        {
            if (!enemy)
                return;

            enemy.RestoreFullHealth();
            enemy.GetComponent<CombatState2D>()?.RestoreLayers();
            enemy.GetComponent<EnemyBrain2D>()?.PrepareForPoolSpawn();
            enemy.GetComponent<EnemyArchetypeController2D>()?.PrepareForPoolSpawn();

            if (!enemy.gameObject.activeSelf)
                enemy.gameObject.SetActive(true);
        }

        private void UnsubscribeAll()
        {
            foreach (Damageable2D enemy in activeEnemies)
            {
                if (enemy)
                    enemy.Defeated -= OnEnemyDefeated;
            }

            activeEnemies.Clear();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!lockCamera)
                return;

            Gizmos.color = Color.yellow;

            Vector3 center = new(
                cameraArenaBounds.center.x,
                cameraArenaBounds.center.y,
                0f
            );

            Vector3 size = new(
                cameraArenaBounds.size.x,
                cameraArenaBounds.size.y,
                0f
            );

            Gizmos.DrawWireCube(center, size);
        }
#endif
    }
}
