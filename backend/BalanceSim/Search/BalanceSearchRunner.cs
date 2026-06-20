using BalanceSim.Agents;
using BalanceSim.Core;
using BalanceSim.Metrics;
using llmmo.Api.GameRules;

namespace BalanceSim.Search;

public sealed class SearchContext
{
    public int CandidatesEvaluated { get; set; }
    public BalanceProfile? BestProfile { get; set; }
    public double BestFitness { get; set; } = double.MinValue;
    public Random Random { get; } = new();
}

public interface IBalanceSearchStrategy
{
    BalanceProfile NextCandidate(SearchContext context);
}

public sealed class RandomSearchStrategy : IBalanceSearchStrategy
{
    // Bounds shared with the hybrid/local strategy so mutations stay in range.
    public const double MinNCost = 1.5;
    public const double MaxNCost = 12.0;
    public const double MinNUpgradeTime = 1.5;
    public const double MaxNUpgradeTime = 12.0;
    public const double MinNTrainTime = 1.0;
    public const double MaxNTrainTime = 10.0;

    // BaseUpgradeTicks is the dominant pacing lever: with a single upgrade slot and
    // ~152 total upgrades across 50k ticks, upgrades must average a few hundred ticks
    // for progression to span the full 14 days instead of finishing in under a day.
    public const int MinBaseUpgradeTicks = 100;
    public const int MaxBaseUpgradeTicks = 900;
    public const int MinBaseTrainTicks = 8;
    public const int MaxBaseTrainTicks = 150;
    public const int MinProductionPerLevel = 1;
    public const int MaxProductionPerLevel = 5;
    public const double MinCostScale = 0.5;
    public const double MaxCostScale = 2.5;

    public BalanceProfile NextCandidate(SearchContext context)
    {
        var r = context.Random;
        var baseProfile = BalanceProfile.ForSimulation();

        return baseProfile with
        {
            NCost = MinNCost + r.NextDouble() * (MaxNCost - MinNCost),
            NUpgradeTime = MinNUpgradeTime + r.NextDouble() * (MaxNUpgradeTime - MinNUpgradeTime),
            NTrainTime = MinNTrainTime + r.NextDouble() * (MaxNTrainTime - MinNTrainTime),
            BaseUpgradeTicks = r.Next(MinBaseUpgradeTicks, MaxBaseUpgradeTicks + 1),
            BaseTrainTicks = r.Next(MinBaseTrainTicks, MaxBaseTrainTicks + 1),
            ProductionPerLevel = r.Next(MinProductionPerLevel, MaxProductionPerLevel + 1),
            BaseUpgradeCosts = ScaleBuildingCosts(
                baseProfile.BaseUpgradeCosts,
                MinCostScale + r.NextDouble() * (MaxCostScale - MinCostScale)),
        };
    }

