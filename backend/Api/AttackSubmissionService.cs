using llmmo.Api.Dtos;
using llmmo.Api.Troops;
using llmmo.Data;
using llmmo.Entities;
using Microsoft.EntityFrameworkCore;

namespace llmmo.Api;

public class AttackSubmissionService
{
    private readonly AppDbContext _db;

    public AttackSubmissionService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AttackPreviewDto> PreviewAsync(
        Guid playerId,
        CreateAttackRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAsync(playerId, request, deductTroops: false, cancellationToken);
        return validation.Preview;
    }

    public async Task<(MilitaryAttack? Attack, string? Error)> CreateAsync(
        Guid playerId,
        CreateAttackRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAsync(playerId, request, deductTroops: true, cancellationToken);
        if (!validation.Preview.Valid)
        {
            return (null, validation.Preview.Errors.FirstOrDefault() ?? "Invalid attack request.");
        }

        if (validation.SourceCity is null || validation.WorldTick is null)
        {
            return (null, "Validation failed.");
        }

        var attack = validation.Attack!;
        _db.MilitaryAttacks.Add(attack);
        await _db.SaveChangesAsync(cancellationToken);

        return (attack, null);
    }

    private async Task<ValidationResult> ValidateAsync(
        Guid playerId,
        CreateAttackRequest request,
        bool deductTroops,
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();

        if (request.SourceCityId == Guid.Empty)
        {
            errors.Add("SourceCityId is required.");
        }

        var normalizedType = request.Type?.ToLowerInvariant() ?? "";
        if (normalizedType is not "attack" and not "scout")
        {
            errors.Add("Type must be attack or scout.");
        }

        var sourceCity = request.SourceCityId == Guid.Empty
            ? null
            : await _db.Cities
                .Include(c => c.Troops)
                .FirstOrDefaultAsync(c => c.Id == request.SourceCityId, cancellationToken);

        if (sourceCity is null)
        {
            errors.Add("Source city not found.");
            return ValidationResult.Invalid(errors);
        }

        if (sourceCity.PlayerId != playerId)
        {
            errors.Add("Forbidden.");
            return ValidationResult.Invalid(errors);
        }

        var troops = request.Troops?
            .Where(entry => entry.Count > 0)
            .Select(entry => new TroopStackEntry(entry.Type, entry.Count))
            .ToList() ?? [];

        if (troops.Count == 0)
        {
            errors.Add("At least one troop must be committed.");
        }

        City? targetCity = null;
        int targetX;
        int targetY;

        if (normalizedType == "attack")
        {
            if (request.TargetCityId is null || request.TargetCityId == Guid.Empty)
            {
                errors.Add("TargetCityId is required for attacks.");
            }
            else
            {
                targetCity = await _db.Cities.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == request.TargetCityId.Value, cancellationToken);

                if (targetCity is null)
                {
                    errors.Add("Target city not found.");
                }
                else if (targetCity.PlayerId == playerId)
                {
                    errors.Add("Cannot attack your own city.");
                }
            }

            if (!TroopStackHelper.HasCombatTroops(troops))
            {
                errors.Add("Attack requires at least one combat troop.");
            }
        }
        else
        {
            var total = TroopStackHelper.TotalCount(troops);
            if (total != 1 || troops.Count != 1 || !troops[0].Type.Equals("spy", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("Scout requires exactly one spy.");
            }
        }

        if (normalizedType == "scout")
        {
            targetX = request.TargetX ?? targetCity?.X ?? -1;
            targetY = request.TargetY ?? targetCity?.Y ?? -1;

            if (targetX < 0 || targetY < 0)
            {
                errors.Add("Target coordinates are required for scout.");
            }
        }
        else if (targetCity is not null)
        {
            targetX = targetCity.X;
            targetY = targetCity.Y;
        }
        else
        {
            targetX = request.TargetX ?? -1;
            targetY = request.TargetY ?? -1;
        }

        foreach (var entry in troops)
        {
            if (!TroopCatalog.IsValidType(entry.Type))
            {
                errors.Add($"Unknown troop type: {entry.Type}");
                continue;
            }

            var available = sourceCity.Troops
                .FirstOrDefault(t => t.Type.Equals(entry.Type, StringComparison.OrdinalIgnoreCase))
                ?.Quantity ?? 0;

            if (entry.Count > available)
            {
                errors.Add($"Insufficient {entry.Type}: have {available}, need {entry.Count}.");
            }
        }

        var worldState = await _db.WorldState.AsNoTracking()
            .FirstOrDefaultAsync(state => state.Id == 1, cancellationToken);

        if (worldState is null)
        {
            errors.Add("World state is not initialized.");
        }

        var manhattan = MapDistance.Manhattan(sourceCity.X, sourceCity.Y, targetX, targetY);
        var partySpeed = TroopStackHelper.PartySpeed(troops);
        var outboundTicks = MapDistance.TravelTicks(manhattan, partySpeed);
        var returnTicks = outboundTicks;
        var currentTick = worldState?.CurrentTick ?? 0;
        var arrivesAtTick = currentTick + outboundTicks;
        var returnsAtTick = arrivesAtTick + returnTicks;

        var preview = new AttackPreviewDto(
            errors.Count == 0,
            errors,
            manhattan,
            partySpeed,
            outboundTicks,
            returnTicks,
            arrivesAtTick,
            returnsAtTick);

        if (errors.Count > 0 || worldState is null)
        {
            return new ValidationResult(preview, sourceCity, worldState?.CurrentTick, null);
        }

        if (deductTroops)
        {
            DeductTroops(sourceCity, troops);
        }

        var attack = new MilitaryAttack
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            SourceCityId = sourceCity.Id,
            TargetCityId = targetCity?.Id,
            TargetX = targetX,
            TargetY = targetY,
            Type = normalizedType,
            Status = "outbound",
            Troops = TroopStackHelper.Serialize(troops),
            OutboundDurationTicks = outboundTicks,
            ReturnDurationTicks = returnTicks,
            DepartedAtTick = currentTick,
            ArrivesAtTick = arrivesAtTick,
            ReturnsAtTick = null,
        };

        return new ValidationResult(preview, sourceCity, currentTick, attack);
    }

    private static void DeductTroops(City sourceCity, IReadOnlyList<TroopStackEntry> troops)
    {
        foreach (var entry in troops)
        {
            var troop = sourceCity.Troops.First(t =>
                t.Type.Equals(entry.Type, StringComparison.OrdinalIgnoreCase));
            troop.Quantity -= entry.Count;
        }
    }

    private record ValidationResult(
        AttackPreviewDto Preview,
        City? SourceCity,
        int? WorldTick,
        MilitaryAttack? Attack)
    {
        public static ValidationResult Invalid(IReadOnlyList<string> errors) =>
            new(new AttackPreviewDto(false, errors, 0, 0, 0, 0, 0, 0), null, null, null);
    }
}
