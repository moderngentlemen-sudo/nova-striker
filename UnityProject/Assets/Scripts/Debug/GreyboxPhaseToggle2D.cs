using UnityEngine;

namespace NovaStriker.Debugging
{
    /// <summary>
    /// Greybox-only alternating platform groups for testing reconfiguring level
    /// spaces. Production geometry/animation replaces this helper later.
    /// </summary>
    public sealed class GreyboxPhaseToggle2D : MonoBehaviour
    {
        [SerializeField] private GameObject[] phaseA;
        [SerializeField] private GameObject[] phaseB;
        [SerializeField, Min(0.5f)] private float interval = 2.75f;
        [SerializeField] private bool startWithA = true;

        private float timer;
        private bool phaseAActive;

        public void Configure(
            GameObject[] a,
            GameObject[] b,
            float seconds = 2.75f,
            bool beginWithA = true)
        {
            phaseA = a;
            phaseB = b;
            interval = Mathf.Max(0.5f, seconds);
            startWithA = beginWithA;
        }

        private void OnEnable()
        {
            timer = 0f;
            phaseAActive = startWithA;
            ApplyPhase();
        }

        private void Update()
        {
            timer += Time.deltaTime;

            if (timer < interval)
                return;

            timer = 0f;
            phaseAActive = !phaseAActive;
            ApplyPhase();
        }

        private void ApplyPhase()
        {
            SetActive(phaseA, phaseAActive);
            SetActive(phaseB, !phaseAActive);
        }

        private static void SetActive(
            GameObject[] objects,
            bool active)
        {
            if (objects == null)
                return;

            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i])
                    objects[i].SetActive(active);
            }
        }
    }
}
