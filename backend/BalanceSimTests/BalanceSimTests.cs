using BalanceSim.Agents;
using BalanceSim.Core;
using BalanceSim.Metrics;
using llmmo.Api.GameRules;

namespace BalanceSimTests;

public class SaturationScalingTests
{
    [Fact]
    public void Multiplier_AtZero_ReturnsZero()
    {
        Assert.Equal(0, SaturationScaling.Multiplier(0, 5, 20));
    }

    [Fact]
    public void Multiplier_AtMax_ReturnsOne()
    {
        Assert.Equal(1, SaturationScaling.Multiplier(20, 5, 20), precision: 5);
    }
}

public class BuildingRulesEngineTests
{
    [Fact]
    public void DefaultProfile_MatchesLinearUpgradeCost()
    {
        var engine = new BuildingRulesEngine(BalanceProfile.Default);
        var cost = engine.UpgradeCostForLevel("gold_mine", 2);
        Assert.Equal(100, cost.Wood);
        Assert.Equal(60, cost.Stone);
    }

    [Fact]
    public void DefaultProfile_MatchesLinearUpgradeDuration()
    {
        var engine = new BuildingRulesEngine(BalanceProfile.Default);
        Assert.Equal(10, engine.UpgradeDurationTicks(5));
    }

    [Fact]
    public void DefaultProfile_TrainDuration_ScalesWithCount()
    {
        var engine = new BuildingRulesEngine(BalanceProfile.Default);
        Assert.Equal(1, engine.TrainDurationTicks(1, 20));
        Assert.Equal(10, engine.TrainDurationTicks(10, 20));
    }

    [Fact]
    public void SaturationProfile_UsesLowerEarlyCosts()
    {
        var profile = BalanceProfile.ForSimulation() with { NCost = 5 };
        var engine = new BuildingRulesEngine(profile);
        var satCost = engine.UpgradeCostForLevel("gold_mine", 2);
        var linearEngine = new BuildingRulesEngine(BalanceProfile.Default);
        var linearCost = linearEngine.UpgradeCostForLevel("gold_mine", 2);
        Assert.True(satCost.Wood < linearCost.Wood);
    }

    [Fact]
    public void SaturationProfile_TrainDuration_ScalesWithCount()
    {
        var profile = BalanceProfile.ForSimulation() with { NTrainTime = 3, BaseTrainTicks = 1 };
        var engine = new BuildingRulesEngine(profile);
        var small = engine.TrainDurationTicks(1, 20);
        var large = engine.TrainDurationTicks(10, 20);
        Assert.True(large > small);
    }
}

public class SimEngineTests
{
    [Fact]
    public void Run_Deterministic_WithSameSeed()
    {
        var profile = BalanceProfile.ForSimulation();
        var agent = AgentRegistry.Create("Balanced");

        var metrics1 = new SimMetricsCollector(profile);
        var engine1 = new SimEngine(profile, agent, seed: 42, metrics: metrics1);
        engine1.Run(500);

        var metrics2 = new SimMetricsCollector(profile);
        var engine2 = new SimEngine(profile, agent, seed: 42, metrics: metrics2);
        engine2.Run(500);

        Assert.Equal(metrics1.Points.Count, metrics2.Points.Count);
        Assert.Equal(metrics1.Points[^1].Wealth.TotalPct, metrics2.Points[^1].Wealth.TotalPct, precision: 5);
    }

    [Fact]
    public void Run_IncreasesWealth()
    {
        var profile = BalanceProfile.ForSimulation();
        var metrics = new SimMetricsCollector(profile);
        var engine = new SimEngine(profile, AgentRegistry.Create("Balanced"), metrics: metrics);
        engine.Run(1000);

        Assert.True(metrics.Points[^1].Wealth.TotalPct > metrics.Points[0].Wealth.TotalPct);
    }
}

public class AgentDeterminismTests
{
    [Fact]
    public void AllAgents_ProduceActions()
    {
        var state = new SimCityState(BalanceProfile.ForSimulation());
        var fixtures = DefenderFixtureLoader.BuiltInDefaults();

        foreach (var name in AgentRegistry.AllNames)
        {
            var agent = AgentRegistry.Create(name);
            var action = agent.ChooseAction(state, fixtures);
            Assert.NotNull(action);
        }
    }
}

public class WealthCalculatorTests
{
    [Fact]
    public void ReferenceWealth_Is100Percent()
    {
        var calc = new CompositeWealthCalculator();
        var reference = calc.ComputeReference(BalanceProfile.ForSimulation());
        Assert.Equal(100, reference.TotalPct, precision: 1);
    }
}

public class ProgressionEvaluatorTests
{
    [Fact]
    public void EvaluateMse_ReturnsNonNegative()
    {
        var profile = BalanceProfile.ForSimulation();
        var metrics = new SimMetricsCollector(profile);
        var engine = new SimEngine(profile, AgentRegistry.Create("Balanced"), metrics: metrics);
        engine.Run(3600);

        var evaluator = new ProgressionEvaluator();
        var mse = evaluator.EvaluateMse(metrics.Points);
        Assert.True(mse >= 0);
    }
}

public class ContractTests
{
    [Fact]
    public void DefaultEngine_UpgradeCost_MatchesBuildingRules()
    {
        var engine = new BuildingRulesEngine(BalanceProfile.Default);
        foreach (var type in BuildingRules.AllTypes)
        {
            for (var level = 2; level <= 5; level++)
            {
                var expected = BuildingRules.UpgradeCostForLevel(type, level);
                var actual = engine.UpgradeCostForLevel(type, level);
                Assert.Equal(expected, actual);
            }
        }
    }
}
