using NovaStriker.Combat;
using NovaStriker.Core;
using NovaStriker.Player;
using NovaStriker.Session;
using UnityEngine;

namespace NovaStriker.Campaign
{
    public enum PickupKind
    {
        HealthSmall = 0,
        HealthLarge = 1,
        SuitEnergy = 2,
        Synergy = 3,
        UltimateCharge = 4
    }

    /// <summary>
    /// Presentation-independent pickup. Visual mesh/VFX can be replaced later
    /// without changing collection, ownership, or reward logic.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class Pickup2D : MonoBehaviour
    {
        [SerializeField] private PickupKind kind;
        [SerializeField] private float amount = 20f;
        [SerializeField] private bool sharedTeamPickup;
        [SerializeField] private bool destroyOnCollect = true;

        private bool collected;

        private void Reset()
        {
            Collider2D collider = GetComponent<Collider2D>();

            if (collider)
                collider.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (collected)
                return;

            StrikerPlayerIdentity player =
                other.GetComponentInParent<StrikerPlayerIdentity>();

            if (!player || !player.IsCombatReady)
                return;

            if (!ApplyReward(player))
                return;

            collected = true;

            GameplayEventHub.Raise(
                new GameplayCue(
                    GameplayCueType.PickupCollected,
                    player.ActorId,
                    transform.position,
                    Vector2.up,
                    (int)kind,
                    amount,
                    kind.ToString()
                )
            );

            if (destroyOnCollect)
                Destroy(gameObject);
            else
                gameObject.SetActive(false);
        }

        private bool ApplyReward(StrikerPlayerIdentity player)
        {
            switch (kind)
            {
                case PickupKind.HealthSmall:
                case PickupKind.HealthLarge:
                {
                    if (sharedTeamPickup)
                        return HealTeam(amount);

                    Damageable2D health =
                        player.GetComponent<Damageable2D>();

                    return health && health.Heal(amount) > 0f;
                }

                case PickupKind.SuitEnergy:
                {
                    if (sharedTeamPickup)
                        return RestoreTeamSuitEnergy(amount);

                    StrikeSuitAbilityController suit =
                        player.GetComponent<StrikeSuitAbilityController>();

                    if (!suit)
                        return false;

                    suit.RestoreSuitEnergy(amount);
                    return true;
                }

                case PickupKind.UltimateCharge:
                {
                    if (sharedTeamPickup)
                        return RestoreTeamUltimate(amount);

                    StrikeSuitAbilityController suit =
                        player.GetComponent<StrikeSuitAbilityController>();

                    if (!suit)
                        return false;

                    suit.AddUltimateCharge(amount);
                    return true;
                }

                case PickupKind.Synergy:
                {
                    StrikeTeamSession session =
                        StrikeTeamSession.Active;

                    if (!session)
                        return false;

                    session.AddSynergy(amount);
                    return true;
                }
            }

            return false;
        }

        private static bool HealTeam(float amount)
        {
            StrikeTeamSession session =
                StrikeTeamSession.Active;

            if (!session)
                return false;

            bool applied = false;

            for (int i = 0; i < StrikeTeamSession.MaxPlayers; i++)
            {
                StrikerPlayerIdentity teammate =
                    session.GetPlayer(i);

                if (!teammate || !teammate.IsCombatReady)
                    continue;

                Damageable2D health =
                    teammate.GetComponent<Damageable2D>();

                if (health && health.Heal(amount) > 0f)
                    applied = true;
            }

            return applied;
        }

        private static bool RestoreTeamSuitEnergy(float amount)
        {
            StrikeTeamSession session =
                StrikeTeamSession.Active;

            if (!session)
                return false;

            bool applied = false;

            for (int i = 0; i < StrikeTeamSession.MaxPlayers; i++)
            {
                StrikerPlayerIdentity teammate =
                    session.GetPlayer(i);

                if (!teammate || !teammate.IsCombatReady)
                    continue;

                StrikeSuitAbilityController suit =
                    teammate.GetComponent<StrikeSuitAbilityController>();

                if (!suit)
                    continue;

                suit.RestoreSuitEnergy(amount);
                applied = true;
            }

            return applied;
        }

        private static bool RestoreTeamUltimate(float amount)
        {
            StrikeTeamSession session =
                StrikeTeamSession.Active;

            if (!session)
                return false;

            bool applied = false;

            for (int i = 0; i < StrikeTeamSession.MaxPlayers; i++)
            {
                StrikerPlayerIdentity teammate =
                    session.GetPlayer(i);

                if (!teammate || !teammate.IsCombatReady)
                    continue;

                StrikeSuitAbilityController suit =
                    teammate.GetComponent<StrikeSuitAbilityController>();

                if (!suit)
                    continue;

                suit.AddUltimateCharge(amount);
                applied = true;
            }

            return applied;
        }
    }
}
