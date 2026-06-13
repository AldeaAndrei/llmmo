using System.Text.Json;
using llmmo.Api.Dtos;
using llmmo.Data;
using llmmo.Entities;
using Microsoft.EntityFrameworkCore;

namespace llmmo.Api.Endpoints;

public static class ActionEndpoints
{
    public static RouteGroupBuilder MapActionEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/actions", ListActions);
        group.MapPost("/actions", CreateAction);
        return group;
    }

    private static async Task<IResult> ListActions(
        Guid? city_id,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        // TODO: auth — resolve playerId from session; reject unauthenticated

        if (city_id is null || city_id == Guid.Empty)
        {
            return Results.BadRequest(new { error = "city_id query parameter is required." });
        }

        var cityExists = await db.Cities.AnyAsync(city => city.Id == city_id.Value, cancellationToken);
        if (!cityExists)
        {
            return Results.NotFound(new { error = "City not found." });
        }

        var actions = await db.Actions
            .AsNoTracking()
            .Where(action => action.CityId == city_id.Value)
            .OrderByDescending(action => action.CreatedAt)
            .ToListAsync(cancellationToken);

        return Results.Ok(actions.Select(ActionMapper.ToListDto));
    }

    private static async Task<IResult> CreateAction(
        CreateActionRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        // TODO: auth — resolve playerId from session; reject unauthenticated
        // TODO: auth — verify city.player_id == authenticated playerId

        if (request.PlayerId == Guid.Empty || request.CityId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "PlayerId and CityId are required." });
        }

        if (string.IsNullOrWhiteSpace(request.Type))
        {
            return Results.BadRequest(new { error = "Type is required." });
        }

        if (!ActionDurations.IsAllowedType(request.Type))
        {
            return Results.BadRequest(new { error = "Type must be one of: build, train, attack, scout." });
        }

        var cityExists = await db.Cities.AnyAsync(city => city.Id == request.CityId, cancellationToken);
        if (!cityExists)
        {
            return Results.NotFound(new { error = "City not found." });
        }

        var playerExists = await db.Players.AnyAsync(player => player.Id == request.PlayerId, cancellationToken);
        if (!playerExists)
        {
            return Results.NotFound(new { error = "Player not found." });
        }

        var worldState = await db.WorldState.AsNoTracking().FirstOrDefaultAsync(state => state.Id == 1, cancellationToken);
        if (worldState is null)
        {
            return Results.Problem("World state is not initialized.", statusCode: StatusCodes.Status500InternalServerError);
        }

        var payloadJson = JsonSerializer.Serialize(request.Payload);
        var normalizedType = request.Type.ToLowerInvariant();

        var action = new GameAction
        {
            Id = Guid.NewGuid(),
            PlayerId = request.PlayerId,
            CityId = request.CityId,
            Type = normalizedType,
            Payload = payloadJson,
            Status = ActionStatus.Queued,
            SubmittedAtTick = worldState.CurrentTick,
            ReadyAtTick = null,
            DurationTicks = ActionDurations.GetDurationTicks(normalizedType),
        };

        db.Actions.Add(action);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/api/v1/actions/{action.Id}",
            ActionMapper.ToCreatedDto(action));
    }
}
