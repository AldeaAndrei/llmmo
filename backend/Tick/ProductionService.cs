using llmmo.Api.Buildings;
using llmmo.Data;
using Microsoft.EntityFrameworkCore;

namespace llmmo.Tick;

public class ProductionService
{
    private readonly AppDbContext _db;

    public ProductionService(AppDbContext db)
    {
        _db = db;
    }

    public async Task ApplyProductionAsync(CancellationToken cancellationToken = default)
    {
        var cities = await _db.Cities
            .Include(city => city.Buildings)
            .ToListAsync(cancellationToken);

        foreach (var city in cities)
        {
            foreach (var building in city.Buildings)
            {
                var def = BuildingCatalog.Get(building.Type);
                if (!def.ProducesResources)
                {
                    continue;
                }

                var amount = BuildingCatalog.ProductionAtLevel(building.Type, building.Level);
                switch (def.Resource)
                {
                    case BuildingResource.Gold:
                        city.Gold += amount;
                        break;
                    case BuildingResource.Stone:
                        city.Stone += amount;
                        break;
                    case BuildingResource.Wood:
                        city.Wood += amount;
                        break;
                    case BuildingResource.Food:
                        city.Food += amount;
                        break;
                }
            }
        }
    }
}
