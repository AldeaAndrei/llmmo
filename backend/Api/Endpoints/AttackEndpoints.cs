using llmmo.Api.Dtos;
using llmmo.Auth;
using llmmo.Data;
using Microsoft.EntityFrameworkCore;

namespace llmmo.Api.Endpoints;

public static class AttackEndpoints
{
    public static RouteGroupBuilder MapAttackEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/attacks/movements", ListMovements).RequireAuth();
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

    private static async Task<IResult> ListMovements(
        Guid? city_id,
        HttpContext httpContext,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var auth = httpContext.GetPlayerAuth()!;

        var worldState = await db.WorldState.AsNoTracking()
            .FirstOrDefaultAsync(state => state.Id == 1, cancellationToken);

        if (worldState is null)
        {
            return Results.Problem("World state is not initialized.", statusCode: StatusCodes.Status500InternalServerError);
        }

        var myCityIds = await db.Cities.AsNoTracking()
            .Where(city => city.PlayerId == auth.PlayerId)
            .Select(city => city.Id)
            .ToListAsync(cancellationToken);

        if (myCityIds.Count == 0)
        {
            return Results.Ok(new TroopMovementsDto([], []));
        }

        var attacks = await db.MilitaryAttacks
            .AsNoTracking()
            .Include(attack => attack.SourceCity)
            .Where(attack => attack.Status == "outbound" || attack.Status == "returning")
            .ToListAsync(cancellationToken);

        var outgoing = new List<AttackMovementDto>();
        var incoming = new List<AttackMovementDto>();

        foreach (var attack in attacks)
        {
            var isOutgoing = attack.PlayerId == auth.PlayerId
                && myCityIds.Contains(attack.SourceCityId);
            var isIncoming = attack.TargetCityId.HasValue
                && myCityIds.Contains(attack.TargetCityId.Value)
                && attack.PlayerId != auth.PlayerId;

            if (city_id is not null && city_id != Guid.Empty)
            {
                if (isOutgoing && attack.SourceCityId != city_id.Value)
                {
                    isOutgoing = false;
                }

                if (isIncoming && attack.TargetCityId != city_id.Value)
                {
                    isIncoming = false;
                }
            }

            if (isOutgoing)
            {
                outgoing.Add(AttackMapper.ToMovementDto(
                    attack,
                    attack.SourceCity,
                    "outgoing",
                    worldState.CurrentTick));
            }

            if (isIncoming)
            {
                incoming.Add(AttackMapper.ToMovementDto(
                    attack,
                    attack.SourceCity,
                    "incoming",
                    worldState.CurrentTick));
            }
        }

        outgoing.Sort((left, right) => left.RemainingTicks.CompareTo(right.RemainingTicks));
        incoming.Sort((left, right) => left.RemainingTicks.CompareTo(right.RemainingTicks));

        return Results.Ok(new TroopMovementsDto(outgoing, incoming));
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
