using llmmo.Api.Dtos;
using llmmo.Auth;
using llmmo.Data;
using Microsoft.EntityFrameworkCore;

namespace llmmo.Api.Endpoints;

public static class AttackEndpoints
{
    public static RouteGroupBuilder MapAttackEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/attacks", ListAttacks);
        group.MapPost("/attacks/preview", PreviewAttack).RequireAuth();
        group.MapPost("/attacks", CreateAttack).RequireAuth();
        return group;
    }

    private static async Task<IResult> ListAttacks(
        Guid? city_id,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var worldState = await db.WorldState.AsNoTracking()
            .FirstOrDefaultAsync(state => state.Id == 1, cancellationToken);

        if (worldState is null)
        {
            return Results.Problem("World state is not initialized.", statusCode: StatusCodes.Status500InternalServerError);
        }

        var query = db.MilitaryAttacks
            .AsNoTracking()
            .Include(attack => attack.SourceCity)
            .Where(attack => attack.Status == "outbound" || attack.Status == "returning");

        if (city_id is not null && city_id != Guid.Empty)
        {
            query = query.Where(attack =>
                attack.SourceCityId == city_id.Value || attack.TargetCityId == city_id.Value);
        }

        var attacks = await query.ToListAsync(cancellationToken);

        var dtos = attacks
            .Select(attack => AttackMapper.ToMapDto(attack, attack.SourceCity, worldState.CurrentTick))
            .ToList();

        return Results.Ok(dtos);
    }

    private static async Task<IResult> PreviewAttack(
        CreateAttackRequest request,
        HttpContext httpContext,
        AttackSubmissionService attacks,
        CancellationToken cancellationToken)
    {
        var auth = httpContext.GetPlayerAuth()!;
        var preview = await attacks.PreviewAsync(auth.PlayerId, request, cancellationToken);
        return Results.Ok(preview);
    }

    private static async Task<IResult> CreateAttack(
        CreateAttackRequest request,
        HttpContext httpContext,
        AttackSubmissionService attacks,
        CancellationToken cancellationToken)
    {
        var auth = httpContext.GetPlayerAuth()!;
        var (attack, error) = await attacks.CreateAsync(auth.PlayerId, request, cancellationToken);

        if (error is not null)
        {
            return Results.BadRequest(new { error });
        }

        return Results.Created($"/api/v1/attacks/{attack!.Id}", AttackMapper.ToCreatedDto(attack));
    }
}
