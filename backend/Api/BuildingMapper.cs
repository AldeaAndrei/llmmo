using llmmo.Api.Buildings;
using llmmo.Api.Dtos;
using llmmo.Entities;

namespace llmmo.Api;

public static class BuildingMapper
{
    public static BuildingDto ToDto(Building building)
    {
        var def = BuildingCatalog.Get(building.Type);
        var production = BuildingCatalog.ProductionAtLevel(building.Type, building.Level);
        var nextCost = BuildingCatalog.UpgradeCostForLevel(building.Type, building.Level + 1);
        var isBarracks = building.Type.Equals("barracks", StringComparison.OrdinalIgnoreCase);
        BuildingUpgradeCostDto? trainCostPerTroop = null;
        int? trainCapacity = null;

        if (isBarracks)
        {
            var perTroop = BuildingCatalog.TrainCostPerTroop;
            trainCostPerTroop = new BuildingUpgradeCostDto(
                perTroop.Wood, perTroop.Stone, perTroop.Gold, perTroop.Food);
            trainCapacity = BuildingCatalog.TrainCapacityAtLevel(building.Level);
        }

        return new BuildingDto(
            building.Type,
            def.Name,
            building.Level,
            def.ProducesResources ? production : null,
            def.ProducesResources ? def.Resource!.Value.ToString().ToLowerInvariant() : null,
            isBarracks,
            new BuildingUpgradeCostDto(nextCost.Wood, nextCost.Stone, nextCost.Gold, nextCost.Food),
            trainCostPerTroop,
            trainCapacity);
    }
}
