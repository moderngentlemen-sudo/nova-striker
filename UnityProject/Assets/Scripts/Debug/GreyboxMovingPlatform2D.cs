using UnityEngine;

namespace NovaStriker.Debugging
{
    /// <summary>
    /// Greybox-only kinematic platform motion used to validate level topology
    /// before production environment animation exists.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class GreyboxMovingPlatform2D : MonoBehaviour
    {
        [SerializeField] private Vector2 travelOffset = new(4f, 0f);
        [SerializeField, Min(0.25f)] private float oneWaySeconds = 2.5f;
        [SerializeField, Range(0f, 1f)] private float phaseOffset;

        private Rigidbody2D body;
        private Vector2 origin;
        private float clock;

        public void Configure(
            Vector2 offset,
            float seconds,
            float phase = 0f)
        {
            travelOffset = offset;
            oneWaySeconds = Mathf.Max(0.25f, seconds);
            phaseOffset = Mathf.Repeat(phase, 1f);
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            origin = body.position;
            clock = phaseOffset * oneWaySeconds * 2f;
        }

        private void FixedUpdate()
        {
            clock += Time.fixedDeltaTime;

            float normalized =
                Mathf.PingPong(
                    clock / Mathf.Max(0.25f, oneWaySeconds),
                    1f
                );

            body.MovePosition(
                origin +
                travelOffset * normalized
            );
        }
    }
}
