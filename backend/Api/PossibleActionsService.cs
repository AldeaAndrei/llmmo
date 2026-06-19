using llmmo.Api.Dtos;
using llmmo.Api.Troops;
using llmmo.Data;
using llmmo.Entities;
using Microsoft.EntityFrameworkCore;

namespace llmmo.Api;

public class PossibleActionsService
{
    private const int MaxAttackTargets = 12;

    private readonly AppDbContext _db;
    private readonly AttackSubmissionService _attacks;

    public PossibleActionsService(AppDbContext db, AttackSubmissionService attacks)
    {
        _db = db;
        _attacks = attacks;
    }

    public async Task<(PossibleActionsDto? Result, string? Error)> GetForCityAsync(
        Guid playerId,
        Guid cityId,
        CancellationToken cancellationToken = default)
    {
        var city = await _db.Cities
            .Include(c => c.Buildings)
            .Include(c => c.Troops)
            .FirstOrDefaultAsync(c => c.Id == cityId, cancellationToken);

        if (city is null)
        {
            return (null, "City not found.");
        }

        if (city.PlayerId != playerId)
        {
            return (null, "Forbidden.");
        }

        var worldState = await _db.WorldState.AsNoTracking()
            .FirstOrDefaultAsync(state => state.Id == 1, cancellationToken);

        if (worldState is null)
        {
            return (null, "World state is not initialized.");
        }

        var inProgressTypes = await _db.Actions
            .AsNoTracking()
            .Where(action => action.CityId == cityId && action.Status == ActionStatus.InProgress)
            .Select(action => action.Type)
            .ToListAsync(cancellationToken);

        var upgradeSlotAvailable = !inProgressTypes.Any(ActionDurations.IsUpgradeSlotType);
        var trainSlotAvailable = !inProgressTypes.Any(ActionDurations.IsTrainSlotType);

        var production = CityResourceCalculator.CalculateProduction(city);
        var foodUpkeep = CityResourceCalculator.CalculateFoodUpkeep(city);

        var upgrades = PossibleActionsCalculator.GetAffordableUpgrades(city, upgradeSlotAvailable);
        var train = PossibleActionsCalculator.GetAffordableTrain(city, trainSlotAvailable);
        var attacks = await GetPossibleAttacksAsync(playerId, city, cancellationToken);

        return (new PossibleActionsDto(
            worldState.CurrentTick,
            city.Id,
            new CityResourcesDto(city.Wood, city.Stone, city.Gold, city.Food),
            production.Food,
            foodUpkeep,
            PossibleActionsCalculator.GetTroopCounts(city),
            upgrades,
            train,
            attacks), null);
    }

    private async Task<IReadOnlyList<PossibleAttackDto>> GetPossibleAttacksAsync(
        Guid playerId,
        City sourceCity,
        CancellationToken cancellationToken)
    {
        var soldierCount = sourceCity.Troops
            .FirstOrDefault(troop => troop.Type.Equals("soldier", StringComparison.OrdinalIgnoreCase))
            ?.Quantity ?? 0;

        if (soldierCount < 1)
        {
            return [];
        }

        var targets = await _db.Cities.AsNoTracking()
            .Where(city => city.PlayerId != playerId)
            .OrderBy(city => Math.Abs(city.X - sourceCity.X) + Math.Abs(city.Y - sourceCity.Y))
            .Take(MaxAttackTargets)
            .ToListAsync(cancellationToken);

        var attacks = new List<PossibleAttackDto>();
        var troops = new List<TroopStackEntryDto> { new("soldier", 1) };

        foreach (var target in targets)
        {
            var preview = await _attacks.PreviewAsync(
                playerId,
                new CreateAttackRequest(
                    sourceCity.Id,
                    target.Id,
                    null,
                    null,
                    "attack",
                    troops),
                cancellationToken);

            if (!preview.Valid)
            {
                continue;
            }

            attacks.Add(new PossibleAttackDto(
                target.Id,
                target.Name,
                target.X,
                target.Y,
                troops));
        }

        return attacks;
    }
}
