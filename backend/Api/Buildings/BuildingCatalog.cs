namespace llmmo.Api.Buildings;

public enum BuildingResource
{
    Gold,
    Stone,
    Wood,
    Food,
}

public record BuildingUpgradeCost(int Wood, int Stone, int Gold, int Food);

public record BuildingDefinition(
    string Type,
    string Name,
    bool ProducesResources,
    BuildingResource? Resource,
    int BaseProductionPerLevel,
    BuildingUpgradeCost BaseUpgradeCost);

public static class BuildingCatalog
{
    public static readonly string[] AllTypes =
    [
        "gold_mine",
        "stone_mine",
        "timber_station",
        "bakery",
        "barracks",
    ];

    private static readonly Dictionary<string, BuildingDefinition> ByType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["gold_mine"] = new BuildingDefinition(
            "gold_mine", "Gold Mine", true, BuildingResource.Gold, 3,
            new BuildingUpgradeCost(50, 30, 0, 20)),
        ["stone_mine"] = new BuildingDefinition(
            "stone_mine", "Stone Mine", true, BuildingResource.Stone, 3,
            new BuildingUpgradeCost(40, 20, 10, 15)),
        ["timber_station"] = new BuildingDefinition(
            "timber_station", "Timber Station", true, BuildingResource.Wood, 3,
            new BuildingUpgradeCost(30, 20, 10, 15)),
        ["bakery"] = new BuildingDefinition(
            "bakery", "Bakery", true, BuildingResource.Food, 3,
            new BuildingUpgradeCost(35, 25, 15, 10)),
        ["barracks"] = new BuildingDefinition(
            "barracks", "Barracks", false, null, 0,
            new BuildingUpgradeCost(60, 40, 25, 30)),
    };

    public static bool IsValidType(string type) => ByType.ContainsKey(type);

    public static BuildingDefinition Get(string type) => ByType[type];

    public static int ProductionAtLevel(string type, int level)
    {
        var def = Get(type);
        if (!def.ProducesResources || level <= 0)
        {
            return 0;
        }

        return def.BaseProductionPerLevel * level;
    }

    public static BuildingUpgradeCost UpgradeCostForLevel(string type, int targetLevel)
    {
        var def = Get(type);
        var multiplier = Math.Max(1, targetLevel);

        return new BuildingUpgradeCost(
            def.BaseUpgradeCost.Wood * multiplier,
            def.BaseUpgradeCost.Stone * multiplier,
            def.BaseUpgradeCost.Gold * multiplier,
            def.BaseUpgradeCost.Food * multiplier);
    }
}
