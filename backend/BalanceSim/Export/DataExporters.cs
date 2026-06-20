using BalanceSim.Metrics;
using BalanceSim.Search;
using llmmo.Api.GameRules;

namespace BalanceSim.Export;

public static class TimeseriesExporter
{
    public static string Export(
        string outputDir,
        string runId,
        string profileId,
        string agent,
        int seed,
        int ticksTotal,
        int sampleEvery,
        SimMetricsCollector metrics)
    {
        Directory.CreateDirectory(outputDir);
        var path = Path.Combine(outputDir, "timeseries.json");
        var summary = metrics.BuildSummary();

        var doc = new
        {
            runId,
            profileId,
            agent,
            seed,
            ticksTotal,
            sampleEvery,
            points = metrics.Points.Select(p => new
            {
                tick = p.Tick,
                day = p.Day,
                wealth = new
                {
                    totalPct = p.Wealth.TotalPct,
                    economicPct = p.Wealth.EconomicPct,
                    militaryPct = p.Wealth.MilitaryPct,
                    sunkCapital = p.Wealth.SunkCapital,
                    productiveCapacity = p.Wealth.ProductiveCapacity,
                    militaryPower = p.Wealth.MilitaryPower,
                    liquidAssets = p.Wealth.LiquidAssets,
                },
                resources = new { gold = p.Resources.Gold, stone = p.Resources.Stone, wood = p.Resources.Wood, food = p.Resources.Food },
                resourceCaps = new { gold = p.ResourceCaps.Gold, stone = p.ResourceCaps.Stone, wood = p.ResourceCaps.Wood, food = p.ResourceCaps.Food },
                productionPerTick = new { gold = p.Production.Gold, stone = p.Production.Stone, wood = p.Production.Wood, food = p.Production.Food },
                atCap = p.AtCap,
                buildingLevels = p.BuildingLevels,
                troops = p.Troops,
                slots = new { upgradeBusy = p.UpgradeBusy, trainBusy = p.TrainBusy },
                events = p.Events.Select(e => new { type = e.Type, detail = e.Detail }),
            }),
            summary = new
            {
                ticksToWealth100Pct = summary.TicksToWealth100Pct,
                starvationEvents = summary.StarvationEvents,
                troopsLostToStarvation = summary.TroopsLostToStarvation,
                upgradeSlotUtilizationPct = summary.UpgradeSlotUtilizationPct,
                trainSlotUtilizationPct = summary.TrainSlotUtilizationPct,
                ticksWaitingForResources = summary.TicksWaitingForResources,
                attacksLaunched = summary.AttacksLaunched,
                attacksWon = summary.AttacksWon,
                totalLoot = new { gold = summary.TotalLoot.Gold, stone = summary.TotalLoot.Stone, wood = summary.TotalLoot.Wood, food = summary.TotalLoot.Food },
                dominantBottleneck = summary.DominantBottleneck,
            },
        };

        File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(doc, JsonOptions));
        return path;
    }

    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
}

public static class BatchAggregateExporter
{
    public static string Export(string outputDir, string batchId, string profileId, BatchAggregateData data)
    {
        Directory.CreateDirectory(outputDir);
        var path = Path.Combine(outputDir, "batch-aggregate.json");
        File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(new
        {
            batchId,
            profileId,
            data.RunCount,
            agents = data.Agents,
            wealthCurve = data.WealthCurves,
            progressionFit = data.ProgressionFit,
            bottleneckPct = data.BottleneckPct,
            buildingUpgradeShare = data.BuildingUpgradeShare,
            buildOrderEntropy = data.BuildOrderEntropy,
            deadEndRatePct = data.DeadEndRatePct,
            strategyDominanceGap = data.StrategyDominanceGap,
            fitnessScore = data.FitnessScore,
        }, JsonOptions));
        return path;
    }

    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
}

public record BatchAggregateData(
    int RunCount,
    List<string> Agents,
    Dictionary<string, object> WealthCurves,
    Dictionary<string, object> ProgressionFit,
    Dictionary<string, double> BottleneckPct,
    Dictionary<string, double> BuildingUpgradeShare,
    double BuildOrderEntropy,
    double DeadEndRatePct,
    double StrategyDominanceGap,
    double FitnessScore);

public static class SearchResultsExporter
{
    public static string Export(string outputDir, SearchResult result)
    {
        Directory.CreateDirectory(outputDir);
        var path = Path.Combine(outputDir, "search-results.json");
        File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(new
        {
            searchId = result.SearchId,
            strategy = result.Strategy,
            candidatesEvaluated = result.CandidatesEvaluated,
            rankedProfiles = result.RankedProfiles.Select(p => new
            {
                rank = p.Rank,
                profileId = p.ProfileId,
                fitnessScore = p.FitnessScore,
                progressionMse = p.ProgressionMse,
                paramDeltas = new
                {
                    nCost = p.Profile.NCost,
                    nUpgradeTime = p.Profile.NUpgradeTime,
                    nTrainTime = p.Profile.NTrainTime,
                    productionPerLevel = p.Profile.ProductionPerLevel,
                },
                recommendedChanges = p.RecommendedChanges,
            }),
        }, JsonOptions));
        return path;
    }

    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
}

public static class ProfileExporter
{
    public static string Export(string path, BalanceProfile profile)
    {
        File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(profile, JsonOptions));
        return path;
    }

    public static BalanceProfile Import(string path)
    {
        var json = File.ReadAllText(path);
        return System.Text.Json.JsonSerializer.Deserialize<BalanceProfile>(json, JsonOptions)
            ?? BalanceProfile.Default;
    }

    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };
}