    internal static Dictionary<string, llmmo.Api.Buildings.BuildingUpgradeCost> ScaleBuildingCosts(
        IReadOnlyDictionary<string, llmmo.Api.Buildings.BuildingUpgradeCost> costs,
        double factor)
    {
        return costs.ToDictionary(
            kvp => kvp.Key,
            kvp => new llmmo.Api.Buildings.BuildingUpgradeCost(
                (int)Math.Round(kvp.Value.Wood * factor),
                (int)Math.Round(kvp.Value.Stone * factor),
                (int)Math.Round(kvp.Value.Gold * factor),
                kvp.Key.Equals("bakery", StringComparison.OrdinalIgnoreCase) ? 0 : (int)Math.Round(kvp.Value.Food * factor)),
            StringComparer.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Explore-then-exploit search: the first fraction of candidates are sampled randomly
/// to map the space, after which the search hill-climbs by perturbing the best profile
/// found so far. This converges on good curves far faster than pure random sampling.
/// </summary>
public sealed class HybridSearchStrategy : IBalanceSearchStrategy
{
    private readonly RandomSearchStrategy _random = new();
    private readonly int _exploreCount;
    private int _index;

    public HybridSearchStrategy(int totalCandidates, double exploreFraction = 0.5)
    {
        _exploreCount = Math.Max(4, (int)Math.Round(totalCandidates * exploreFraction));
    }

    public BalanceProfile NextCandidate(SearchContext context)
    {
        _index++;
        if (context.BestProfile is null || _index <= _exploreCount)
        {
            return _random.NextCandidate(context);
        }

        return Mutate(context.BestProfile, context.Random);
    }

    private static BalanceProfile Mutate(BalanceProfile b, Random r)
    {
        double Jitter(double value, double frac, double min, double max)
        {
            var factor = 1.0 + (r.NextDouble() * 2 - 1) * frac;
            return Math.Clamp(value * factor, min, max);
        }

        int JitterInt(int value, double frac, int min, int max) =>
            (int)Math.Round(Jitter(value, frac, min, max));

        return b with
        {
            NCost = Jitter(b.NCost, 0.3, RandomSearchStrategy.MinNCost, RandomSearchStrategy.MaxNCost),
            NUpgradeTime = Jitter(b.NUpgradeTime, 0.3, RandomSearchStrategy.MinNUpgradeTime, RandomSearchStrategy.MaxNUpgradeTime),
            NTrainTime = Jitter(b.NTrainTime, 0.3, RandomSearchStrategy.MinNTrainTime, RandomSearchStrategy.MaxNTrainTime),
            BaseUpgradeTicks = JitterInt(b.BaseUpgradeTicks, 0.3, RandomSearchStrategy.MinBaseUpgradeTicks, RandomSearchStrategy.MaxBaseUpgradeTicks),
            BaseTrainTicks = JitterInt(b.BaseTrainTicks, 0.3, RandomSearchStrategy.MinBaseTrainTicks, RandomSearchStrategy.MaxBaseTrainTicks),
            ProductionPerLevel = Math.Clamp(
                b.ProductionPerLevel + r.Next(-1, 2),
                RandomSearchStrategy.MinProductionPerLevel,
                RandomSearchStrategy.MaxProductionPerLevel),
            BaseUpgradeCosts = RandomSearchStrategy.ScaleBuildingCosts(
                b.BaseUpgradeCosts,
                Jitter(1.0, 0.25, 0.6, 1.6)),
        };
    }
}

public sealed class BalanceSearchRunner
{
    private readonly ProgressionConfig _progression;
    private readonly ProgressionEvaluator _evaluator;

    public BalanceSearchRunner(ProgressionConfig? progression = null)
    {
        _progression = progression ?? ProgressionConfig.LoadDefault();
        _evaluator = new ProgressionEvaluator(_progression);
    }

    public SearchResult Run(
        IBalanceSearchStrategy strategy,
        int candidates,
        int ticks,
        bool quick = false)
    {
        var context = new SearchContext();
        var ranked = new List<RankedCandidate>();
        var seeds = quick ? [42] : new[] { 42, 43, 44 };

        for (var i = 0; i < candidates; i++)
        {
            var profile = strategy.NextCandidate(context);
            var profileId = $"candidate-{i:D3}";
            var agentResults = new List<(string Agent, double Mse, double Error)>();

            foreach (var seed in seeds)
            {
                var agent = AgentRegistry.Create("Balanced");
                var metrics = new SimMetricsCollector(profile, _progression);
                var engine = new SimEngine(profile, agent, seed: seed, metrics: metrics);
                engine.Run(ticks);
                var mse = _evaluator.EvaluateMse(metrics.Points);
                var error = _evaluator.EvaluateFitnessError(metrics.Points);
                agentResults.Add((agent.Name, mse, error));
            }

            var avgMse = agentResults.Average(r => r.Mse);
            var avgError = agentResults.Average(r => r.Error);
            var fitness = -avgError;
            var candidate = new RankedCandidate(
                i + 1,
                profileId,
                profile,
                fitness,
                avgMse,
                BuildRecommendations(profile));

            ranked.Add(candidate);

            if (fitness > context.BestFitness)
            {
                context.BestFitness = fitness;
                context.BestProfile = profile;
            }

            context.CandidatesEvaluated++;
        }

        ranked = ranked.OrderByDescending(c => c.FitnessScore).ToList();
        for (var i = 0; i < ranked.Count; i++)
        {
            ranked[i] = ranked[i] with { Rank = i + 1 };
        }

        return new SearchResult(
            $"search-{DateTime.UtcNow:yyyy-MM-dd-HHmmss}",
            strategy.GetType().Name,
            candidates,
            ranked);
    }

    private static List<string> BuildRecommendations(BalanceProfile profile)
    {
        var defaults = BalanceProfile.Default;
        var changes = new List<string>
        {
            $"n_cost={profile.NCost:F2}",
            $"n_upgradeTime={profile.NUpgradeTime:F2}",
            $"n_trainTime={profile.NTrainTime:F2}",
            $"productionPerLevel={profile.ProductionPerLevel} (default {defaults.ProductionPerLevel})",
        };
        return changes;
    }
}

public record RankedCandidate(
    int Rank,
    string ProfileId,
    BalanceProfile Profile,
    double FitnessScore,
    double ProgressionMse,
    List<string> RecommendedChanges);

public record SearchResult(
    string SearchId,
    string Strategy,
    int CandidatesEvaluated,
    List<RankedCandidate> RankedProfiles);
