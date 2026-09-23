using NovaStriker.Input;
using NovaStriker.Session;
using UnityEngine;

namespace NovaStriker.Player
{
    /// <summary>
    /// Teammate revive interaction. Holding the ability/interact control near a
    /// downed ally contributes revive progress. Multiple teammates can
    /// contribute simultaneously.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StrikerReviveInteractor : MonoBehaviour
    {
        [SerializeField] private StrikerPlayerIdentity identity;
        [SerializeField] private StrikeTeamSession session;
        [SerializeField] private float reviveRange = 1.45f;
        [SerializeField] private float reviveRate = 1.0f;

        private bool interactHeld;

        public bool IsReviving { get; private set; }
        public StrikerDownedState CurrentTarget { get; private set; }

        private void Reset()
        {
            identity = GetComponent<StrikerPlayerIdentity>();
        }

        private void Awake()
        {
            if (!identity)
                identity = GetComponent<StrikerPlayerIdentity>();

            if (!session)
                session = StrikeTeamSession.Active;
        }

        public void SetInput(PlayerInputState input)
        {
            interactHeld = input.AbilityHeld;
        }

        private void FixedUpdate()
        {
            if (!session)
                session = StrikeTeamSession.Active;

            IsReviving = false;
            CurrentTarget = null;

            if (
                !interactHeld ||
                !identity ||
                !identity.IsCombatReady ||
                !session
            )
            {
                return;
            }

            StrikerDownedState target =
                FindNearestDownedTeammate();

            if (!target)
                return;

            IsReviving = true;
            CurrentTarget = target;

            target.AddReviveProgress(
                Time.fixedDeltaTime *
                Mathf.Max(0f, reviveRate)
            );
        }

        private StrikerDownedState FindNearestDownedTeammate()
        {
            StrikerDownedState nearest = null;
            float bestSqr =
                reviveRange * reviveRange;

            for (int i = 0; i < StrikeTeamSession.MaxPlayers; i++)
            {
                StrikerPlayerIdentity teammate =
                    session.GetPlayer(i);

                if (
                    !teammate ||
                    teammate == identity
                )
                {
                    continue;
                }

                StrikerDownedState downed =
                    teammate.GetComponent<StrikerDownedState>();

                if (!downed || !downed.IsDowned)
                    continue;

                float sqr =
                    (
                        (Vector2)teammate.transform.position -
                        (Vector2)transform.position
                    ).sqrMagnitude;

                if (sqr > bestSqr)
                    continue;

                bestSqr = sqr;
                nearest = downed;
            }

            return nearest;
        }
    }
}
