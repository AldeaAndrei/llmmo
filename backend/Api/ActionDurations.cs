using llmmo.Api.GameRules;

namespace llmmo.Api;

public static class ActionDurations
{
    public static bool IsAllowedType(string type) =>
        type.Equals("upgrade", StringComparison.OrdinalIgnoreCase)
        || type.Equals("train", StringComparison.OrdinalIgnoreCase);

    public static int GetUpgradeDurationTicks(int currentLevel) =>
        BuildingRules.UpgradeDurationTicks(currentLevel);

    public static int GetTrainDurationTicks(int count, int barracksLevel) =>
        BuildingRules.TrainDurationTicks(count, BuildingRules.TrainCapacityAtLevel(barracksLevel));

    public static bool IsUpgradeSlotType(string type) =>
        type.Equals("upgrade", StringComparison.OrdinalIgnoreCase);

    public static bool IsTrainSlotType(string type) =>
        type.Equals("train", StringComparison.OrdinalIgnoreCase);
}
