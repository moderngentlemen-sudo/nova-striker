using System;
using NovaStriker.Combat;
using NovaStriker.Core;
using NovaStriker.Player;
using NovaStriker.Session;
using UnityEngine;

namespace NovaStriker.Progression
{
    public readonly struct SkillPerkDefinition
    {
        public SkillPerkDefinition(
            string id,
            string displayName,
            int cost,
            string description)
        {
            Id = id;
            DisplayName = displayName;
            Cost = cost;
            Description = description;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public int Cost { get; }
        public string Description { get; }
    }

    public static class SkillPerkCatalog
    {
        public const string CapacitorLoop = "capacitor-loop";
        public const string CombatRecycler = "combat-recycler";
        public const string GuardianLink = "guardian-link";
        public const string ReviveBuffer = "revive-buffer";
        public const string SyncReservoir = "sync-reservoir";
        public const string StyleConverter = "style-converter";

        private static readonly SkillPerkDefinition[] Definitions =
        {
            new(
                CapacitorLoop,
                "Capacitor Loop",
                2,
                "Perfect Deflects restore Suit Energy and Ultimate charge."
            ),
            new(
                CombatRecycler,
                "Combat Recycler",
                3,
                "Enemy defeats recover a small amount of Suit Energy."
            ),
            new(
                GuardianLink,
                "Guardian Link",
                3,
                "Guardian ability cooldowns recover 20% faster."
            ),
            new(
                ReviveBuffer,
                "Revive Buffer",
                2,
                "Revived Strikers receive a longer invulnerability buffer."
            ),
            new(
                SyncReservoir,
                "Sync Reservoir",
                4,
                "Team Sync participants recover Suit Energy and Ultimate charge."
            ),
            new(
                StyleConverter,
                "Style Converter",
                4,
                "Reaching A rank or higher converts momentum into Suit resources."
            )
        };

        public static ReadOnlySpan<SkillPerkDefinition> All =>
            Definitions;

        public static bool TryGet(
            string id,
            out SkillPerkDefinition definition)
        {
            for (int i = 0; i < Definitions.Length; i++)
            {
                if (Definitions[i].Id == id)
                {
                    definition = Definitions[i];
                    return true;
                }
            }

            definition = default;
            return false;
        }
    }

    /// <summary>
    /// Profile-wide skill/perk authority. Unlock data is persisted through the
    /// existing NovaSaveData skill fields; runtime effects are applied through
    /// gameplay APIs rather than animation or presentation state.
    /// </summary>
    [DefaultExecutionOrder(-450)]
    [DisallowMultipleComponent]
    public sealed class SkillPerkService : MonoBehaviour
    {
        [SerializeField] private SaveGameService saveService;
        [SerializeField] private StrikeTeamSession session;

        private readonly int[] lastStyleRank =
            new int[StrikeTeamSession.MaxPlayers];

        private StrikeTeamSession boundSession;

        public static SkillPerkService Active { get; private set; }

        public event Action<string> SkillUnlocked;
        public event Action<int> SkillPointsChanged;

        public int SkillPoints =>
            saveService && saveService.Current != null
                ? saveService.Current.skillPoints
                : 0;

        private void Awake()
        {
            Active = this;

            if (!saveService)
                saveService = SaveGameService.Active;

            ResolveSession();
        }

        private void OnEnable()
        {
            GameplayEventHub.CueRaised += OnGameplayCue;
            ResolveSession();
            BindSession();
            RefreshPassiveEffects();
        }

        private void OnDisable()
        {
            GameplayEventHub.CueRaised -= OnGameplayCue;
            UnbindSession();
        }

        private void OnDestroy()
        {
            if (Active == this)
                Active = null;
        }

        private void Update()
        {
            if (!saveService)
                saveService = SaveGameService.Active;

            if (!session)
            {
                ResolveSession();
                BindSession();
                RefreshPassiveEffects();
            }
        }

        public bool IsUnlocked(string id)
        {
            return
                !string.IsNullOrEmpty(id) &&
                saveService &&
                saveService.Current != null &&
                saveService.Current.unlockedSkills != null &&
                saveService.Current.unlockedSkills.Contains(id);
        }

        public bool TryUnlock(string id)
        {
            if (
                !saveService ||
                saveService.Current == null ||
                !SkillPerkCatalog.TryGet(
                    id,
                    out SkillPerkDefinition definition
                ) ||
                IsUnlocked(id) ||
                saveService.Current.skillPoints < definition.Cost
            )
            {
                return false;
            }

            saveService.Current.skillPoints -=
                definition.Cost;

            saveService.Current.unlockedSkills.Add(id);
            saveService.Save();

            RefreshPassiveEffects();

            SkillUnlocked?.Invoke(id);
            SkillPointsChanged?.Invoke(
                saveService.Current.skillPoints
            );

            return true;
        }

        public void AddSkillPoints(int amount)
        {
            if (
                amount <= 0 ||
                !saveService ||
                saveService.Current == null
            )
            {
                return;
            }

            saveService.Current.skillPoints += amount;
            saveService.Save();

            SkillPointsChanged?.Invoke(
                saveService.Current.skillPoints
            );
        }

        public void RefreshPassiveEffects()
        {
            if (!session)
                ResolveSession();

            if (!session)
                return;

            float guardianRate =
                IsUnlocked(SkillPerkCatalog.GuardianLink)
                    ? 1.20f
                    : 1f;

            for (
                int slot = 0;
                slot < StrikeTeamSession.MaxPlayers;
                slot++
            )
            {
                StrikerPlayerIdentity player =
                    session.GetPlayer(slot);

                if (!player)
                    continue;

                player
                    .GetComponent<GuardianAbilityController>()
                    ?.SetCooldownRecoveryMultiplier(
                        guardianRate
                    );
            }
        }

        private void OnGameplayCue(GameplayCue cue)
        {
            if (!session)
                ResolveSession();

            StrikerPlayerIdentity player =
                session
                    ? session.GetPlayerByActorId(cue.ActorId)
                    : null;

            if (!player)
                return;

            StrikeSuitAbilityController suit =
                player.GetComponent<StrikeSuitAbilityController>();

            switch (cue.Type)
            {
                case GameplayCueType.PerfectParry:
                    if (
                        IsUnlocked(
                            SkillPerkCatalog.CapacitorLoop
                        )
                    )
                    {
                        suit?.RestoreSuitEnergy(12f);
                        suit?.AddUltimateCharge(4f);
                    }
                    break;

                case GameplayCueType.DefeatDealt:
                    if (
                        IsUnlocked(
                            SkillPerkCatalog.CombatRecycler
                        )
                    )
                    {
                        suit?.RestoreSuitEnergy(5f);
                    }
                    break;

                case GameplayCueType.Revived:
                    if (
                        IsUnlocked(
                            SkillPerkCatalog.ReviveBuffer
                        )
                    )
                    {
                        player
                            .GetComponent<Damageable2D>()
                            ?.GrantInvulnerability(2f);
                    }
                    break;

                case GameplayCueType.StyleRankChanged:
                    ApplyStyleConversion(
                        player,
                        suit,
                        cue.Tier
                    );
                    break;
            }
        }

        private void ApplyStyleConversion(
            StrikerPlayerIdentity player,
            StrikeSuitAbilityController suit,
            int rank)
        {
            int slot = player.PlayerSlot;

            if (
                slot < 0 ||
                slot >= lastStyleRank.Length
            )
            {
                return;
            }

            int previous = lastStyleRank[slot];
            lastStyleRank[slot] = rank;

            if (
                rank <= previous ||
                rank < (int)StyleRank.A ||
                !IsUnlocked(
                    SkillPerkCatalog.StyleConverter
                )
            )
            {
                return;
            }

            suit?.RestoreSuitEnergy(
                4f + (rank - (int)StyleRank.A) * 2f
            );

            suit?.AddUltimateCharge(
                2f + (rank - (int)StyleRank.A)
            );
        }

        private void OnTeamSyncActivated(
            TeamSyncTier tier,
            int participants)
        {
            if (
                !session ||
                !IsUnlocked(
                    SkillPerkCatalog.SyncReservoir
                )
            )
            {
                return;
            }

            int mask =
                session.LastSyncParticipantMask;

            for (
                int slot = 0;
                slot < StrikeTeamSession.MaxPlayers;
                slot++
            )
            {
                if ((mask & (1 << slot)) == 0)
                    continue;

                StrikeSuitAbilityController suit =
                    session
                        .GetPlayer(slot)
                        ?.GetComponent<StrikeSuitAbilityController>();

                suit?.RestoreSuitEnergy(8f);
                suit?.AddUltimateCharge(2f);
            }
        }

        private void OnPlayerRegistered(
            int slot,
            StrikerPlayerIdentity player)
        {
            RefreshPassiveEffects();
        }

        private void ResolveSession()
        {
            if (!session)
                session = StrikeTeamSession.Active;
        }

        private void BindSession()
        {
            if (!session || boundSession == session)
                return;

            UnbindSession();
            boundSession = session;
            boundSession.TeamSyncActivated +=
                OnTeamSyncActivated;
            boundSession.PlayerRegistered +=
                OnPlayerRegistered;
        }

        private void UnbindSession()
        {
            if (!boundSession)
                return;

            boundSession.TeamSyncActivated -=
                OnTeamSyncActivated;
            boundSession.PlayerRegistered -=
                OnPlayerRegistered;
            boundSession = null;
        }
    }
}
