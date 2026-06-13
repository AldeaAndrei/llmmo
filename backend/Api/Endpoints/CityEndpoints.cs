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
            .Where(city => city.PlayerId == auth.PlayerId)
            .OrderBy(city => city.CreatedAt)
            .ToListAsync(cancellationToken);

        return Results.Ok(cities.Select(city => CityMapper.ToFullDto(city)));
    }

    private static async Task<IResult> GetCity(
        Guid cityId,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var city = await db.Cities.AsNoTracking().FirstOrDefaultAsync(c => c.Id == cityId, cancellationToken);
        if (city is null)
        {
            return Results.NotFound(new { error = "City not found." });
        }

        return Results.Ok(CityMapper.ToPublicDto(city));
    }
}
