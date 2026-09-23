using System;
using System.Collections.Generic;
using NovaStriker.Bosses;
using NovaStriker.Enemies;
using NovaStriker.Progression;
using UnityEngine;

namespace NovaStriker.Campaign
{
    [Serializable]
    public sealed class EnemyPrefabBinding
    {
        public EnemyArchetype Archetype;
        public GameObject Prefab;
    }

    [Serializable]
    public sealed class ActEncounterSlot
    {
        public EncounterController2D Encounter;
        public Transform[] SpawnPoints;
    }

    /// <summary>
    /// Converts the eighteen-act gameplay catalog into authored scene slots.
    /// Final level geometry only needs spawn/trigger placement and prefab
    /// bindings; encounter composition remains centralized and scalable.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CampaignActDirector2D : MonoBehaviour
    {
        [SerializeField] private CampaignProgressionController progression;

        [Header("Enemy Library")]
        [SerializeField] private EnemyPrefabBinding[] enemyPrefabs;

        [Header("Scene Slots")]
        [SerializeField] private ActEncounterSlot[] encounterSlots;
        [SerializeField] private SectorHazard2D[] hazards;
        [SerializeField] private SectorSetpieceController2D setpiece;
        [SerializeField] private SecretChallengeController2D secret;
        [SerializeField] private MiniBossController2D miniBoss;
        [SerializeField] private GuardianBossController2D guardianBoss;

        [Header("Runtime")]
        [SerializeField] private bool configureOnStart = true;
        [SerializeField] private bool prewarmCurrentAct = true;
        [SerializeField] private bool startSetpieceOnConfigure;

        private readonly List<EncounterWave> waveScratch = new(2);

        public bool Configured { get; private set; }
        public ActGameplayReference CurrentAct { get; private set; }

        public event Action<ActGameplayReference> ActConfigured;

        private void Awake()
        {
            ResolveProgression();
        }

        private void OnEnable()
        {
            ResolveProgression();

            if (progression)
                progression.ActChanged += OnActChanged;
        }

        private void OnDisable()
        {
            if (progression)
                progression.ActChanged -= OnActChanged;
        }

        private void Start()
        {
            if (configureOnStart)
                ConfigureCurrentAct();
        }

        public bool ConfigureCurrentAct()
        {
            ResolveProgression();

            if (!progression)
                return false;

            return ConfigureAct(
                progression.SectorIndex,
                progression.ActIndex
            );
        }

        public bool ConfigureAct(
            int sectorIndex,
            int actIndex)
        {
            CurrentAct =
                ActGameplayCatalog.Get(
                    sectorIndex,
                    actIndex
                );

            ConfigureEnvironment();
            ConfigureEncounterSlots();
            ConfigureBossEndpoint();

            Configured = true;
            ActConfigured?.Invoke(CurrentAct);
            return true;
        }

        private void ConfigureEnvironment()
        {
            if (hazards != null)
            {
                for (int i = 0; i < hazards.Length; i++)
                    hazards[i]?.ConfigureHazard(
                        CurrentAct.Hazard
                    );
            }

            if (setpiece)
            {
                setpiece.ConfigureSetpiece(
                    CurrentAct.Setpiece
                );

                if (
                    startSetpieceOnConfigure &&
                    CurrentAct.SetpieceIntensity > 0f
                )
                {
                    setpiece.StartSetpiece();
                }
            }

            if (secret)
            {
                secret.ConfigureIdentity(
                    CurrentAct.Key +
                    "-secret-" +
                    CurrentAct.SecretKind
                        .ToString()
                        .ToLowerInvariant(),
                    CurrentAct.SecretKind
                );
            }
        }

        private void ConfigureEncounterSlots()
        {
            if (encounterSlots == null)
                return;

            int slotIndex = 0;
            ActEncounterBeat[] beats =
                CurrentAct.Beats;

            if (beats != null)
            {
                for (int i = 0; i < beats.Length; i++)
                {
                    ActEncounterBeat beat = beats[i];

                    if (
                        beat.Groups == null ||
                        beat.Groups.Length == 0
                    )
                    {
                        continue;
                    }

                    if (slotIndex >= encounterSlots.Length)
                        break;

                    ActEncounterSlot slot =
                        encounterSlots[slotIndex++];

                    ConfigureEncounterSlot(
                        slot,
                        beat,
                        i
                    );
                }
            }

            for (
                int i = slotIndex;
                i < encounterSlots.Length;
                i++
            )
            {
                EncounterController2D encounter =
                    encounterSlots[i]?.Encounter;

                if (encounter)
                {
                    encounter.ResetForRetry();
                    encounter.enabled = false;
                }
            }
        }

        private void ConfigureEncounterSlot(
            ActEncounterSlot slot,
            ActEncounterBeat beat,
            int beatIndex)
        {
            if (slot == null || !slot.Encounter)
                return;

            slot.Encounter.ResetForRetry();
            slot.Encounter.enabled = true;

            EncounterWave wave =
                new()
                {
                    Id =
                        CurrentAct.Key +
                        "-" +
                        beat.Id,
                    StartDelay =
                        Mathf.Max(
                            0f,
                            beat.StartDelay
                        )
                };

            for (int i = 0; i < beat.Groups.Length; i++)
            {
                ActEnemyGroup group =
                    beat.Groups[i];

                GameObject prefab =
                    FindEnemyPrefab(
                        group.Archetype
                    );

                if (!prefab)
                    continue;

                Transform spawnPoint =
                    ResolveSpawnPoint(slot, i);

                wave.Spawns.Add(
                    new EncounterSpawnEntry
                    {
                        EnemyPrefab = prefab,
                        SpawnPoint = spawnPoint,
                        BaseCount =
                            Mathf.Max(
                                1,
                                group.BaseCount
                            ),
                        ExtraPerAdditionalPlayer =
                            Mathf.Max(
                                0,
                                group.ExtraPerAdditionalPlayer
                            ),
                        Interval =
                            SpawnIntervalFor(
                                beat.Kind
                            ),
                        SpawnSpread =
                            new Vector2(
                                0.85f,
                                beat.Kind ==
                                ActBeatKind.Setpiece
                                    ? 0.20f
                                    : 0f
                            )
                    }
                );

                if (prewarmCurrentAct)
                {
                    int maxCount =
                        group.BaseCount +
                        3 *
                        group.ExtraPerAdditionalPlayer;

                    EncounterEnemyPool2D.Prewarm(
                        prefab,
                        Mathf.Clamp(maxCount, 1, 8)
                    );
                }
            }

            waveScratch.Clear();
            waveScratch.Add(wave);

            slot.Encounter.ConfigureContent(
                CurrentAct.Key +
                "-encounter-" +
                beatIndex,
                new List<EncounterWave>(waveScratch),
                SynergyRewardFor(beat.Kind)
            );
        }

        private void ConfigureBossEndpoint()
        {
            if (miniBoss)
            {
                miniBoss.gameObject.SetActive(
                    CurrentAct.HasMiniBoss
                );

                if (CurrentAct.HasMiniBoss)
                {
                    miniBoss.ConfigureIdentity(
                        CurrentAct.MiniBoss
                    );
                }
            }

            if (guardianBoss)
            {
                guardianBoss.gameObject.SetActive(
                    CurrentAct.HasGuardian
                );

                if (CurrentAct.HasGuardian)
                {
                    guardianBoss.ConfigureIdentity(
                        CurrentAct.Guardian
                    );
                }
            }
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

        private Transform ResolveSpawnPoint(
            ActEncounterSlot slot,
            int index)
        {
            if (
                slot.SpawnPoints == null ||
                slot.SpawnPoints.Length == 0
            )
            {
                return transform;
            }

            return slot.SpawnPoints[
                index % slot.SpawnPoints.Length
            ];
        }

        private static float SpawnIntervalFor(
            ActBeatKind kind)
        {
            return kind switch
            {
                ActBeatKind.Pressure => 0.12f,
                ActBeatKind.Elite => 0.22f,
                ActBeatKind.Setpiece => 0.10f,
                _ => 0.18f
            };
        }

        private static float SynergyRewardFor(
            ActBeatKind kind)
        {
            return kind switch
            {
                ActBeatKind.Elite => 10f,
                ActBeatKind.Setpiece => 12f,
                ActBeatKind.Pressure => 8f,
                _ => 6f
            };
        }

        private void OnActChanged(
            int sectorIndex,
            int actIndex)
        {
            ConfigureAct(
                sectorIndex,
                actIndex
            );
        }

        private void ResolveProgression()
        {
            if (!progression)
            {
                progression =
                    FindFirstObjectByType<CampaignProgressionController>();
            }
        }
    }
}
