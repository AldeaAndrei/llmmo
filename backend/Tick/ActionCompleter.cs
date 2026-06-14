using System.Text.Json;
using llmmo.Api;
using llmmo.Api.Buildings;
using llmmo.Api.Troops;
using llmmo.Data;
using llmmo.Entities;
using Microsoft.EntityFrameworkCore;

namespace llmmo.Tick;

public class ActionCompleter
{
    private readonly AppDbContext _db;

    public ActionCompleter(AppDbContext db)
    {
        _db = db;
    }

    public async Task CompleteReadyActionsAsync(int currentTick, CancellationToken cancellationToken = default)
    {
        var actions = await _db.Actions
            .Include(action => action.City)
            .ThenInclude(city => city.Buildings)
            .Include(action => action.City)
            .ThenInclude(city => city.Troops)
            .Where(action =>
                action.Status == ActionStatus.InProgress
                && action.ReadyAtTick != null
                && action.ReadyAtTick <= currentTick)
            .ToListAsync(cancellationToken);

        foreach (var action in actions)
        {
            try
            {
                CompleteAction(action);
                action.Status = ActionStatus.Done;
            }
            catch
            {
                Refund(action);
                action.Status = ActionStatus.Failed;
            }
        }
    }

    private static void CompleteAction(GameAction action)
    {
        var payload = JsonSerializer.Deserialize<JsonElement>(action.Payload);

        if (action.Type.Equals("upgrade", StringComparison.OrdinalIgnoreCase))
        {
            CompleteUpgrade(action.City, payload);
            return;
        }

        if (action.Type.Equals("train", StringComparison.OrdinalIgnoreCase))
        {
            CompleteTrain(action.City, payload);
            return;
        }

        throw new InvalidOperationException($"Unsupported action type: {action.Type}");
    }

    private static void CompleteUpgrade(City city, JsonElement payload)
    {
        var buildingType = ActionPayloadHelper.GetBuildingType(payload);
        if (string.IsNullOrWhiteSpace(buildingType))
        {
            throw new InvalidOperationException("Upgrade payload missing buildingType.");
        }

        var building = city.Buildings.FirstOrDefault(b =>
            b.Type.Equals(buildingType, StringComparison.OrdinalIgnoreCase));

        if (building is null)
        {
            throw new InvalidOperationException("Upgrade target building not found.");
        }

        building.Level += 1;
    }

    private static void CompleteTrain(City city, JsonElement payload)
    {
        var count = ActionPayloadHelper.GetTrainCount(payload, defaultCount: 0);
        if (count <= 0)
        {
            throw new InvalidOperationException("Train payload missing valid count.");
        }

        var troopType = ActionPayloadHelper.GetTroopType(payload) ?? "soldier";
        var troop = city.Troops.FirstOrDefault(t =>
            t.Type.Equals(troopType, StringComparison.OrdinalIgnoreCase));

        if (troop is null)
        {
            troop = new CityTroop
            {
                Id = Guid.NewGuid(),
                CityId = city.Id,
                Type = troopType,
                Quantity = 0,
            };
            city.Troops.Add(troop);
        }

        troop.Quantity += count;
    }

    private static void Refund(GameAction action)
    {
        var payload = JsonSerializer.Deserialize<JsonElement>(action.Payload);
        var deducted = ActionPayloadHelper.GetDeducted(payload);
        if (deducted is null)
        {
            return;
        }

        CityResources.Refund(
            action.City,
            new BuildingUpgradeCost(deducted.Wood, deducted.Stone, deducted.Gold, deducted.Food));
    }
}
