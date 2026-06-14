using System.Text.Json;
using llmmo.Api.Buildings;
using llmmo.Api.Dtos;
using llmmo.Api.Troops;
using llmmo.Data;
using llmmo.Entities;
using Microsoft.EntityFrameworkCore;

namespace llmmo.Api;

public class ActionSubmissionService
{
    private readonly AppDbContext _db;

    public ActionSubmissionService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(GameAction? Action, string? Error)> SubmitAsync(
        Guid playerId,
        CreateActionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.CityId == Guid.Empty)
        {
            return (null, "CityId is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Type))
        {
            return (null, "Type is required.");
        }

        var normalizedType = request.Type.ToLowerInvariant();

        if (!ActionDurations.IsAllowedType(normalizedType))
        {
            return (null, "Type must be one of: upgrade, train.");
        }

        var city = await _db.Cities
            .Include(c => c.Buildings)
            .Include(c => c.Troops)
            .FirstOrDefaultAsync(c => c.Id == request.CityId, cancellationToken);

        if (city is null)
        {
            return (null, "City not found.");
        }

        if (city.PlayerId != playerId)
        {
            return (null, "Forbidden.");
        }

        var slotError = await ValidateSlotAvailableAsync(city.Id, normalizedType, cancellationToken);
        if (slotError is not null)
        {
            return (null, slotError);
        }

        var worldState = await _db.WorldState.FirstOrDefaultAsync(state => state.Id == 1, cancellationToken);
        if (worldState is null)
        {
            return (null, "World state is not initialized.");
        }

        var payloadElement = ActionPayloadHelper.ToJsonElement(request.Payload);
        var buildingType = ActionPayloadHelper.GetBuildingType(payloadElement);

        BuildingUpgradeCost cost;
        string payloadJson;

        if (ActionDurations.IsUpgradeSlotType(normalizedType))
        {
            var upgradeResult = ValidateUpgrade(city, buildingType, out cost, out payloadJson);
            if (upgradeResult is not null)
            {
                return (null, upgradeResult);
            }
        }
        else
        {
            var trainResult = ValidateTrain(city, buildingType, payloadElement, out cost, out payloadJson);
            if (trainResult is not null)
            {
                return (null, trainResult);
            }
        }

        if (!CityResources.CanAfford(city, cost))
        {
            return (null, "Insufficient resources.");
        }

        CityResources.Deduct(city, cost);

        var durationTicks = ActionDurations.GetDurationTicks(normalizedType);
        var action = new GameAction
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            CityId = city.Id,
            Type = normalizedType,
            Payload = payloadJson,
            Status = ActionStatus.InProgress,
            SubmittedAtTick = worldState.CurrentTick,
            ReadyAtTick = worldState.CurrentTick + durationTicks,
            DurationTicks = durationTicks,
        };

        _db.Actions.Add(action);
        await _db.SaveChangesAsync(cancellationToken);

        return (action, null);
    }

    private async Task<string?> ValidateSlotAvailableAsync(
        Guid cityId,
        string normalizedType,
        CancellationToken cancellationToken)
    {
        var inProgressTypes = await _db.Actions
            .AsNoTracking()
            .Where(action => action.CityId == cityId && action.Status == ActionStatus.InProgress)
            .Select(action => action.Type)
            .ToListAsync(cancellationToken);

        if (ActionDurations.IsUpgradeSlotType(normalizedType)
            && inProgressTypes.Any(type => ActionDurations.IsUpgradeSlotType(type)))
        {
            return "An upgrade is already in progress for this city.";
        }

        if (ActionDurations.IsTrainSlotType(normalizedType)
            && inProgressTypes.Any(type => ActionDurations.IsTrainSlotType(type)))
        {
            return "Training is already in progress for this city.";
        }

        return null;
    }

    private static string? ValidateUpgrade(
        City city,
        string? buildingType,
        out BuildingUpgradeCost cost,
        out string payloadJson)
    {
        cost = new BuildingUpgradeCost(0, 0, 0, 0);
        payloadJson = "{}";

        if (string.IsNullOrWhiteSpace(buildingType) || !BuildingCatalog.IsValidType(buildingType))
        {
            return "Payload buildingType is required and must be a valid building type.";
        }

        var building = city.Buildings.FirstOrDefault(b =>
            b.Type.Equals(buildingType, StringComparison.OrdinalIgnoreCase));

        if (building is null)
        {
            return "Building not found in this city.";
        }

        cost = BuildingCatalog.UpgradeCostForLevel(building.Type, building.Level + 1);
        payloadJson = ActionPayloadHelper.SerializePayload(new
        {
            buildingType = building.Type,
            deducted = new { cost.Wood, cost.Stone, cost.Gold, cost.Food },
        });

        return null;
    }

    private static string? ValidateTrain(
        City city,
        string? buildingType,
        JsonElement payloadElement,
        out BuildingUpgradeCost cost,
        out string payloadJson)
    {
        cost = new BuildingUpgradeCost(0, 0, 0, 0);
        payloadJson = "{}";

        if (!string.Equals(buildingType, "barracks", StringComparison.OrdinalIgnoreCase))
        {
            return "Training must target the barracks building.";
        }

        var barracks = city.Buildings.FirstOrDefault(b =>
            b.Type.Equals("barracks", StringComparison.OrdinalIgnoreCase));

        if (barracks is null)
        {
            return "Barracks not found in this city.";
        }

        var troopType = ActionPayloadHelper.GetTroopType(payloadElement) ?? "soldier";
        if (!TroopCatalog.IsValidType(troopType))
        {
            return "Invalid troopType.";
        }

        if (!TroopCatalog.IsTrainableAt(troopType, "barracks"))
        {
            return $"Troop type {troopType} cannot be trained at barracks.";
        }

        var count = ActionPayloadHelper.GetTrainCount(payloadElement);
        if (count <= 0)
        {
            return "Train count must be greater than zero.";
        }

        var capacity = BuildingCatalog.TrainCapacityAtLevel(barracks.Level);
        if (count > capacity)
        {
            return $"Cannot train more than {capacity} troops at barracks level {barracks.Level}.";
        }

        cost = TroopCatalog.TrainCostForCount(troopType, count);
        payloadJson = ActionPayloadHelper.SerializePayload(new
        {
            buildingType = "barracks",
            troopType,
            count,
            deducted = new { cost.Wood, cost.Stone, cost.Gold, cost.Food },
        });

        return null;
    }
}
