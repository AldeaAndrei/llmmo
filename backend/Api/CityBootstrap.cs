using llmmo.Api.Buildings;
using llmmo.Api.Troops;
using llmmo.Data;
using llmmo.Entities;

namespace llmmo.Api;

public static class CityBootstrap
{
    public const int DefaultSoldierCount = 0;

    public static void AddDefaults(AppDbContext db, Guid cityId, int soldierCount = DefaultSoldierCount)
    {
        db.Buildings.AddRange(BuildingSetup.CreateDefaults(cityId));

        foreach (var troop in TroopSetup.CreateDefaults(cityId))
        {
            if (troop.Type.Equals("soldier", StringComparison.OrdinalIgnoreCase))
            {
                troop.Quantity = soldierCount;
            }

            db.CityTroops.Add(troop);
        }
    }
}
