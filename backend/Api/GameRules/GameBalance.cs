namespace llmmo.Api.GameRules;

/// <summary>Single source of truth for numeric game balance constants.</summary>
public static class GameBalance
{
    public const int MaxBuildingLevel = 20;

    public const int BaseMaxResource = 1000;
    public const int StorageBonusPerLevel = 150;

    public const int ProductionPerLevel = 3;

    public const double SpySurvivalBase = 0.50;
    public const double SpySurvivalBonusPerLevel = 0.015;
    public const double SpySurvivalCap = 0.80;

    public const int WallDefencePerLevel = 10;
    public const int BarracksTrainCapPerLevel = 4;

    public const int UpgradeTicksPerLevel = 2;
    public const int TrainDurationTicks = 1;

    public const double CombatWinnerLossFactor = 0.5;
    public const double CombatLoserWipeFactor = 0.9;
}
