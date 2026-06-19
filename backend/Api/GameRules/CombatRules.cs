namespace llmmo.Api.GameRules;

/// <summary>Combat balance constants and documented rules.</summary>
public static class CombatRules
{
    public static double WinnerLossFactor => GameBalance.CombatWinnerLossFactor;
    public static double LoserWipeFactor => GameBalance.CombatLoserWipeFactor;
}
