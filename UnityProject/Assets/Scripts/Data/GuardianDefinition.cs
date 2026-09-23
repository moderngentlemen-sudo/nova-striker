using UnityEngine;

namespace NovaStriker.Data
{
    public enum GuardianId
    {
        Aegis,
        Cinder,
        Mycel,
        Rime,
        Tempest,
        Null
    }

    [CreateAssetMenu(menuName = "Nova Striker/Guardian Definition", fileName = "Guardian_")]
    public sealed class GuardianDefinition : ScriptableObject
    {
        public GuardianId Id;
        public string DisplayName;

        [Min(0f)]
        public float CooldownSeconds = 8f;

        [TextArea]
        public string GameplayDescription;
    }
}
