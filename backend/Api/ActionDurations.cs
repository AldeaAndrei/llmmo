namespace llmmo.Api;

public static class ActionDurations
{
    private static readonly Dictionary<string, int> TicksByType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["upgrade"] = 3,
        ["train"] = 1,
    };

    public static bool IsAllowedType(string type) => TicksByType.ContainsKey(type);

    public static int GetDurationTicks(string type) => TicksByType[type];

    public static bool IsUpgradeSlotType(string type) =>
        type.Equals("upgrade", StringComparison.OrdinalIgnoreCase);

    public static bool IsTrainSlotType(string type) =>
        type.Equals("train", StringComparison.OrdinalIgnoreCase);
}
