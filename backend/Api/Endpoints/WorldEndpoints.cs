using llmmo.Api.Dtos;
using llmmo.Data;
using llmmo.Tick;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace llmmo.Api.Endpoints;

public static class WorldEndpoints
{
    public static RouteGroupBuilder MapWorldEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/world", GetWorld);
        return group;
    }

    private static async Task<IResult> GetWorld(
        AppDbContext db,
        IOptions<TickOptions> tickOptions,
        CancellationToken cancellationToken)
    {
        var worldState = await db.WorldState.AsNoTracking()
            .FirstOrDefaultAsync(state => state.Id == 1, cancellationToken);

        if (worldState is null)
        {
            return Results.Problem("World state is not initialized.", statusCode: StatusCodes.Status500InternalServerError);
        }

        var intervalSeconds = Math.Max(1, tickOptions.Value.TickIntervalSeconds);
        var interval = TimeSpan.FromSeconds(intervalSeconds);
        var nextTickAt = worldState.LastTickAt.Add(interval);

        return Results.Ok(new WorldDto(
            worldState.CurrentTick,
            intervalSeconds,
            worldState.LastTickAt,
            nextTickAt));
    }
}
