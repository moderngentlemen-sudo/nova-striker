using NovaStriker.Player;
using UnityEngine;

namespace NovaStriker.Campaign
{
    public enum ActObjectiveTriggerRole
    {
        Zone = 0,
        Node = 1,
        Goal = 2
    }

    /// <summary>
    /// Minimal scene trigger that forwards player interaction into the generic
    /// act-objective authority. A node can be activated sequentially in solo or
    /// in parallel by co-op players; no objective requires simultaneous players
    /// unless an authored hold-zone explicitly opts into that behavior.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class ActObjectiveTrigger2D : MonoBehaviour
    {
        [SerializeField] private ActObjectiveController2D objective;
        [SerializeField] private ActObjectiveTriggerRole role;
        [SerializeField] private string nodeId = "node";
        [SerializeField] private bool oneShot = true;

        private bool consumed;

        public void Configure(
            ActObjectiveController2D target,
            ActObjectiveTriggerRole triggerRole,
            string id = null,
            bool useOnce = true)
        {
            objective = target;
            role = triggerRole;
            nodeId =
                string.IsNullOrWhiteSpace(id)
                    ? gameObject.name
                    : id;
            oneShot = useOnce;
        }

        private void Reset()
        {
            Collider2D collider =
                GetComponent<Collider2D>();

            if (collider)
                collider.isTrigger = true;

            if (!objective)
            {
                objective =
                    GetComponentInParent<ActObjectiveController2D>();
            }
        }

        private void Awake()
        {
            if (!objective)
            {
                objective =
                    GetComponentInParent<ActObjectiveController2D>();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!objective)
                return;

            StrikerPlayerIdentity player =
                other.GetComponentInParent<StrikerPlayerIdentity>();

            if (
                !player ||
                !player.IsParticipating
            )
            {
                return;
            }

            switch (role)
            {
                case ActObjectiveTriggerRole.Zone:
                    objective.SetZoneActor(
                        player.ActorId,
                        true
                    );
                    break;

                case ActObjectiveTriggerRole.Node:
                    if (!consumed || !oneShot)
                    {
                        objective.ActivateNode(
                            string.IsNullOrWhiteSpace(nodeId)
                                ? gameObject.name
                                : nodeId
                        );

                        consumed = oneShot;
                    }
                    break;

                case ActObjectiveTriggerRole.Goal:
                    if (!consumed || !oneShot)
                    {
                        objective.ReportGoalReached();
                        consumed = oneShot;
                    }
                    break;
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (
                !objective ||
                role != ActObjectiveTriggerRole.Zone
            )
            {
                return;
            }

            StrikerPlayerIdentity player =
                other.GetComponentInParent<StrikerPlayerIdentity>();

            if (!player)
                return;

            objective.SetZoneActor(
                player.ActorId,
                false
            );
        }

        private void OnDisable()
        {
            consumed = false;
        }
    }
}
