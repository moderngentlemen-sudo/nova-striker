using System;
using NovaStriker.Core;
using NovaStriker.Player;
using NovaStriker.Progression;
using UnityEngine;

namespace NovaStriker.Campaign
{
    public enum SecretChallengeKind
    {
        Gauntlet = 0,
        WeaponTrial = 1,
        DashCourse = 2
    }

    /// <summary>
    /// Secret/challenge-room state independent from final environment art.
    /// Combat challenges can delegate to EncounterController2D; traversal
    /// courses complete through an explicit finish trigger.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SecretChallengeController2D : MonoBehaviour
    {
        [SerializeField] private string secretId = "secret";
        [SerializeField] private SecretChallengeKind kind;
        [SerializeField] private EncounterController2D combatEncounter;
        [SerializeField] private float dashCourseTimeLimit = 8f;
        [SerializeField] private string weaponRewardId;
        [SerializeField] private int skillPointReward = 1;
        [SerializeField] private float synergyReward = 12f;

        private float timer;

        public bool Opened { get; private set; }
        public bool Cleared { get; private set; }
        public float TimeRemaining => timer;

        public event Action OpenedEvent;
        public event Action ClearedEvent;

        public SecretChallengeKind Kind => kind;
        public string SecretId => secretId;

        public void ConfigureIdentity(
            string id,
            SecretChallengeKind challengeKind)
        {
            if (!string.IsNullOrEmpty(id))
                secretId = id;

            kind = challengeKind;
            Opened = false;
            Cleared = false;
            timer = 0f;
        }

        private void OnEnable()
        {
            if (combatEncounter)
                combatEncounter.EncounterCompleted += OnEncounterCompleted;
        }

        private void OnDisable()
        {
            if (combatEncounter)
                combatEncounter.EncounterCompleted -= OnEncounterCompleted;
        }

        private void Update()
        {
            if (
                !Opened ||
                Cleared ||
                kind != SecretChallengeKind.DashCourse
            )
            {
                return;
            }

            timer =
                Mathf.Max(
                    0f,
                    timer - Time.deltaTime
                );

            if (timer <= 0f)
            {
                // Course remains open; touching the start again restarts the
                // timer instead of permanently failing the secret.
                Opened = false;
            }
        }

        public void Open(StrikerPlayerIdentity opener)
        {
            if (Cleared)
                return;

            Opened = true;

            if (kind == SecretChallengeKind.DashCourse)
                timer = Mathf.Max(0.1f, dashCourseTimeLimit);

            if (
                combatEncounter &&
                (
                    kind == SecretChallengeKind.Gauntlet ||
                    kind == SecretChallengeKind.WeaponTrial
                )
            )
            {
                combatEncounter.StartEncounter();
            }

            OpenedEvent?.Invoke();

            GameplayEventHub.Raise(
                new GameplayCue(
                    GameplayCueType.SecretOpened,
                    opener ? opener.ActorId : -1,
                    transform.position,
                    Vector2.up,
                    (int)kind,
                    timer,
                    secretId
                )
            );
        }

        public void ReachDashCourseFinish(
            StrikerPlayerIdentity player)
        {
            if (
                !Opened ||
                Cleared ||
                kind != SecretChallengeKind.DashCourse ||
                timer <= 0f
            )
            {
                return;
            }

            Clear(player);
        }

        private void OnEncounterCompleted()
        {
            if (
                Opened &&
                !Cleared &&
                kind != SecretChallengeKind.DashCourse
            )
            {
                Clear(null);
            }
        }

        private void Clear(StrikerPlayerIdentity player)
        {
            if (Cleared)
                return;

            Cleared = true;
            Opened = true;

            SaveGameService save =
                SaveGameService.Active;

            if (save && save.Current != null)
            {
                if (
                    !string.IsNullOrEmpty(weaponRewardId) &&
                    !save.Current.unlockedWeapons.Contains(
                        weaponRewardId
                    )
                )
                {
                    save.Current.unlockedWeapons.Add(
                        weaponRewardId
                    );
                }

                save.Current.skillPoints +=
                    Mathf.Max(0, skillPointReward);

                save.Save();
            }

            NovaStriker.Session.StrikeTeamSession.Active
                ?.AddSynergy(synergyReward);

            ClearedEvent?.Invoke();

            GameplayEventHub.Raise(
                new GameplayCue(
                    GameplayCueType.SecretCleared,
                    player ? player.ActorId : -1,
                    transform.position,
                    Vector2.up,
                    (int)kind,
                    synergyReward,
                    secretId
                )
            );
        }
    }

    /// <summary>
    /// Simple secret-start trigger usable by hidden doors or discovery volumes.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class SecretStartTrigger2D : MonoBehaviour
    {
        [SerializeField] private SecretChallengeController2D challenge;

        private void Reset()
        {
            Collider2D collider = GetComponent<Collider2D>();

            if (collider)
                collider.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            StrikerPlayerIdentity player =
                other.GetComponentInParent<StrikerPlayerIdentity>();

            if (player)
                challenge?.Open(player);
        }
    }

    /// <summary>
    /// Finish trigger for timed traversal secret rooms.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class SecretFinishTrigger2D : MonoBehaviour
    {
        [SerializeField] private SecretChallengeController2D challenge;

        private void Reset()
        {
            Collider2D collider = GetComponent<Collider2D>();

            if (collider)
                collider.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            StrikerPlayerIdentity player =
                other.GetComponentInParent<StrikerPlayerIdentity>();

            if (player)
                challenge?.ReachDashCourseFinish(player);
        }
    }
}
