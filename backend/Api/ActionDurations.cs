namespace llmmo.Api;

public static class ActionDurations
{
    private static readonly Dictionary<string, int> TicksByType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["build"] = 3,
        ["train"] = 1,
        ["attack"] = 1,
        ["scout"] = 1,
    };

    public static bool IsAllowedType(string type) => TicksByType.ContainsKey(type);

    public static int GetDurationTicks(string type) => TicksByType[type];
}
