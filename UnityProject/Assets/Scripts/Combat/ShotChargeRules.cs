namespace NovaStriker.Combat
{
    /// <summary>
    /// Charge thresholds carried forward from the browser gameplay reference.
    /// Tier 0 is an immediate/uncharged shot.
    /// </summary>
    public static class ShotChargeRules
    {
        public const float Tier1Threshold = 0.40f;
        public const float Tier2Threshold = 0.92f;
        public const float Tier3Threshold = 1.58f;

        public static int TierFromSeconds(float seconds)
        {
            if (seconds >= Tier3Threshold)
                return 3;

            if (seconds >= Tier2Threshold)
                return 2;

            if (seconds >= Tier1Threshold)
                return 1;

            return 0;
        }
    }
}
