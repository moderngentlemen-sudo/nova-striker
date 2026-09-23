using NovaStriker.Combat;
using NovaStriker.Data;
using NovaStriker.Input;
using NovaStriker.Session;
using UnityEngine;

namespace NovaStriker.Player
{
    /// <summary>
    /// Stable player-slot and Strike Team identity. Character selection,
    /// team role, and input slot are intentionally separate concepts.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StrikerPlayerIdentity : MonoBehaviour
    {
        [Header("Local Player")]
        [SerializeField, Range(0, StrikeTeamSession.MaxPlayers - 1)]
        private int playerSlot;
        [SerializeField] private bool participating = true;

        [Header("Strike Team")]
        [SerializeField] private StrikeTeamRole teamRole =
            StrikeTeamRole.Striker1;

        [Header("References")]
        [SerializeField] private Damageable2D damageable;
        [SerializeField] private NovaCombatController combat;
        [SerializeField] private StrikeTeamSession session;

        public int PlayerSlot => playerSlot;
        public int ActorId =>
            damageable
                ? damageable.ActorId
                : playerSlot;

        public bool IsParticipating => participating;

        public StrikeTeamRole TeamRole => teamRole;
        public StrikeTeamCommandRank CommandRank =>
            StrikeTeamRoleRules.CommandRank(teamRole);

        public StrikerCharacter Character =>
            combat
                ? combat.Character
                : StrikerCharacter.Nova;

        public bool IsCombatReady =>
            participating &&
            damageable &&
            !damageable.IsDefeated;

        private void Reset()
        {
            damageable = GetComponent<Damageable2D>();
            combat = GetComponent<NovaCombatController>();
        }

        private void Awake()
        {
            if (!damageable)
                damageable = GetComponent<Damageable2D>();

            if (!combat)
                combat = GetComponent<NovaCombatController>();

            ResolveSession();
        }

        private void OnEnable()
        {
            ResolveSession();
            session?.Register(this);
        }

        private void OnDisable()
        {
            session?.Unregister(this);
        }

        public void Configure(
            int slot,
            StrikeTeamRole role,
            StrikeTeamSession teamSession)
        {
            session?.Unregister(this);

            playerSlot =
                Mathf.Clamp(
                    slot,
                    0,
                    StrikeTeamSession.MaxPlayers - 1
                );

            teamRole = role;
            session = teamSession;

            if (isActiveAndEnabled)
                session?.Register(this);
        }

        public void SetParticipation(bool value)
        {
            if (participating == value)
                return;

            participating = value;

            if (!participating)
            {
                GetComponent<NovaPlayerGameplay>()?.ClearInput();
            }

            session?.NotifyParticipationChanged(this);
        }

        public void SubmitTeamInput(
            PlayerInputState input)
        {
            if (input.SyncPressed)
                session?.RequestTeamSync(playerSlot);
        }

        private void ResolveSession()
        {
            if (session)
                return;

            session =
                StrikeTeamSession.Active
                ? StrikeTeamSession.Active
                : Object.FindFirstObjectByType<StrikeTeamSession>();
        }
    }
}
