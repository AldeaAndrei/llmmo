using llmmo.Api;
using llmmo.Api.Troops;
using llmmo.Data;
using llmmo.Entities;
using Microsoft.EntityFrameworkCore;

namespace llmmo.Tick;

public class TroopUpkeepService
{
    private readonly AppDbContext _db;

    public TroopUpkeepService(AppDbContext db)
    {
        _db = db;
    }

    public async Task ApplyUpkeepAsync(CancellationToken cancellationToken = default)
    {
        var cities = await _db.Cities
            .Include(city => city.Troops)
            .ToListAsync(cancellationToken);

        foreach (var city in cities)
        {
            ApplyUpkeep(city);
        }
    }

    /// <summary>
    /// Deduct food upkeep after production for this tick. If food is insufficient,
    /// clamp to zero and remove one of each troop type.
    /// </summary>
    public static void ApplyUpkeep(City city)
    {
        var requiredFood = CityResourceCalculator.CalculateFoodUpkeep(city);

        if (requiredFood <= 0)
        {
            return;
        }

        if (city.Food >= requiredFood)
        {
            city.Food -= requiredFood;
            return;
        }

        city.Food = 0;

        foreach (var troop in city.Troops)
        {
            troop.Quantity = Math.Max(0, troop.Quantity - 1);
        }
    }
}
