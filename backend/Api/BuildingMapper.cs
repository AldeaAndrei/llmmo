using llmmo.Api.Buildings;
using llmmo.Api.Dtos;
using llmmo.Api.GameRules;
using llmmo.Entities;

namespace llmmo.Api;

public static class BuildingMapper
{
    public static BuildingDto ToDto(Building building)
    {
        var rule = BuildingRules.Get(building.Type);
        var production = BuildingRules.ProductionAtLevel(building.Type, building.Level);
        var nextCost = BuildingRules.UpgradeCostForLevel(building.Type, building.Level + 1);
        var isBarracks = building.Type.Equals("barracks", StringComparison.OrdinalIgnoreCase);
        BuildingUpgradeCostDto? trainCostPerTroop = null;
        int? trainCapacity = null;

        if (isBarracks)
        {
            var perTroop = TroopRules.Get("soldier").TrainCostPerUnit;
            trainCostPerTroop = new BuildingUpgradeCostDto(
                perTroop.Wood, perTroop.Stone, perTroop.Gold, perTroop.Food);
            trainCapacity = BuildingRules.TrainCapacityAtLevel(building.Level);
        }

        int? upgradeDuration = building.Level < BuildingRules.MaxBuildingLevel
            ? BuildingRules.UpgradeDurationTicks(building.Level)
            : null;

        return new BuildingDto(
            building.Type,
            rule.Name,
            building.Level,
            rule.Description,
            BuildingRules.FormatCurrentEffect(building.Type, building.Level),
            BuildingRules.FormatNextLevelEffect(building.Type, building.Level),
            upgradeDuration,
            rule.EffectKind == BuildingEffectKind.Production ? production : null,
            rule.EffectKind == BuildingEffectKind.Production
                ? rule.Resource!.Value.ToString().ToLowerInvariant()
                : null,
            isBarracks,
            building.Level < BuildingRules.MaxBuildingLevel
                ? new BuildingUpgradeCostDto(nextCost.Wood, nextCost.Stone, nextCost.Gold, nextCost.Food)
                : null,
            trainCostPerTroop,
            trainCapacity);
    }

    public static BuildingCatalogDto ToCatalogDto(string type)
    {
        var rule = BuildingRules.Get(type);
        return new BuildingCatalogDto(
            rule.Type,
            rule.Name,
            rule.Description,
            rule.EffectKind.ToString(),
            BuildingRules.EffectFormulaText(type),
            BuildingRules.MaxBuildingLevel,
            new BuildingUpgradeCostDto(
                rule.BaseUpgradeCost.Wood,
                rule.BaseUpgradeCost.Stone,
                rule.BaseUpgradeCost.Gold,
                rule.BaseUpgradeCost.Food),
            rule.EffectKind == BuildingEffectKind.Production ? BuildingRules.ProductionPerLevel : null,
            rule.EffectKind == BuildingEffectKind.Production
                ? rule.Resource!.Value.ToString().ToLowerInvariant()
                : null);
    }
}
