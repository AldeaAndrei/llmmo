using System.Text.Json;

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

    public static string Serialize<T>(T payload) =>
        JsonSerializer.Serialize(payload, JsonOptions);

    public static object Deserialize(string json) =>
        JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
}
