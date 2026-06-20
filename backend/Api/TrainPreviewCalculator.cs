using llmmo.Api.Buildings;
using llmmo.Api.Dtos;
using llmmo.Api.Troops;
using llmmo.Entities;

namespace llmmo.Api;

public static class TrainPreviewCalculator
{
    private static readonly string[] TrainTroopTypes = ["soldier", "spy"];

    public static TrainPreviewDto Preview(City city, IReadOnlyList<TrainPreviewLineRequest> lines)
    {
        var errors = new List<string>();
        var barracks = city.Buildings.FirstOrDefault(building =>
            building.Type.Equals("barracks", StringComparison.OrdinalIgnoreCase));

        if (barracks is null)
        {
            return EmptyPreview(["Barracks not found in this city."]);
        }

        var maxCapacity = BuildingCatalog.TrainCapacityAtLevel(barracks.Level);
        var previewLines = new List<TrainPreviewLineDto>();
        var totalCost = new BuildingUpgradeCost(0, 0, 0, 0);
        var totalDuration = 0;
        var activeLines = 0;

        foreach (var troopType in TrainTroopTypes)
        {
            var requested = lines.FirstOrDefault(line =>
                line.Type.Equals(troopType, StringComparison.OrdinalIgnoreCase));
            var count = Math.Max(0, requested?.Count ?? 0);

            if (!TroopCatalog.IsTrainableAt(troopType, "barracks"))
            {
                continue;
            }

            var def = TroopCatalog.Get(troopType);
            var carryPerUnit = def.CapacityWood + def.CapacityStone + def.CapacityGold + def.CapacityFood;
            BuildingUpgradeCostDto lineCost;
            int durationTicks;

            if (count <= 0)
            {
                var unitCost = def.TrainCostPerUnit;
                lineCost = new BuildingUpgradeCostDto(unitCost.Wood, unitCost.Stone, unitCost.Gold, unitCost.Food);
                durationTicks = 0;
            }
            else
            {
                if (count > maxCapacity)
                {
                    errors.Add($"Cannot train more than {maxCapacity} {def.Name} at barracks level {barracks.Level}.");
                }

                var cost = TroopCatalog.TrainCostForCount(troopType, count);
                lineCost = new BuildingUpgradeCostDto(cost.Wood, cost.Stone, cost.Gold, cost.Food);
                durationTicks = BuildingCatalog.TrainDurationTicks(count, barracks.Level);
                totalCost = new BuildingUpgradeCost(
                    totalCost.Wood + cost.Wood,
                    totalCost.Stone + cost.Stone,
                    totalCost.Gold + cost.Gold,
                    totalCost.Food + cost.Food);
                totalDuration += durationTicks;
                activeLines++;
            }

            previewLines.Add(new TrainPreviewLineDto(
                def.Type,
                def.Name,
                count,
                def.AttackMelee,
                def.AttackRange,
                def.Speed,
                carryPerUnit,
                lineCost,
                durationTicks));
        }

        if (activeLines > 1)
        {
            errors.Add("Only one troop type can be trained per action.");
        }

        if (activeLines == 1 && !CityResources.CanAfford(city, totalCost))
        {
            errors.Add("Insufficient resources.");
        }

        return new TrainPreviewDto(
            errors.Count == 0 && activeLines == 1,
            errors,
            maxCapacity,
            totalDuration,
            new BuildingUpgradeCostDto(totalCost.Wood, totalCost.Stone, totalCost.Gold, totalCost.Food),
            previewLines);
    }

    private static TrainPreviewDto EmptyPreview(IReadOnlyList<string> errors) =>
        new(false, errors, 0, 0, new BuildingUpgradeCostDto(0, 0, 0, 0), []);
}
