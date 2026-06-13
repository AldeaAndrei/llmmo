using System.Text.Json;
using llmmo.Api.Dtos;
using llmmo.Auth;
using llmmo.Data;
using llmmo.Entities;
using Microsoft.EntityFrameworkCore;

namespace llmmo.Api.Endpoints;

public static class ActionEndpoints
{
    public static RouteGroupBuilder MapActionEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/actions", ListActions).RequireAuth();
        group.MapPost("/actions", CreateAction).RequireAuth();
        return group;
    }

    private static async Task<IResult> ListActions(
        Guid? city_id,
        HttpContext httpContext,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var auth = httpContext.GetPlayerAuth()!;

        if (city_id is null || city_id == Guid.Empty)
        {
            return Results.BadRequest(new { error = "city_id query parameter is required." });
        }

        var city = await db.Cities.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == city_id.Value, cancellationToken);

        if (city is null)
        {
            return Results.NotFound(new { error = "City not found." });
        }

        if (city.PlayerId != auth.PlayerId)
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
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
        HttpContext httpContext,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var auth = httpContext.GetPlayerAuth()!;

        if (request.CityId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "CityId is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Type))
        {
            return Results.BadRequest(new { error = "Type is required." });
        }

        if (!ActionDurations.IsAllowedType(request.Type))
        {
            return Results.BadRequest(new { error = "Type must be one of: build, train, attack, scout." });
        }

        var city = await db.Cities.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CityId, cancellationToken);

        if (city is null)
        {
            return Results.NotFound(new { error = "City not found." });
        }

        if (city.PlayerId != auth.PlayerId)
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
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
            PlayerId = auth.PlayerId,
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
