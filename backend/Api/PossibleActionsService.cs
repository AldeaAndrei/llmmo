using llmmo.Api.Dtos;
using llmmo.Api.Troops;
using llmmo.Data;
using llmmo.Entities;
using Microsoft.EntityFrameworkCore;

namespace llmmo.Api;

public class PossibleActionsService
{
    private readonly AppDbContext _db;
    private readonly AttackSubmissionService _attacks;
    private readonly DiplomacyService _diplomacy;

    private static readonly IReadOnlyList<TroopStackEntryDto> AttackTroops =
        [new("soldier", 1)];

    private static readonly IReadOnlyList<TroopStackEntryDto> ScoutTroops =
        [new("spy", 1)];

    public PossibleActionsService(
        AppDbContext db,
        AttackSubmissionService attacks,
        DiplomacyService diplomacy)
    {
        _db = db;
        _attacks = attacks;
        _diplomacy = diplomacy;
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
        var targets = await GetPossibleTargetsAsync(playerId, city, cancellationToken);
        var diplomacy = await _diplomacy.BuildPossibleDiplomacyAsync(playerId, cancellationToken);

        return (new PossibleActionsDto(
            worldState.CurrentTick,
            city.Id,
            new CityResourcesDto(city.Wood, city.Stone, city.Gold, city.Food),
            production.Food,
            foodUpkeep,
            PossibleActionsCalculator.GetTroopCounts(city),
            upgrades,
            train,
            targets,
            diplomacy), null);
    }

    private async Task<IReadOnlyList<PossibleTargetDto>> GetPossibleTargetsAsync(
        Guid playerId,
        City sourceCity,
        CancellationToken cancellationToken)
    {
        var soldierCount = sourceCity.Troops
            .FirstOrDefault(troop => troop.Type.Equals("soldier", StringComparison.OrdinalIgnoreCase))
            ?.Quantity ?? 0;

        var spyCount = sourceCity.Troops
            .FirstOrDefault(troop => troop.Type.Equals("spy", StringComparison.OrdinalIgnoreCase))
            ?.Quantity ?? 0;

        var enemyCities = await _db.Cities.AsNoTracking()
            .Include(target => target.Player)
            .Where(target => target.PlayerId != playerId)
            .OrderBy(target => Math.Abs(target.X - sourceCity.X) + Math.Abs(target.Y - sourceCity.Y))
            .ToListAsync(cancellationToken);

        var targets = new List<PossibleTargetDto>();
        var attackTroopEntries = new List<TroopStackEntry> { new("soldier", 1) };
        var scoutTroopEntries = new List<TroopStackEntry> { new("spy", 1) };
        var soldierSpeed = TroopStackHelper.PartySpeed(attackTroopEntries);

        foreach (var target in enemyCities)
        {
            var distance = MapDistance.Manhattan(sourceCity.X, sourceCity.Y, target.X, target.Y);
            var travelTicks = MapDistance.TravelTicks(distance, soldierSpeed);

            var canAttack = false;
            if (soldierCount >= 1)
            {
                var attackPreview = await _attacks.PreviewAsync(
                    playerId,
                    new CreateAttackRequest(
                        sourceCity.Id,
                        target.Id,
                        null,
                        null,
                        "attack",
                        AttackTroops),
                    cancellationToken);

                canAttack = attackPreview.Valid;
            }

            var canScout = false;
            if (spyCount >= 1)
            {
                var scoutPreview = await _attacks.PreviewAsync(
                    playerId,
                    new CreateAttackRequest(
                        sourceCity.Id,
                        target.Id,
                        target.X,
                        target.Y,
                        "scout",
                        ScoutTroops),
                    cancellationToken);

                canScout = scoutPreview.Valid;
            }

            targets.Add(new PossibleTargetDto(
                target.Id,
                target.PlayerId,
                target.Player.Name,
                distance,
                travelTicks,
                canAttack,
                canScout));
        }

        return targets;
    }
}
