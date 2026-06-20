using llmmo.Api.GameRules;
using llmmo.Api.Troops;

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
    public static string[] AllTypes => BuildingRules.AllTypes;

    public static bool IsValidType(string type) => BuildingRules.IsValidType(type);

    public static BuildingDefinition Get(string type)
    {
        var rule = BuildingRules.Get(type);
        return new BuildingDefinition(
            rule.Type,
            rule.Name,
            rule.EffectKind == BuildingEffectKind.Production,
            rule.Resource,
            rule.EffectKind == BuildingEffectKind.Production ? BuildingRules.ProductionPerLevel : 0,
            rule.BaseUpgradeCost);
    }

    public static int ProductionAtLevel(string type, int level) =>
        BuildingRules.ProductionAtLevel(type, level);

    public static BuildingUpgradeCost UpgradeCostForLevel(string type, int targetLevel) =>
        BuildingRules.UpgradeCostForLevel(type, targetLevel);

    public static BuildingUpgradeCost TrainCostPerTroop =>
        TroopRules.Get("soldier").TrainCostPerUnit;

    public static int TrainCapacityAtLevel(int barracksLevel) =>
        BuildingRules.TrainCapacityAtLevel(barracksLevel);

    public static BuildingUpgradeCost TrainCostForCount(int count) =>
        TroopRules.TrainCostForCount("soldier", count);

    public static int UpgradeDurationTicks(int currentLevel) =>
        BuildingRules.UpgradeDurationTicks(currentLevel);

    public static int TrainDurationTicks(int count, int barracksLevel) =>
        BuildingRules.TrainDurationTicks(count, TrainCapacityAtLevel(barracksLevel));
}
