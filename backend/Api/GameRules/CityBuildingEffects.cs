using llmmo.Api.Buildings;
using llmmo.Entities;

namespace llmmo.Api.GameRules;

public static class CityBuildingEffects
{
    public static void Apply(City city)
    {
        var shedLevel = GetLevel(city, "storage_shed");
        var max = BuildingRules.StorageCapacityAtLevel(shedLevel);
        city.MaxWood = max;
        city.MaxStone = max;
        city.MaxGold = max;
        city.MaxFood = max;

        var academyLevel = GetLevel(city, "spy_academy");
        var survival = BuildingRules.SpySurvivalAtLevel(academyLevel);
        city.SpyDieChance = 1.0 - survival;

        CityResources.ClampToMax(city);
    }

    private static int GetLevel(City city, string buildingType) =>
        city.Buildings
            .FirstOrDefault(b => b.Type.Equals(buildingType, StringComparison.OrdinalIgnoreCase))
            ?.Level ?? 0;
}
