namespace NovaStriker.Combat
{
    public enum ChargeTier
    {
        Quick = 1,
        Burst = 2,
        VelocityBreak = 3
    }

    public static class ChargeTierRules
    {
        public const float BurstThreshold = 0.30f;
        public const float VelocityBreakThreshold = 0.85f;

        public static ChargeTier FromDashCharge(float seconds)
        {
            if (seconds >= VelocityBreakThreshold)
                return ChargeTier.VelocityBreak;

            if (seconds >= BurstThreshold)
                return ChargeTier.Burst;

            return ChargeTier.Quick;
        }
    }
}
