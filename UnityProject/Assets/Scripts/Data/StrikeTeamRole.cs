namespace NovaStriker.Data
{
    /// <summary>
    /// Canonical four-person Strike Team structure from the Academy:
    /// Tank, Striker 0, Striker 1, Support.
    /// Striker 1 is the team lead; Striker 0 is second-in-command.
    /// </summary>
    public enum StrikeTeamRole
    {
        Tank = 0,
        Striker0 = 1,
        Striker1 = 2,
        Support = 3
    }

    public enum StrikeTeamCommandRank
    {
        Member = 0,
        SecondInCommand = 1,
        Lead = 2
    }

    public static class StrikeTeamRoleRules
    {
        public static StrikeTeamCommandRank CommandRank(
            StrikeTeamRole role)
        {
            return role switch
            {
                StrikeTeamRole.Striker1 =>
                    StrikeTeamCommandRank.Lead,
                StrikeTeamRole.Striker0 =>
                    StrikeTeamCommandRank.SecondInCommand,
                _ =>
                    StrikeTeamCommandRank.Member
            };
        }
    }
}
