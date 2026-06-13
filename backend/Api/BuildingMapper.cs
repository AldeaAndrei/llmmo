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

        return new BuildingDto(
            building.Type,
            def.Name,
            building.Level,
            def.ProducesResources ? production : null,
            def.ProducesResources ? def.Resource!.Value.ToString().ToLowerInvariant() : null,
            building.Type.Equals("barracks", StringComparison.OrdinalIgnoreCase),
            new BuildingUpgradeCostDto(nextCost.Wood, nextCost.Stone, nextCost.Gold, nextCost.Food));
    }
}
