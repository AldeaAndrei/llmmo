using System.Text.Json;
using System.Text.Json.Serialization;

namespace llmmo.Api.Troops;

public record TroopStackEntry(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("count")] int Count);

public static class TroopStackHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static string Serialize(IEnumerable<TroopStackEntry> troops) =>
        JsonSerializer.Serialize(troops, JsonOptions);

    public static List<TroopStackEntry> Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<TroopStackEntry>>(json, JsonOptions) ?? [];
    }

    public static int TotalCount(IEnumerable<TroopStackEntry> troops) =>
        troops.Sum(entry => entry.Count);

    public static int PartySpeed(IEnumerable<TroopStackEntry> troops)
    {
        if (!troops.Any(entry => entry.Count > 0))
        {
            return 1;
        }

        return troops
            .Where(entry => entry.Count > 0)
            .Select(entry => TroopCatalog.Get(entry.Type).Speed)
            .Min();
    }

    public static bool HasCombatTroops(IEnumerable<TroopStackEntry> troops) =>
        troops.Any(entry => entry.Count > 0 && TroopCatalog.Get(entry.Type).IsCombat);
}
