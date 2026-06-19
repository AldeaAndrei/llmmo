using llmmo.Api.Buildings;
using llmmo.Api.GameRules;
using llmmo.Api.Troops;
using llmmo.Data;
using llmmo.Entities;

namespace llmmo.Api;

public static class CityBootstrap
{
    public const int DefaultSoldierCount = 0;

    public static void AddDefaults(City city, AppDbContext db, int soldierCount = DefaultSoldierCount)
    {
        foreach (var building in BuildingSetup.CreateDefaults(city.Id))
        {
            city.Buildings.Add(building);
            db.Buildings.Add(building);
        }

        foreach (var troop in TroopSetup.CreateDefaults(city.Id))
        {
            if (troop.Type.Equals("soldier", StringComparison.OrdinalIgnoreCase))
            {
                troop.Quantity = soldierCount;
            }

            city.Troops.Add(troop);
            db.CityTroops.Add(troop);
        }

        CityBuildingEffects.Apply(city);
    }
}
