using llmmo.Api.Dtos;
using llmmo.Data;
using Microsoft.EntityFrameworkCore;

namespace llmmo.Api.Endpoints;

public static class CityEndpoints
{
    public static RouteGroupBuilder MapCityEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/cities", ListCities);
        group.MapGet("/cities/{cityId:guid}", GetCity);
        return group;
    }

    private static async Task<IResult> ListCities(
        Guid? player_id,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        // TODO: auth — resolve playerId from session; reject unauthenticated

        if (player_id is null || player_id == Guid.Empty)
        {
            return Results.BadRequest(new { error = "player_id query parameter is required." });
        }

        var cities = await db.Cities
            .AsNoTracking()
            .Where(city => city.PlayerId == player_id.Value)
            .OrderBy(city => city.CreatedAt)
            .ToListAsync(cancellationToken);

        return Results.Ok(cities.Select(CityMapper.ToFullDto));
    }

    private static async Task<IResult> GetCity(
        Guid cityId,
        Guid? player_id,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        // TODO: auth — resolve playerId from session; reject unauthenticated

        var city = await db.Cities.AsNoTracking().FirstOrDefaultAsync(c => c.Id == cityId, cancellationToken);
        if (city is null)
        {
            return Results.NotFound(new { error = "City not found." });
        }

        if (player_id is not null && player_id != Guid.Empty && city.PlayerId != player_id.Value)
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        return Results.Ok(CityMapper.ToFullDto(city));
    }
}
