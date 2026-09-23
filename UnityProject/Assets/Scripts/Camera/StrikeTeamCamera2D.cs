using NovaStriker.Player;
using NovaStriker.Session;
using UnityEngine;

namespace NovaStriker.CameraSystem
{
    /// <summary>
    /// One-screen camera for one-to-four local Strikers. It frames all
    /// combat-ready players and scales orthographic size without changing
    /// gameplay simulation.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class StrikeTeamCamera2D : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private StrikeTeamSession session;

        [Header("Framing")]
        [SerializeField] private float minOrthographicSize = 5.8f;
        [SerializeField] private float maxOrthographicSize = 9.5f;
        [SerializeField] private Vector2 worldPadding = new(3.0f, 2.1f);
        [SerializeField] private float positionSmoothTime = 0.12f;
        [SerializeField] private float zoomSmoothTime = 0.18f;

        [Header("Optional Arena Bounds")]
        [SerializeField] private bool arenaBoundsEnabled;
        [SerializeField] private Rect arenaBounds;

        private Vector3 positionVelocity;
        private float zoomVelocity;

        private void Reset()
        {
            targetCamera = GetComponent<Camera>();
        }

        private void Awake()
        {
            if (!targetCamera)
                targetCamera = GetComponent<Camera>();

            if (!session)
                session = StrikeTeamSession.Active;
        }

        private void LateUpdate()
        {
            if (!session)
                session = StrikeTeamSession.Active;

            if (!session || !targetCamera)
                return;

            if (!TryCalculateTeamBounds(out Bounds bounds))
                return;

            Vector3 targetPosition = new(
                bounds.center.x,
                bounds.center.y,
                transform.position.z
            );

            if (arenaBoundsEnabled)
                targetPosition = ClampToArena(targetPosition);

            transform.position =
                Vector3.SmoothDamp(
                    transform.position,
                    targetPosition,
                    ref positionVelocity,
                    Mathf.Max(0.01f, positionSmoothTime)
                );

            float aspect =
                Mathf.Max(0.1f, targetCamera.aspect);

            float verticalRequired =
                bounds.extents.y + worldPadding.y;

            float horizontalRequired =
                (bounds.extents.x + worldPadding.x) /
                aspect;

            float targetSize =
                Mathf.Clamp(
                    Mathf.Max(
                        minOrthographicSize,
                        verticalRequired,
                        horizontalRequired
                    ),
                    minOrthographicSize,
                    maxOrthographicSize
                );

            targetCamera.orthographicSize =
                Mathf.SmoothDamp(
                    targetCamera.orthographicSize,
                    targetSize,
                    ref zoomVelocity,
                    Mathf.Max(0.01f, zoomSmoothTime)
                );
        }

        public void SetArenaBounds(Rect bounds)
        {
            arenaBounds = bounds;
            arenaBoundsEnabled = true;
        }

        public void ClearArenaBounds()
        {
            arenaBoundsEnabled = false;
        }

        private bool TryCalculateTeamBounds(
            out Bounds bounds)
        {
            bounds = default;
            bool initialized = false;

            for (int i = 0; i < StrikeTeamSession.MaxPlayers; i++)
            {
                StrikerPlayerIdentity player =
                    session.GetPlayer(i);

                if (!player || !player.isActiveAndEnabled)
                    continue;

                Vector3 position =
                    player.transform.position;

                if (!initialized)
                {
                    bounds =
                        new Bounds(
                            position,
                            Vector3.zero
                        );

                    initialized = true;
                }
                else
                {
                    bounds.Encapsulate(position);
                }
            }

            return initialized;
        }

        private Vector3 ClampToArena(
            Vector3 position)
        {
            position.x =
                Mathf.Clamp(
                    position.x,
                    arenaBounds.xMin,
                    arenaBounds.xMax
                );

            position.y =
                Mathf.Clamp(
                    position.y,
                    arenaBounds.yMin,
                    arenaBounds.yMax
                );

            return position;
        }
    }
}
