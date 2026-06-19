using llmmo.Api.GameRules;

namespace llmmo.Api;

public static class ActionDurations
{
    public static bool IsAllowedType(string type) =>
        type.Equals("upgrade", StringComparison.OrdinalIgnoreCase)
        || type.Equals("train", StringComparison.OrdinalIgnoreCase);

    public static int GetDurationTicks(string type) =>
        type.Equals("train", StringComparison.OrdinalIgnoreCase)
            ? GameBalance.TrainDurationTicks
            : throw new InvalidOperationException("Use GetUpgradeDurationTicks for upgrade actions.");

    public static int GetUpgradeDurationTicks(int currentLevel) =>
        BuildingRules.UpgradeDurationTicks(currentLevel);

    public static bool IsUpgradeSlotType(string type) =>
        type.Equals("upgrade", StringComparison.OrdinalIgnoreCase);

    public static bool IsTrainSlotType(string type) =>
        type.Equals("train", StringComparison.OrdinalIgnoreCase);
}
