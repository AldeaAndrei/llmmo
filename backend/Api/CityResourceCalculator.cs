using llmmo.Api.Buildings;
using llmmo.Api.Dtos;
using llmmo.Api.Troops;
using llmmo.Entities;

namespace llmmo.Api;

public record ResourceProduction(int Gold, int Stone, int Wood, int Food);

public static class CityResourceCalculator
{
    public const int DefaultMaxResource = 1000;

    public static ResourceProduction CalculateProduction(City city)
    {
        var gold = 0;
        var stone = 0;
        var wood = 0;
        var food = 0;

        foreach (var building in city.Buildings)
        {
            var def = BuildingCatalog.Get(building.Type);
            if (!def.ProducesResources || def.Resource is null)
            {
                continue;
            }

            var amount = BuildingCatalog.ProductionAtLevel(building.Type, building.Level);
            switch (def.Resource)
            {
                case BuildingResource.Gold:
                    gold += amount;
                    break;
                case BuildingResource.Stone:
                    stone += amount;
                    break;
                case BuildingResource.Wood:
                    wood += amount;
                    break;
                case BuildingResource.Food:
                    food += amount;
                    break;
            }
        }

        return new ResourceProduction(gold, stone, wood, food);
    }

    public static int CalculateFoodUpkeep(City city) =>
        city.Troops.Sum(troop =>
        {
            if (troop.Quantity <= 0 || !TroopCatalog.IsValidType(troop.Type))
            {
                return 0;
            }

            return troop.Quantity * TroopCatalog.Get(troop.Type).Upkeep;
        });

    public static void ApplyCappedProduction(City city)
    {
        var production = CalculateProduction(city);

        if (production.Gold > 0)
        {
            city.Gold = Math.Min(city.MaxGold, city.Gold + production.Gold);
        }

        if (production.Stone > 0)
        {
            city.Stone = Math.Min(city.MaxStone, city.Stone + production.Stone);
        }

        if (production.Wood > 0)
        {
            city.Wood = Math.Min(city.MaxWood, city.Wood + production.Wood);
        }

        if (production.Food > 0)
        {
            city.Food = Math.Min(city.MaxFood, city.Food + production.Food);
        }
    }

    public static int GrossTickDelta(int current, int max, int production) =>
        production <= 0 ? 0 : Math.Min(production, Math.Max(0, max - current));

    public static int NetFoodTickDelta(City city)
    {
        var production = CalculateProduction(city);
        var upkeep = CalculateFoodUpkeep(city);
        var effectiveProduction = GrossTickDelta(city.Food, city.MaxFood, production.Food);
        return effectiveProduction - upkeep;
    }

    public static CityResourcesViewDto BuildResourcesView(City city)
    {
        var production = CalculateProduction(city);

        return new CityResourcesViewDto(
            new CityResourceViewDto(city.Gold, city.MaxGold, production.Gold),
            new CityResourceViewDto(city.Stone, city.MaxStone, production.Stone),
            new CityResourceViewDto(city.Wood, city.MaxWood, production.Wood),
            new CityResourceViewDto(city.Food, city.MaxFood, NetFoodTickDelta(city)));
    }
}
