using llmmo.Entities;

namespace llmmo.Api.Buildings;

public static class CityResources
{
    public static bool CanAfford(City city, BuildingUpgradeCost cost) =>
        city.Wood >= cost.Wood
        && city.Stone >= cost.Stone
        && city.Gold >= cost.Gold
        && city.Food >= cost.Food;

    public static void Deduct(City city, BuildingUpgradeCost cost)
    {
        city.Wood -= cost.Wood;
        city.Stone -= cost.Stone;
        city.Gold -= cost.Gold;
        city.Food -= cost.Food;
    }

    public static void Refund(City city, BuildingUpgradeCost cost)
    {
        city.Wood += cost.Wood;
        city.Stone += cost.Stone;
        city.Gold += cost.Gold;
        city.Food += cost.Food;
    }
}
