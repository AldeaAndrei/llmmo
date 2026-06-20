using llmmo.Api.Buildings;

namespace BalanceSim.Core;

public enum SimActionKind
{
    Upgrade,
    Train,
    Scout,
    Attack,
}

public record CandidateAction(
    SimActionKind Kind,
    string? BuildingType = null,
    string? TroopType = null,
    int Count = 1,
    string? FixtureName = null);

public record PendingAction(
    SimActionKind Kind,
    int ReadyAtTick,
    string? BuildingType = null,
    string? TroopType = null,
    int Count = 1,
    string? FixtureName = null);

public record SimResources(int Gold, int Stone, int Wood, int Food);

public record SimResourceCaps(int Gold, int Stone, int Wood, int Food);

public record SpendLedger(
    int UpgradeWood,
    int UpgradeStone,
    int UpgradeGold,
    int UpgradeFood,
    int TrainWood,
    int TrainStone,
    int TrainGold,
    int TrainFood)
{
    public int TotalWood => UpgradeWood + TrainWood;
    public int TotalStone => UpgradeStone + TrainStone;
    public int TotalGold => UpgradeGold + TrainGold;
    public int TotalFood => UpgradeFood + TrainFood;
    public int Total => TotalWood + TotalStone + TotalGold + TotalFood;
}

public enum MilitaryPhase
{
    Outbound,
    Returning,
}

public record MilitaryMission(
    string FixtureName,
    MilitaryPhase Phase,
    int SoldiersSent,
    int TicksRemaining,
    int LootGold,
    int LootStone,
    int LootWood,
    int LootFood,
    bool Won);

public record SimEvent(string Type, string Detail);
