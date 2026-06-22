using llmmo.Api.Buildings;
using llmmo.Api.Dtos;
using llmmo.Api.GameRules;
using llmmo.Api.Troops;
using llmmo.Entities;

namespace llmmo.Api;

public static class PossibleActionsCalculator
{
    private static readonly string[] TrainTroopTypes = ["soldier", "spy"];
    private const int TrainCount = 1;

    public static IReadOnlyList<PossibleUpgradeDto> GetAffordableUpgrades(
        City city,
        bool upgradeSlotAvailable)
    {
        if (!upgradeSlotAvailable)
        {
            return [];
        }

        var upgrades = new List<PossibleUpgradeDto>();

        foreach (var building in city.Buildings)
        {
            if (building.Level >= BuildingRules.MaxBuildingLevel)
            {
                continue;
            }

            var cost = BuildingCatalog.UpgradeCostForLevel(building.Type, building.Level + 1);
            if (!CityResources.CanAfford(city, cost))
            {
                continue;
            }

            upgrades.Add(new PossibleUpgradeDto(
                building.Type,
                building.Level,
                building.Level + 1,
                new BuildingUpgradeCostDto(cost.Wood, cost.Stone, cost.Gold, cost.Food)));
        }

        return upgrades
            .OrderBy(upgrade => upgrade.Cost.Food + upgrade.Cost.Gold + upgrade.Cost.Stone + upgrade.Cost.Wood)
            .ToList();
    }

    public static IReadOnlyList<PossibleTrainDto> GetAffordableTrain(
        City city,
        bool trainSlotAvailable)
    {
        if (!trainSlotAvailable)
        {
            return [];
        }

        var barracks = city.Buildings.FirstOrDefault(building =>
            building.Type.Equals("barracks", StringComparison.OrdinalIgnoreCase));

        if (barracks is null)
        {
            return [];
        }

        var capacity = BuildingCatalog.TrainCapacityAtLevel(barracks.Level);
        if (capacity < TrainCount)
        {
            return [];
        }

        var options = new List<PossibleTrainDto>();

        foreach (var troopType in TrainTroopTypes)
        {
            if (!TroopCatalog.IsTrainableAt(troopType, "barracks"))
            {
                continue;
            }

            var unitCost = TroopCatalog.TrainCostForCount(troopType, TrainCount);
            if (!CityResources.CanAfford(city, unitCost))
            {
                continue;
            }

            // Largest batch the city can both fit (barracks capacity) and afford.
            var maxCount = TrainCount;
            var maxCost = unitCost;
            for (var count = capacity; count > TrainCount; count--)
            {
                var cost = TroopCatalog.TrainCostForCount(troopType, count);
                if (CityResources.CanAfford(city, cost))
                {
                    maxCount = count;
                    maxCost = cost;
                    break;
                }
            }

            options.Add(new PossibleTrainDto(
                troopType,
                TrainCount,
                maxCount,
                new BuildingUpgradeCostDto(unitCost.Wood, unitCost.Stone, unitCost.Gold, unitCost.Food),
                new BuildingUpgradeCostDto(maxCost.Wood, maxCost.Stone, maxCost.Gold, maxCost.Food)));
        }

        return options;
    }

    public static IReadOnlyList<TroopStackEntryDto> GetTroopCounts(City city) =>
        city.Troops
            .Where(troop => troop.Quantity > 0)
            .OrderBy(troop => troop.Type, StringComparer.OrdinalIgnoreCase)
            .Select(troop => new TroopStackEntryDto(troop.Type, troop.Quantity))
            .ToList();
}
