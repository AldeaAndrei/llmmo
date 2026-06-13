using llmmo.Data;
using Microsoft.EntityFrameworkCore;

namespace llmmo.Api.Endpoints;

public static class MapEndpoints
{
    public static RouteGroupBuilder MapMapEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/map", GetMap);
        return group;
    }

    private static async Task<IResult> GetMap(
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        // TODO: auth — resolve playerId from session; reject unauthenticated

        var cities = await db.Cities
            .AsNoTracking()
            .OrderBy(city => city.Y)
            .ThenBy(city => city.X)
            .ToListAsync(cancellationToken);

        return Results.Ok(cities.Select(CityMapper.ToMapDto));
    }
}
