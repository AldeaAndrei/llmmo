using llmmo.Api.Dtos;
using llmmo.Auth;
using llmmo.Data;
using Microsoft.EntityFrameworkCore;

namespace llmmo.Api.Endpoints;

public static class CityEndpoints
{
    public static RouteGroupBuilder MapCityEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/cities/me", GetMyCities).RequireAuth();
        group.MapGet("/cities/{cityId:guid}", GetCity);
        return group;
    }

    private static async Task<IResult> GetMyCities(
        HttpContext httpContext,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var auth = httpContext.GetPlayerAuth()!;

        var cities = await db.Cities
            .AsNoTracking()
            .Include(city => city.Buildings)
            .Include(city => city.Troops)
            .Where(city => city.PlayerId == auth.PlayerId)
            .OrderBy(city => city.CreatedAt)
            .ToListAsync(cancellationToken);

        return Results.Ok(cities.Select(city => CityMapper.ToFullDto(city)));
    }

    private static async Task<IResult> GetCity(
        Guid cityId,
        HttpContext httpContext,
        AppDbContext db,
        IntelService intelService,
        CancellationToken cancellationToken)
    {
        var city = await db.Cities.AsNoTracking()
            .Include(c => c.Troops)
            .FirstOrDefaultAsync(c => c.Id == cityId, cancellationToken);

        if (city is null)
        {
            return Results.NotFound(new { error = "City not found." });
        }

        var auth = httpContext.GetPlayerAuth();
        var isOwnCity = auth?.IsAuthenticated == true && city.PlayerId == auth.PlayerId;

        if (isOwnCity)
        {
            return Results.Ok(CityMapper.ToVisibilityDto(
                city,
                "own",
                new CityResourcesDto(city.Wood, city.Stone, city.Gold, city.Food),
                city.Troops
                    .Where(t => t.Quantity > 0)
                    .Select(t => new TroopStackEntryDto(t.Type, t.Quantity))
                    .ToList()));
        }

        if (auth?.IsAuthenticated == true)
        {
            var hasIntel = await intelService.HasScoutIntelAsync(auth.PlayerId, cityId, cancellationToken);
            if (hasIntel)
            {
                return Results.Ok(CityMapper.ToVisibilityDto(
                    city,
                    "scouted",
                    new CityResourcesDto(city.Wood, city.Stone, city.Gold, city.Food),
                    city.Troops
                        .Where(t => t.Quantity > 0)
                        .Select(t => new TroopStackEntryDto(t.Type, t.Quantity))
                        .ToList()));
            }
        }

        return Results.Ok(CityMapper.ToVisibilityDto(city, "public", null, null));
    }
}
