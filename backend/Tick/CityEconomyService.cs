using llmmo.Data;
using llmmo.Entities;
using Microsoft.EntityFrameworkCore;

namespace llmmo.Tick;

/// <summary>
/// Applies per-city resource production then troop food upkeep in a single pass
/// so bakery output is available before upkeep is checked each tick.
/// </summary>
public class CityEconomyService
{
    private readonly AppDbContext _db;

    public CityEconomyService(AppDbContext db)
    {
        _db = db;
    }

    public async Task ApplyProductionThenUpkeepAsync(CancellationToken cancellationToken = default)
    {
        var cities = await _db.Cities
            .Include(city => city.Buildings)
            .Include(city => city.Troops)
            .ToListAsync(cancellationToken);

        foreach (var city in cities)
        {
            ProductionService.ApplyProduction(city);
            TroopUpkeepService.ApplyUpkeep(city);
        }
    }
}
