using System;
using NovaStriker.Combat;
using NovaStriker.Player;
using NovaStriker.Session;
using UnityEngine;

namespace NovaStriker.Campaign
{
    /// <summary>
    /// Adaptive post-encounter recovery. The reward choice is based on current
    /// team state so four-player sessions do not need hand-authored duplicate
    /// pickup layouts for every encounter.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EncounterRewardSpawner2D : MonoBehaviour
    {
        [SerializeField] private EncounterController2D encounter;
        [SerializeField] private Pickup2D pickupPrefab;
        [SerializeField] private Transform rewardPoint;
        [SerializeField] private StrikeTeamSession session;

        [Header("Reward Values")]
        [SerializeField] private float healthAmount = 30f;
        [SerializeField] private float suitEnergyAmount = 25f;
        [SerializeField] private float ultimateAmount = 18f;
        [SerializeField] private float synergyAmount = 10f;

        [Header("Need Thresholds")]
        [SerializeField, Range(0f, 1f)] private float healthNeedThreshold = 0.60f;
        [SerializeField, Range(0f, 1f)] private float suitNeedThreshold = 0.50f;

        public event Action<PickupKind> RewardSpawned;

        private void Reset()
        {
            encounter = GetComponent<EncounterController2D>();
        }

        private void Awake()
        {
            if (!encounter)
                encounter = GetComponent<EncounterController2D>();

            if (!session)
                session = StrikeTeamSession.Active;
        }

        private void OnEnable()
        {
            if (encounter)
                encounter.EncounterCompleted += OnEncounterCompleted;
        }

        private void OnDisable()
        {
            if (encounter)
                encounter.EncounterCompleted -= OnEncounterCompleted;
        }

        private void OnEncounterCompleted()
        {
            if (!pickupPrefab)
                return;

            if (!session)
                session = StrikeTeamSession.Active;

            PickupKind kind =
                SelectRewardKind();

            float amount =
                kind switch
                {
                    PickupKind.HealthLarge => healthAmount,
                    PickupKind.SuitEnergy => suitEnergyAmount,
                    PickupKind.UltimateCharge => ultimateAmount,
                    _ => synergyAmount
                };

            Vector3 position =
                rewardPoint
                    ? rewardPoint.position
                    : transform.position;

            Pickup2D pickup =
                Instantiate(
                    pickupPrefab,
                    position,
                    Quaternion.identity
                );

            if (!pickup)
                return;

            pickup.Configure(
                kind,
                amount,
                shared: true
            );

            RewardSpawned?.Invoke(kind);
        }

        private PickupKind SelectRewardKind()
        {
            if (!session)
                return PickupKind.Synergy;

            float lowestHealth = 1f;
            float lowestSuit = 1f;
            bool found = false;
            bool anyUltimateMissing = false;

            for (
                int slot = 0;
                slot < StrikeTeamSession.MaxPlayers;
                slot++
            )
            {
                StrikerPlayerIdentity player =
                    session.GetPlayer(slot);

                if (
                    !player ||
                    !player.IsCombatReady
                )
                {
                    continue;
                }

                found = true;

                Damageable2D health =
                    player.GetComponent<Damageable2D>();

                if (health && health.MaxHealth > 0f)
                {
                    lowestHealth =
                        Mathf.Min(
                            lowestHealth,
                            health.Health / health.MaxHealth
                        );
                }

                StrikeSuitAbilityController suit =
                    player.GetComponent<StrikeSuitAbilityController>();

                if (suit && suit.MaxSuitEnergy > 0f)
                {
                    lowestSuit =
                        Mathf.Min(
                            lowestSuit,
                            suit.SuitEnergy / suit.MaxSuitEnergy
                        );

                    anyUltimateMissing |=
                        suit.UltimateCharge + 0.001f <
                        suit.UltimateChargeRequired;
                }
            }

            if (!found)
                return PickupKind.Synergy;

            if (lowestHealth < healthNeedThreshold)
                return PickupKind.HealthLarge;

            if (lowestSuit < suitNeedThreshold)
                return PickupKind.SuitEnergy;

            if (anyUltimateMissing)
                return PickupKind.UltimateCharge;

            return PickupKind.Synergy;
        }
    }
}
