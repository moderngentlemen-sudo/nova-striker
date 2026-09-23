using System;
using NovaStriker.Core;
using NovaStriker.Player;
using NovaStriker.Progression;
using NovaStriker.Session;
using UnityEngine;

namespace NovaStriker.Progression
{
    /// <summary>
    /// Shared profile weapon-mastery progression for all four local players.
    /// Damage/defeats award XP to the weapon currently equipped by the actor.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponMasteryService : MonoBehaviour
    {
        [SerializeField] private SaveGameService saveService;
        [SerializeField] private StrikeTeamSession session;
        [SerializeField] private int maxMasteryLevel = 10;
        [SerializeField] private float baseExperiencePerLevel = 240f;
        [SerializeField] private float autosaveInterval = 8f;

        private float saveTimer;
        private bool dirty;

        public event Action<string, int> MasteryLevelChanged;

        private void Awake()
        {
            if (!saveService)
                saveService = SaveGameService.Active;

            if (!session)
                session = StrikeTeamSession.Active;
        }

        private void OnEnable()
        {
            GameplayEventHub.CueRaised += OnGameplayCue;
        }

        private void OnDisable()
        {
            GameplayEventHub.CueRaised -= OnGameplayCue;
            Flush();
        }

        private void Update()
        {
            if (!dirty)
                return;

            saveTimer += Time.unscaledDeltaTime;

            if (saveTimer >= autosaveInterval)
                Flush();
        }

        public WeaponMasterySave Get(string weaponId)
        {
            if (
                string.IsNullOrEmpty(weaponId) ||
                !saveService ||
                saveService.Current == null
            )
            {
                return null;
            }

            for (
                int i = 0;
                i < saveService.Current.weaponMastery.Count;
                i++
            )
            {
                WeaponMasterySave mastery =
                    saveService.Current.weaponMastery[i];

                if (mastery != null && mastery.weaponId == weaponId)
                    return mastery;
            }

            WeaponMasterySave created = new()
            {
                weaponId = weaponId,
                experience = 0f,
                level = 0
            };

            saveService.Current.weaponMastery.Add(created);
            dirty = true;
            return created;
        }

        public void AddExperience(
            string weaponId,
            float amount)
        {
            if (
                string.IsNullOrEmpty(weaponId) ||
                amount <= 0f
            )
            {
                return;
            }

            WeaponMasterySave mastery = Get(weaponId);

            if (mastery == null)
                return;

            mastery.experience += amount;
            int previous = mastery.level;

            while (
                mastery.level < maxMasteryLevel &&
                mastery.experience >=
                ExperienceRequiredForLevel(
                    mastery.level + 1
                )
            )
            {
                mastery.level++;
            }

            dirty = true;

            if (mastery.level != previous)
            {
                MasteryLevelChanged?.Invoke(
                    weaponId,
                    mastery.level
                );

                Flush();
            }
        }

        public float ExperienceRequiredForLevel(
            int level)
        {
            if (level <= 0)
                return 0f;

            return
                baseExperiencePerLevel *
                Mathf.Pow(level, 1.35f);
        }

        public void Flush()
        {
            if (!dirty)
                return;

            saveTimer = 0f;

            if (saveService && saveService.Save())
                dirty = false;
        }

        private void OnGameplayCue(GameplayCue cue)
        {
            if (
                cue.Type != GameplayCueType.DamageDealt &&
                cue.Type != GameplayCueType.DefeatDealt
            )
            {
                return;
            }

            if (!session)
                session = StrikeTeamSession.Active;

            StrikerPlayerIdentity player =
                session
                    ? session.GetPlayerByActorId(cue.ActorId)
                    : null;

            if (!player)
                return;

            StrikerLoadoutController loadout =
                player.GetComponent<StrikerLoadoutController>();

            string weaponId =
                loadout && loadout.CurrentWeapon
                    ? loadout.CurrentWeapon.Id
                    : cue.Id;

            if (string.IsNullOrEmpty(weaponId))
                return;

            NovaStriker.Combat.PlayerStyleMeter style =
                player.GetComponent<NovaStriker.Combat.PlayerStyleMeter>();

            float multiplier =
                style ? style.MasteryMultiplier : 1f;

            float xp =
                cue.Type == GameplayCueType.DefeatDealt
                    ? 20f
                    : Mathf.Max(1f, cue.Value * 0.55f);

            AddExperience(
                weaponId,
                xp * multiplier
            );
        }
    }
}
