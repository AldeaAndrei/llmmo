using llmmo.Api;
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
        group.MapGet("/actions/llm", ListLlmActions);
        group.MapGet("/actions", ListActions).RequireAuth();
        group.MapPost("/actions/train/preview", PreviewTrain).RequireAuth();
        group.MapPost("/actions", CreateAction).RequireAuth();
        return group;
    }

    private static async Task<IResult> PreviewTrain(
        TrainPreviewRequest request,
        HttpContext httpContext,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var auth = httpContext.GetPlayerAuth()!;

        var city = await db.Cities.AsNoTracking()
            .Include(c => c.Buildings)
            .FirstOrDefaultAsync(c => c.Id == request.CityId, cancellationToken);

        if (city is null)
        {
            return Results.NotFound(new { error = "City not found." });
        }

        if (city.PlayerId != auth.PlayerId)
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        var lines = request.Lines ?? [];
        var preview = TrainPreviewCalculator.Preview(city, lines);
        return Results.Ok(preview);
    }

    private static async Task<IResult> ListLlmActions(
        bool? include_done,
        int? limit,
        AppDbContext db,
        DiplomacyService diplomacy,
        CancellationToken cancellationToken)
    {
        var take = Math.Clamp(limit ?? 50, 1, 100);
        var includeDone = include_done ?? false;

        var query = db.Actions
            .AsNoTracking()
            .Include(action => action.Player)
            .Include(action => action.City)
            .Where(action => action.Player.PlayerType == PlayerType.Llm);

        if (!includeDone)
        {
            query = query.Where(action => action.Status != ActionStatus.Done);
        }

        var actions = await query
            .OrderByDescending(action => action.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

        var diplomacyFeed = await diplomacy.ListLlmDiplomacyFeedAsync(take, cancellationToken);

        var merged = actions
            .Select(ActionMapper.ToLlmUnifiedFeedDto)
            .Concat(diplomacyFeed)
            .OrderByDescending(item => item.SubmittedAtTick)
            .ThenByDescending(item => item.CreatedAt)
            .Take(take)
            .ToList();

        return Results.Ok(merged);
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
        ActionSubmissionService submissionService,
        CancellationToken cancellationToken)
    {
        var auth = httpContext.GetPlayerAuth()!;

        var (action, error) = await submissionService.SubmitAsync(
            auth.PlayerId,
            request,
            cancellationToken);

        if (error is not null)
        {
            if (error.Equals("Forbidden.", StringComparison.OrdinalIgnoreCase))
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }

            if (error.Equals("City not found.", StringComparison.OrdinalIgnoreCase))
            {
                return Results.NotFound(new { error });
            }

            return Results.BadRequest(new { error });
        }

        return Results.Created(
            $"/api/v1/actions/{action!.Id}",
            ActionMapper.ToCreatedDto(action));
    }
}
