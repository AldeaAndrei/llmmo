using llmmo.Api.Buildings;
using llmmo.Api.GameRules;

namespace BalanceSim.Core;

public static class SimActionGenerator
{
    private static readonly string[] TrainTroopTypes = ["soldier", "spy"];

    public static IReadOnlyList<CandidateAction> GetAllActions(
        SimCityState state,
        IReadOnlyList<DefenderFixture> fixtures)
    {
        var actions = new List<CandidateAction>();

        if (state.UpgradeSlotFree)
        {
            foreach (var type in BuildingRules.AllTypes.OrderBy(t => t, StringComparer.OrdinalIgnoreCase))
            {
                var level = state.GetBuildingLevel(type);
                if (level >= state.Profile.MaxBuildingLevel)
                {
                    continue;
                }

                var cost = state.BuildingsEngine.UpgradeCostForLevel(type, level + 1);
                if (state.CanAfford(cost))
                {
                    actions.Add(new CandidateAction(SimActionKind.Upgrade, BuildingType: type));
                }
            }
        }

        if (state.TrainSlotFree && state.ActiveMission is null)
        {
            var capacity = state.TrainCapacity;
            if (capacity > 0)
            {
                var batchSizes = GetBatchSizes(capacity);
                foreach (var troopType in TrainTroopTypes.OrderBy(t => t, StringComparer.OrdinalIgnoreCase))
                {
                    if (!state.TroopsEngine.IsTrainableAt(troopType, "barracks"))
                    {
                        continue;
                    }

                    foreach (var count in batchSizes.OrderBy(c => c))
                    {
                        var cost = state.TroopsEngine.TrainCostForCount(troopType, count, capacity);
                        if (state.CanAfford(cost))
                        {
                            actions.Add(new CandidateAction(SimActionKind.Train, TroopType: troopType, Count: count));
                        }
                    }
                }
            }
        }

        if (state.GetTroopCount("spy") > 0 && state.TrainSlotFree)
        {
            foreach (var fixture in fixtures.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase))
            {
                actions.Add(new CandidateAction(SimActionKind.Scout, FixtureName: fixture.Name));
            }
        }

        if (state.GetTroopCount("soldier") > 0 && state.ActiveMission is null && state.TrainSlotFree)
        {
            var soldiers = state.GetTroopCount("soldier");
            var attackerPower = soldiers * 3;
            foreach (var fixture in fixtures.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase))
            {
                // Only offer attacks the army can actually win. Sending a token force
                // that loses just churns soldiers and wastes mission time.
                var defenderPower = fixture.Soldiers * 3 + state.Profile.WallDefencePerLevel * fixture.WallLevel;
                if (attackerPower > defenderPower)
                {
                    actions.Add(new CandidateAction(SimActionKind.Attack, FixtureName: fixture.Name, Count: soldiers));
                }
            }
        }

        return actions;
    }

    public static int[] GetBatchSizes(int capacity)
    {
        if (capacity <= 1)
        {
            return [1];
        }

        var sizes = new HashSet<int>
        {
            1,
            Math.Max(1, capacity / 4),
            Math.Max(1, capacity / 2),
            capacity,
        };

        return sizes.OrderBy(s => s).ToArray();
    }
}
