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
        ClampToMax(city);
    }

    public static void ClampToMax(City city)
    {
        city.Wood = Math.Min(city.Wood, city.MaxWood);
        city.Stone = Math.Min(city.Stone, city.MaxStone);
        city.Gold = Math.Min(city.Gold, city.MaxGold);
        city.Food = Math.Min(city.Food, city.MaxFood);
    }
}
