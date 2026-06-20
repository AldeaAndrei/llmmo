using BalanceSim.Core;
using BalanceSim.Metrics;
using llmmo.Api.Buildings;
using llmmo.Api.GameRules;

namespace BalanceSim.Agents;

public sealed class ActionScorer : IActionScorer
{
    private readonly CompositeWealthCalculator _wealth = new();

    public ActionScore Score(SimCityState state, CandidateAction action, IReadOnlyList<DefenderFixture> fixtures)
    {
        return action.Kind switch
        {
            SimActionKind.Upgrade when action.BuildingType is not null =>
                ScoreUpgrade(state, action.BuildingType),
            SimActionKind.Train when action.TroopType is not null =>
                ScoreTrain(state, action.TroopType, action.Count),
            SimActionKind.Attack when action.FixtureName is not null =>
                ScoreAttack(state, action, fixtures),
            SimActionKind.Scout =>
                new ActionScore(100, 0.01, 0, 0, 0),
            _ => new ActionScore(0, 0, 0, 0, 0),
        };
    }

    private ActionScore ScoreUpgrade(SimCityState state, string buildingType)
    {
        var level = state.GetBuildingLevel(buildingType);
        var cost = state.BuildingsEngine.UpgradeCostForLevel(buildingType, level + 1);
        var normalizedCost = NormalizeCost(state, cost);

        var prodGain = EstimateProductionGain(state, buildingType, level);
        var payback = prodGain > 0 ? normalizedCost / prodGain : normalizedCost * 100;

        // WealthDelta drives *balanced* growth: upgrading the lowest-level buildings
        // first keeps every building type progressing together, which is what the
        // wealth metric (sunk capital across all 8 buildings) rewards and what
        // produces a smooth rising curve instead of a single-building plateau.
        var maxLevel = Math.Max(2, state.Profile.MaxBuildingLevel);
        var levelGap = (double)(maxLevel - level) / (maxLevel - 1);

        var bottleneck = EstimateBottleneckRelief(state, buildingType);
        var military = buildingType.Equals("wall", StringComparison.OrdinalIgnoreCase)
            || buildingType.Equals("barracks", StringComparison.OrdinalIgnoreCase)
            ? 0.5 * levelGap
            : 0;

        // Normalised production-investment signal (kept ~O(1) so it nudges economy
        // agents toward mines without overwhelming balanced growth).
        var future = prodGain / Math.Max(1.0, state.Profile.ProductionPerLevel);

        return new ActionScore(payback, levelGap, military, bottleneck, future);
    }

    private ActionScore ScoreTrain(SimCityState state, string troopType, int count)
    {
        var capacity = state.TrainCapacity;
        var cost = state.TroopsEngine.TrainCostForCount(troopType, count, capacity);
        var normalizedCost = NormalizeCost(state, cost);
        var military = troopType.Equals("soldier", StringComparison.OrdinalIgnoreCase)
            ? count * 0.3
            : count * 0.05;
        var foodBurden = count * 0.02;

        return new ActionScore(
            normalizedCost / Math.Max(0.1, military),
            -foodBurden,
            military,
            troopType.Equals("soldier", StringComparison.OrdinalIgnoreCase) ? -foodBurden : 0,
            0);
    }

    private ActionScore ScoreAttack(SimCityState state, CandidateAction action, IReadOnlyList<DefenderFixture> fixtures)
    {
        var fixture = fixtures.First(f => f.Name.Equals(action.FixtureName, StringComparison.OrdinalIgnoreCase));
        var soldiers = action.Count > 0 ? action.Count : state.GetTroopCount("soldier");
        var attackerPower = soldiers * 3;
        var defenderPower = fixture.Soldiers * 3 + state.Profile.WallDefencePerLevel * fixture.WallLevel;
        var winProb = attackerPower > defenderPower ? 1.0 : 0.1;
        var lootValue = (fixture.Resources.Gold + fixture.Resources.Stone + fixture.Resources.Wood + fixture.Resources.Food) * 0.01;

        return new ActionScore(50, lootValue * winProb, soldiers * 0.1, 0, lootValue * winProb);
    }

    private static double NormalizeCost(SimCityState state, BuildingUpgradeCost cost)
    {
        var rates = ResourceExchangeRates.FromProfile(state.Profile);
        return cost.Wood * rates.Wood
            + cost.Stone * rates.Stone
            + cost.Gold * rates.Gold
            + cost.Food * rates.Food;
    }

    private static double EstimateProductionGain(SimCityState state, string buildingType, int currentLevel)
    {
        var rule = BuildingRules.GetDefinition(buildingType);
        if (rule.EffectKind == BuildingEffectKind.Production)
        {
            return state.Profile.ProductionPerLevel;
        }

        if (buildingType.Equals("storage_shed", StringComparison.OrdinalIgnoreCase))
        {
            var atCap = state.Gold >= state.MaxGold || state.Wood >= state.MaxWood;
            return atCap ? 2.0 : 0.2;
        }

        if (buildingType.Equals("barracks", StringComparison.OrdinalIgnoreCase))
        {
            return state.Profile.BarracksTrainCapPerLevel * 0.5;
        }

        return 0.1;
    }

    private static double EstimateBottleneckRelief(SimCityState state, string buildingType)
    {
        var rule = BuildingRules.GetDefinition(buildingType);
        if (rule.EffectKind != BuildingEffectKind.Production || rule.Resource is null)
        {
            return buildingType.Equals("storage_shed", StringComparison.OrdinalIgnoreCase) ? 0.3 : 0;
        }

        var resource = rule.Resource.Value;
        var atCap = resource switch
        {
            llmmo.Api.Buildings.BuildingResource.Gold => state.Gold >= state.MaxGold,
            llmmo.Api.Buildings.BuildingResource.Stone => state.Stone >= state.MaxStone,
            llmmo.Api.Buildings.BuildingResource.Wood => state.Wood >= state.MaxWood,
            llmmo.Api.Buildings.BuildingResource.Food => state.Food >= state.MaxFood,
            _ => false,
        };

        var low = resource switch
        {
            llmmo.Api.Buildings.BuildingResource.Gold => state.Gold < state.MaxGold * 0.2,
            llmmo.Api.Buildings.BuildingResource.Stone => state.Stone < state.MaxStone * 0.2,
            llmmo.Api.Buildings.BuildingResource.Wood => state.Wood < state.MaxWood * 0.2,
            llmmo.Api.Buildings.BuildingResource.Food => state.Food < state.MaxFood * 0.2,
            _ => false,
        };

        return (atCap ? 1.0 : 0) + (low ? 0.5 : 0);
    }
}

public sealed record ResourceExchangeRates(double Wood, double Stone, double Gold, double Food)
{
    public static ResourceExchangeRates FromProfile(llmmo.Api.GameRules.BalanceProfile profile)
    {
        var costs = profile.BaseUpgradeCosts.Values.ToList();
        if (costs.Count == 0)
        {
            return new ResourceExchangeRates(1, 1, 1, 1);
        }

        var avgWood = costs.Average(c => c.Wood);
        var avgStone = costs.Average(c => c.Stone);
        var avgGold = costs.Average(c => c.Gold);
        var avgFood = costs.Average(c => c.Food);
        var baseRate = Math.Max(1, avgWood);

        return new ResourceExchangeRates(1, avgStone / baseRate, avgGold / baseRate, avgFood / baseRate);
    }
}
