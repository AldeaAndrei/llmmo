using llmmo.Api;
using llmmo.Data;
using llmmo.Entities;
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
            ApplyProduction(city);
        }
    }

    public static void ApplyProduction(City city) =>
        CityResourceCalculator.ApplyCappedProduction(city);
}
