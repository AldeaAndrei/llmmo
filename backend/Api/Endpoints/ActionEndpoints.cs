using llmmo.Api;
using llmmo.Api.Dtos;
using llmmo.Auth;
using llmmo.Data;
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
