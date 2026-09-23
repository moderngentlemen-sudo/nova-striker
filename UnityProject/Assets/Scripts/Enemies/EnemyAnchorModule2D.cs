using UnityEngine;

namespace NovaStriker.Enemies
{
    /// <summary>
    /// Passive baseline role used to validate the shared EnemyBrain2D shell
    /// without autonomous locomotion or attacks.
    /// </summary>
    public sealed class EnemyAnchorModule2D : EnemyRoleModule2D
    {
        [SerializeField] private float braking = 30f;

        public override EnemyRole Role => EnemyRole.Anchor;

        public override void TickIdle(EnemyBrain2D brain, float dt)
        {
            brain.StopHorizontal(braking);
        }

        public override void TickEngage(EnemyBrain2D brain, float dt)
        {
            brain.StopHorizontal(braking);
        }
    }
}
