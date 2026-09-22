using UnityEngine;

namespace NovaStriker.Traversal
{
    /// <summary>
    /// Level-authored traversal anchor for Echo's contextual grapple.
    /// The collider should normally be a trigger on the GrapplePoint layer.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class GrapplePoint2D : MonoBehaviour
    {
        [SerializeField] private Transform anchor;
        [SerializeField] private bool available = true;
        [SerializeField, Min(0.05f)] private float arrivalDistance = 0.38f;
        [SerializeField] private string grappleId = "grapple-point";

        public bool Available => available;
        public float ArrivalDistance => arrivalDistance;
        public string GrappleId => grappleId;

        public Vector2 AnchorPosition =>
            anchor ? anchor.position : transform.position;

        public void SetAvailable(bool value)
        {
            available = value;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = available
                ? new Color(0.45f, 0.95f, 1f, 0.9f)
                : new Color(0.35f, 0.35f, 0.4f, 0.45f);

            Gizmos.DrawWireSphere(
                AnchorPosition,
                Mathf.Max(0.12f, arrivalDistance)
            );
        }
#endif
    }
}
