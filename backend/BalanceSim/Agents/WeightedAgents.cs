using BalanceSim.Core;

namespace BalanceSim.Agents;

public abstract class WeightedAgentBase : IActionAgent
{
    private readonly ActionScorer _scorer = new();
    private readonly double _paybackW;
    private readonly double _wealthW;
    private readonly double _militaryW;
    private readonly double _bottleneckW;
    private readonly double _futureW;

    protected WeightedAgentBase(
        string name,
        double paybackW,
        double wealthW,
        double militaryW,
        double bottleneckW,
        double futureW)
    {
        Name = name;
        _paybackW = paybackW;
        _wealthW = wealthW;
        _militaryW = militaryW;
        _bottleneckW = bottleneckW;
        _futureW = futureW;
    }

    public string Name { get; }

    public CandidateAction? ChooseAction(SimCityState state, IReadOnlyList<DefenderFixture> fixtures)
    {
        var actions = SimActionGenerator.GetAllActions(state, fixtures)
            .Where(a => IsAllowed(state, a))
            .ToList();

        if (actions.Count == 0)
        {
            return null;
        }

        CandidateAction? best = null;
        double bestScore = double.MinValue;

        foreach (var action in actions)
        {
            var score = _scorer.Score(state, action, fixtures);
            var weighted = score.Weighted(_paybackW, _wealthW, _militaryW, _bottleneckW, _futureW);
            if (weighted > bestScore)
            {
                bestScore = weighted;
                best = action;
            }
        }

        return best;
    }

    protected virtual bool IsAllowed(SimCityState state, CandidateAction action) =>
        IsFoodSustainable(state, action);

    /// <summary>
    /// Reject training that would push food upkeep above food production. This stops
    /// the death-spiral where the agent trains troops that immediately starve, wasting
    /// resources and flat-lining military progression.
    /// </summary>
    protected static bool IsFoodSustainable(SimCityState state, CandidateAction action)
    {
        if (action.Kind != SimActionKind.Train || action.TroopType is null)
        {
            return true;
        }

        var config = state.TroopsEngine.GetConfig(action.TroopType);
        if (config.UpkeepFood <= 0)
        {
            return true;
        }

        var addedUpkeep = Math.Max(1, action.Count) * config.UpkeepFood;
        var currentUpkeep = state.CalculateFoodUpkeep();
        var foodProduction = state.CalculateProduction().Food;
        return currentUpkeep + addedUpkeep <= foodProduction;
    }
}

public sealed class EconomicOptimizerAgent : WeightedAgentBase
{
    public EconomicOptimizerAgent() : base("Economic", 2, 1, 0, 2, 3) { }

    protected override bool IsAllowed(SimCityState state, CandidateAction action) =>
        base.IsAllowed(state, action)
        && action.Kind is not SimActionKind.Attack and not SimActionKind.Scout;
}

public sealed class MilitaryOptimizerAgent : WeightedAgentBase
{
    public MilitaryOptimizerAgent() : base("Military", 0.5, 1, 3, 1, 0.5) { }

    protected override bool IsAllowed(SimCityState state, CandidateAction action)
    {
        if (action.Kind == SimActionKind.Train && action.TroopType == "spy")
        {
            return false;
        }

        return base.IsAllowed(state, action);
    }
}

public sealed class BalancedOptimizerAgent : WeightedAgentBase
{
    public BalancedOptimizerAgent() : base("Balanced", 0.5, 3, 0.8, 1.5, 0.5) { }

    // The balance-search agent grows the city evenly and banks soldiers for military
    // wealth rather than raiding; keeping troops home avoids mission downtime and
    // makes progression deterministic and comparable across profiles.
    protected override bool IsAllowed(SimCityState state, CandidateAction action) =>
        base.IsAllowed(state, action)
        && action.Kind is not SimActionKind.Attack and not SimActionKind.Scout;
}

public sealed class ExpansionOptimizerAgent : WeightedAgentBase
{
    public ExpansionOptimizerAgent() : base("Expansion", 1, 2, 0.5, 1, 2) { }
}

public static class AgentRegistry
{
    private static readonly Dictionary<string, Func<IActionAgent>> Agents =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Economic"] = () => new EconomicOptimizerAgent(),
            ["Military"] = () => new MilitaryOptimizerAgent(),
            ["Balanced"] = () => new BalancedOptimizerAgent(),
            ["Expansion"] = () => new ExpansionOptimizerAgent(),
        };

    public static IReadOnlyList<string> AllNames => Agents.Keys.OrderBy(k => k).ToList();

    public static IActionAgent Create(string name)
    {
        if (!Agents.TryGetValue(name, out var factory))
        {
            throw new ArgumentException($"Unknown agent: {name}. Available: {string.Join(", ", AllNames)}");
        }

        return factory();
    }

    public static IReadOnlyList<IActionAgent> CreateAll() =>
        AllNames.Select(n => Create(n)).ToList();
}
