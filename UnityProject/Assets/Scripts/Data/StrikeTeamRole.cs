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

    /// <summary>
    /// Art-agnostic contract for the two future Strike Team roles and the two
    /// current Striker slots. These values describe systems expectations only;
    /// they do not silently modify Nova/Echo balance.
    /// </summary>
    public readonly struct StrikeTeamRoleGameplayContract
    {
        public StrikeTeamRoleGameplayContract(
            StrikeTeamRole role,
            string gameplayFocus,
            float threatWeight,
            float reviveContribution,
            float assistContribution,
            bool expectsProtectiveAbility,
            bool expectsRecoveryAbility)
        {
            Role = role;
            GameplayFocus = gameplayFocus;
            ThreatWeight = threatWeight;
            ReviveContribution = reviveContribution;
            AssistContribution = assistContribution;
            ExpectsProtectiveAbility = expectsProtectiveAbility;
            ExpectsRecoveryAbility = expectsRecoveryAbility;
        }

        public StrikeTeamRole Role { get; }
        public string GameplayFocus { get; }
        public float ThreatWeight { get; }
        public float ReviveContribution { get; }
        public float AssistContribution { get; }
        public bool ExpectsProtectiveAbility { get; }
        public bool ExpectsRecoveryAbility { get; }
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

        public static StrikeTeamRoleGameplayContract GameplayContract(
            StrikeTeamRole role)
        {
            return role switch
            {
                StrikeTeamRole.Tank =>
                    new StrikeTeamRoleGameplayContract(
                        role,
                        "Defense anchor, interception, space control",
                        1.35f,
                        1f,
                        1.10f,
                        true,
                        false
                    ),

                StrikeTeamRole.Support =>
                    new StrikeTeamRoleGameplayContract(
                        role,
                        "Recovery, ally amplification, battlefield control",
                        0.80f,
                        1.35f,
                        1.20f,
                        true,
                        true
                    ),

                StrikeTeamRole.Striker0 =>
                    new StrikeTeamRoleGameplayContract(
                        role,
                        "Second-in-command assault flex",
                        1f,
                        1f,
                        1.10f,
                        false,
                        false
                    ),

                _ =>
                    new StrikeTeamRoleGameplayContract(
                        StrikeTeamRole.Striker1,
                        "Team-lead Striker and tactical initiator",
                        1f,
                        1f,
                        1.10f,
                        false,
                        false
                    )
            };
        }
    }
}
