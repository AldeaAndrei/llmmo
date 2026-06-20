using BalanceSim.Core;
using llmmo.Api.GameRules;

namespace BalanceSim.Metrics;

public record WealthSnapshot(
    double Total,
    double Economic,
    double Military,
    double SunkCapital,
    double ProductiveCapacity,
    double MilitaryPower,
    double LiquidAssets,
    double TotalPct,
    double EconomicPct,
    double MilitaryPct);

public interface IWealthCalculator
{
    WealthSnapshot Compute(SimCityState state);
    WealthSnapshot ComputeReference(BalanceProfile profile);
}

public sealed class CompositeWealthCalculator : IWealthCalculator
{
    public WealthSnapshot Compute(SimCityState state)
    {
        var rates = Agents.ResourceExchangeRates.FromProfile(state.Profile);
        var sunk = state.Spend.Total;
        var production = state.CalculateProduction();
        var productive = production.Gold * rates.Gold
            + production.Stone * rates.Stone
            + production.Wood * rates.Wood
            + production.Food * rates.Food;

        var soldierCount = state.GetTroopCount("soldier");
        var wallLevel = state.GetBuildingLevel("wall");
        var militaryPower = soldierCount * 3 + wallLevel * state.Profile.WallDefencePerLevel;

        var capPenalty = ComputeCapPenalty(state);
        var liquid = (state.Gold * rates.Gold + state.Stone * rates.Stone
            + state.Wood * rates.Wood + state.Food * rates.Food) * (1 - capPenalty);

        var economic = sunk * 0.6 + productive * 10 + liquid * 0.4;
        var military = soldierCount * rates.Gold * 2 + wallLevel * rates.Stone * 5;
        var total = economic * 0.75 + military * 0.25;

        var reference = ComputeReference(state.Profile);
        return new WealthSnapshot(
            total,
            economic,
            military,
            sunk,
            productive,
            militaryPower,
            liquid,
            reference.Total > 0 ? 100 * total / reference.Total : 0,
            reference.Economic > 0 ? 100 * economic / reference.Economic : 0,
            reference.Military > 0 ? 100 * military / reference.Military : 0);
    }

    public WealthSnapshot ComputeReference(BalanceProfile profile)
    {
        var engine = new BuildingRulesEngine(profile);
        var rates = Agents.ResourceExchangeRates.FromProfile(profile);

        var sunkEstimate = 0.0;
        foreach (var type in BuildingRules.AllTypes)
        {
            for (var level = 2; level <= profile.MaxBuildingLevel; level++)
            {
                var cost = engine.UpgradeCostForLevel(type, level);
                sunkEstimate += cost.Wood * rates.Wood + cost.Stone * rates.Stone
                    + cost.Gold * rates.Gold + cost.Food * rates.Food;
            }
        }

        var prodPerTick = profile.ProductionPerLevel * profile.MaxBuildingLevel * 4;
        var productive = prodPerTick * (rates.Gold + rates.Stone + rates.Wood + rates.Food);
        var military = profile.ReferenceEndgameSoldiers * rates.Gold * 2
            + profile.MaxBuildingLevel * rates.Stone * 5;
        var economic = sunkEstimate * 0.6 + productive * 10;
        var total = economic * 0.75 + military * 0.25;

        return new WealthSnapshot(
            total, economic, military,
            sunkEstimate, productive,
            profile.ReferenceEndgameSoldiers * 3 + profile.MaxBuildingLevel * profile.WallDefencePerLevel,
            0,
            100, 100, 100);
    }

    private static double ComputeCapPenalty(SimCityState state)
    {
        var capped = 0;
        if (state.Gold >= state.MaxGold) capped++;
        if (state.Stone >= state.MaxStone) capped++;
        if (state.Wood >= state.MaxWood) capped++;
        if (state.Food >= state.MaxFood) capped++;
        return capped * 0.15;
    }
}
