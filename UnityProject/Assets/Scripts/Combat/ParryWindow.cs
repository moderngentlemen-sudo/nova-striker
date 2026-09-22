using UnityEngine;

namespace NovaStriker.Combat
{
    /// <summary>
    /// Shared parry timing model carried forward from the browser reference.
    ///
    /// Active: 0.035s through 0.145s.
    /// Perfect: 0.035s through 0.078s.
    /// Full action: 0.405s.
    /// </summary>
    [System.Serializable]
    public struct ParryWindow
    {
        [Min(0f)] public float Startup;
        [Min(0f)] public float Active;
        [Min(0f)] public float Perfect;
        [Min(0f)] public float Recovery;

        public static ParryWindow Default => new()
        {
            Startup = 0.035f,
            Active = 0.110f,
            Perfect = 0.043f,
            Recovery = 0.260f
        };

        public float TotalDuration => Startup + Active + Recovery;

        public bool IsActive(float elapsed) =>
            elapsed >= Startup &&
            elapsed <= Startup + Active;

        public bool IsPerfect(float elapsed) =>
            elapsed >= Startup &&
            elapsed <= Startup + Mathf.Min(Perfect, Active);
    }
}
