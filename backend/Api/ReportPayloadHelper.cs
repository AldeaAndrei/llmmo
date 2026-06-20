using System.Text.Json;
using llmmo.Api.Troops;

namespace llmmo.Api;

public static class ReportPayloadHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static object ToResources(int wood, int stone, int gold, int food) => new
    {
        wood,
        stone,
        gold,
        food,
    };

    public static object ToLoot(int wood, int stone, int gold, int food) => new
    {
        wood,
        stone,
        gold,
        food,
    };

    /// <summary>
    /// Troop snapshot for reports: each stack shows remaining count and losses in
    /// "remaining (-lost)" form (e.g. soldier 10 (-6) means 16 started, 6 lost).
    /// </summary>
    public static object ToAttackerSide(
        int totalPower,
        IReadOnlyList<TroopStackEntry> before,
        IReadOnlyList<TroopStackEntry> losses)
    {
        return new
        {
            totalPower,
            troops = BuildTroopSnapshots(before, losses),
        };
    }

    public static object ToDefenderSide(
        int totalPower,
        int troopPower,
        int wallBonus,
        IReadOnlyList<TroopStackEntry> before,
        IReadOnlyDictionary<string, int> losses)
    {
        var lossStacks = losses
            .Select(kvp => new TroopStackEntry(kvp.Key, kvp.Value))
            .ToList();

        return new
        {
            totalPower,
            troopPower,
            wallBonus,
            troops = BuildTroopSnapshots(before, lossStacks),
        };
    }

    private static List<object> BuildTroopSnapshots(
        IReadOnlyList<TroopStackEntry> before,
        IReadOnlyList<TroopStackEntry> losses)
    {
        var lossByType = losses.ToDictionary(
            l => l.Type,
            l => l.Count,
            StringComparer.OrdinalIgnoreCase);

        return before
            .OrderBy(t => t.Type, StringComparer.OrdinalIgnoreCase)
            .Select(t =>
            {
                var lost = lossByType.GetValueOrDefault(t.Type, 0);
                return (object)new
                {
                    type = t.Type,
                    before = t.Count,
                    lost,
                    remaining = Math.Max(0, t.Count - lost),
                };
            })
            .ToList();
    }

    public static string Serialize<T>(T payload) =>
        JsonSerializer.Serialize(payload, JsonOptions);

    public static object Deserialize(string json) =>
        JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
}
