using System.Collections.Generic;
using NovaStriker.Player;
using NovaStriker.Session;
using UnityEngine;

namespace NovaStriker.Enemies
{
    public enum EnemySquadTactic
    {
        Advance = 0,
        Fortify = 1,
        Pincer = 2,
        Crossfire = 3
    }

    /// <summary>
    /// Lightweight squad-level positioning layer inspired by the preserved
    /// browser tactics. Individual role modules still own locomotion/attacks;
    /// this coordinator only supplies desired offsets around the current team
    /// target so mixed-role groups behave as a formation rather than a crowd.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    public sealed class EnemySquadCoordinator2D : MonoBehaviour
    {
        [SerializeField] private StrikeTeamSession session;

        [Header("Grouping")]
        [SerializeField] private float squadGapDistance = 8.5f;
        [SerializeField] private float tacticRefreshInterval = 3.0f;

        [Header("Unity-space role offsets")]
        [SerializeField] private float anchorOffset = 2.2f;
        [SerializeField] private float artilleryOffset = 6.8f;
        [SerializeField] private float flankerOffset = 4.0f;
        [SerializeField] private float pincerFlankerOffset = 5.5f;
        [SerializeField] private float skirmisherOffset = 2.0f;
        [SerializeField] private Vector2 aerialOffset = new(2.2f, 2.7f);

        private readonly HashSet<EnemyBrain2D> registered = new();
        private readonly List<EnemyBrain2D> alive = new();
        private readonly List<List<EnemyBrain2D>> squadPool = new();
        private int activeSquadCount;

        private float refreshTimer;

        public static EnemySquadCoordinator2D Active { get; private set; }

        private void Awake()
        {
            Active = this;

            if (!session)
                session = StrikeTeamSession.Active;

            EnemyBrain2D[] existing =
                FindObjectsByType<EnemyBrain2D>(
                    FindObjectsInactive.Exclude
                );

            for (int i = 0; i < existing.Length; i++)
                Register(existing[i]);

            refreshTimer = 0f;
        }

        private void OnDestroy()
        {
            if (Active == this)
                Active = null;
        }

        private void Update()
        {
            if (!session)
                session = StrikeTeamSession.Active;

            refreshTimer -= Time.deltaTime;

            if (refreshTimer > 0f)
                return;

            refreshTimer =
                Mathf.Max(0.25f, tacticRefreshInterval);

            RebuildAndAssign();
        }

        public void Register(EnemyBrain2D brain)
        {
            if (brain)
                registered.Add(brain);
        }

        public void Unregister(EnemyBrain2D brain)
        {
            if (!brain)
                return;

            registered.Remove(brain);
            brain.ClearTacticalTargetOffset();
        }

        private void RebuildAndAssign()
        {
            alive.Clear();

            foreach (EnemyBrain2D brain in registered)
            {
                if (
                    brain &&
                    brain.isActiveAndEnabled &&
                    !brain.IsDefeated
                )
                {
                    alive.Add(brain);
                }
            }

            alive.Sort(
                (a, b) =>
                    a.transform.position.x.CompareTo(
                        b.transform.position.x
                    )
            );

            for (int i = 0; i < squadPool.Count; i++)
                squadPool[i].Clear();

            activeSquadCount = 0;

            List<EnemyBrain2D> current = null;
            float previousX = float.NegativeInfinity;

            for (int i = 0; i < alive.Count; i++)
            {
                EnemyBrain2D brain = alive[i];
                float x = brain.transform.position.x;

                if (
                    current == null ||
                    x - previousX > squadGapDistance
                )
                {
                    if (activeSquadCount >= squadPool.Count)
                    {
                        squadPool.Add(
                            new List<EnemyBrain2D>(8)
                        );
                    }

                    current =
                        squadPool[activeSquadCount++];
                }

                current.Add(brain);
                previousX = x;
            }

            for (int i = 0; i < activeSquadCount; i++)
                AssignSquad(squadPool[i]);
        }

        private void AssignSquad(List<EnemyBrain2D> squad)
        {
            if (squad == null || squad.Count == 0)
                return;

            Vector2 center = Vector2.zero;

            for (int i = 0; i < squad.Count; i++)
                center += (Vector2)squad[i].transform.position;

            center /= squad.Count;

            StrikerPlayerIdentity target = null;

            session?.TryGetNearestCombatReadyPlayer(
                center,
                out target
            );

            if (!target)
            {
                for (int i = 0; i < squad.Count; i++)
                    squad[i]?.ClearTacticalTargetOffset();

                return;
            }

            bool hasAnchor = false;
            bool hasArtillery = false;
            bool hasFlanker = false;

            for (int i = 0; i < squad.Count; i++)
            {
                switch (squad[i].Role)
                {
                    case EnemyRole.Anchor:
                        hasAnchor = true;
                        break;
                    case EnemyRole.Artillery:
                        hasArtillery = true;
                        break;
                    case EnemyRole.Flanker:
                        hasFlanker = true;
                        break;
                }
            }

            EnemySquadTactic tactic =
                hasAnchor && hasArtillery
                    ? EnemySquadTactic.Fortify
                    : hasFlanker && squad.Count >= 3
                        ? EnemySquadTactic.Pincer
                        : squad.Count >= 4
                            ? EnemySquadTactic.Crossfire
                            : EnemySquadTactic.Advance;

            int flankerIndex = 0;
            int rangedIndex = 0;

            for (int i = 0; i < squad.Count; i++)
            {
                EnemyBrain2D brain = squad[i];

                if (!brain)
                    continue;

                float currentSide =
                    Mathf.Sign(
                        brain.transform.position.x -
                        target.transform.position.x
                    );

                if (Mathf.Approximately(currentSide, 0f))
                    currentSide = (i & 1) == 0 ? -1f : 1f;

                Vector2 offset;

                switch (brain.Role)
                {
                    case EnemyRole.Anchor:
                        offset =
                            Vector2.right *
                            currentSide *
                            anchorOffset;
                        break;

                    case EnemyRole.Artillery:
                    {
                        float side =
                            (rangedIndex++ & 1) == 0
                                ? currentSide
                                : -currentSide;

                        offset =
                            Vector2.right *
                            side *
                            artilleryOffset;
                        break;
                    }

                    case EnemyRole.Flanker:
                    {
                        float side =
                            (flankerIndex++ & 1) == 0
                                ? -1f
                                : 1f;

                        float distance =
                            tactic == EnemySquadTactic.Pincer
                                ? pincerFlankerOffset
                                : flankerOffset;

                        offset =
                            Vector2.right *
                            side *
                            distance;
                        break;
                    }

                    case EnemyRole.Aerial:
                    {
                        float side =
                            (i & 1) == 0 ? -1f : 1f;

                        offset = new Vector2(
                            aerialOffset.x * side,
                            aerialOffset.y
                        );
                        break;
                    }

                    default:
                        offset =
                            Vector2.right *
                            currentSide *
                            skirmisherOffset;
                        break;
                }

                if (
                    tactic == EnemySquadTactic.Crossfire &&
                    brain.Role == EnemyRole.Skirmisher
                )
                {
                    offset.x *=
                        (i & 1) == 0 ? 1.45f : -1.45f;
                }

                brain.SetTacticalTargetOffset(offset);
            }
        }
    }
}
