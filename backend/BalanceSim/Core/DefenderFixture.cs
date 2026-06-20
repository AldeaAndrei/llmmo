using System.Text.Json;
using System.Text.Json.Serialization;

namespace BalanceSim.Core;

public record DefenderResources(int Gold, int Stone, int Wood, int Food);

public record DefenderFixture(
    string Name,
    int WallLevel,
    int Soldiers,
    int Distance,
    DefenderResources Resources);

public static class DefenderFixtureLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static IReadOnlyList<DefenderFixture> LoadDefaults()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Config", "defenders-default.json");
        if (File.Exists(path))
        {
            try
            {
                return LoadFromFile(path);
            }
            catch
            {
                return BuiltInDefaults();
            }
        }

        return BuiltInDefaults();
    }

    public static IReadOnlyList<DefenderFixture> LoadFromFile(string path)
    {
        var json = File.ReadAllText(path);
        var fixtures = JsonSerializer.Deserialize<List<DefenderFixtureDto>>(json, JsonOptions)
            ?? [];

        return fixtures.Select(dto => new DefenderFixture(
            dto.Name ?? "unknown",
            dto.WallLevel,
            dto.Soldiers,
            dto.Distance,
            new DefenderResources(
                dto.Resources?.Gold ?? 0,
                dto.Resources?.Stone ?? 0,
                dto.Resources?.Wood ?? 0,
                dto.Resources?.Food ?? 0)))
            .ToList();
    }

    public static IReadOnlyList<DefenderFixture> BuiltInDefaults() =>
    [
        new("early_neutral", 1, 2, 4, new DefenderResources(50, 50, 50, 30)),
        new("midgame_neutral", 5, 10, 8, new DefenderResources(200, 150, 150, 100)),
        new("lategame_neutral", 10, 25, 12, new DefenderResources(400, 350, 350, 200)),
    ];

    private sealed class DefenderFixtureDto
    {
        public string? Name { get; set; }
        public int WallLevel { get; set; }
        public int Soldiers { get; set; }
        public int Distance { get; set; }
        public DefenderResourcesDto? Resources { get; set; }
    }

    private sealed class DefenderResourcesDto
    {
        public int Gold { get; set; }
        public int Stone { get; set; }
        public int Wood { get; set; }
        public int Food { get; set; }
    }
}
