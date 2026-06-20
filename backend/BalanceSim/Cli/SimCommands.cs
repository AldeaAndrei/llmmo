using BalanceSim.Agents;
using BalanceSim.Core;
using BalanceSim.Export;
using BalanceSim.Metrics;
using BalanceSim.Search;
using llmmo.Api.GameRules;

namespace BalanceSim.Cli;

public static class SimCommands
{
    public static int RunSimulate(string[] args)
    {
        var progression = ProgressionConfig.LoadDefault();
        var parsed = ParsedCliArgs.ParseSimulate(args, progression.DefaultTicks);
        var ticks = parsed.Ticks ?? progression.DefaultTicks;
        var profile = LoadProfile(parsed.ProfilePath);

        var agent = AgentRegistry.Create(parsed.Agent);
        var metrics = new SimMetricsCollector(profile, progression);
        var engine = new SimEngine(profile, agent, seed: parsed.Seed, metrics: metrics);
        engine.Run(ticks);

        var runId = $"run-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
        var outputDir = Path.Combine(GetOutputRoot(), runId);
        Directory.CreateDirectory(outputDir);

        ProfileExporter.Export(Path.Combine(outputDir, "profile.json"), profile);
        TimeseriesExporter.Export(outputDir, runId, profile.GetType().Name, parsed.Agent, parsed.Seed, ticks, parsed.SampleEvery, metrics);

        var evaluator = new ProgressionEvaluator(progression);
        var mse = evaluator.EvaluateMse(metrics.Points);
        Console.WriteLine($"Agent: {parsed.Agent}, Ticks: {ticks}, MSE: {mse:F2}");
        foreach (var cp in evaluator.EvaluateCheckpoints(metrics.Points))
        {
            Console.WriteLine($"  Day {cp.Day}: target={cp.TargetTotalPct}% actual={cp.ActualTotalPct:F1}%");
        }

        if (parsed.ExportCharts)
        {
            var chartPath = ChartReportGenerator.Generate(outputDir, metrics.Points, progression);
            Console.WriteLine($"Charts: {chartPath}");
        }
        else
        {
            Console.WriteLine("Tip: add 'charts' to generate report.html (npm strips --export-charts on Windows).");
        }

        Console.WriteLine($"Output: {outputDir}");
        return 0;
    }

    public static int RunSearch(string[] args)
    {
        var progression = ProgressionConfig.LoadDefault();
        var parsed = ParsedCliArgs.ParseSearch(args, progression.DefaultTicks);
        var ticks = parsed.Quick ? 10_000 : (parsed.Ticks ?? progression.DefaultTicks);
        var candidates = parsed.Candidates ?? 20;

        var runner = new BalanceSearchRunner(progression);
        var strategy = new HybridSearchStrategy(candidates);
        var result = runner.Run(strategy, candidates, ticks, parsed.Quick);

        var outputDir = Path.Combine(GetOutputRoot(), result.SearchId);
        SearchResultsExporter.Export(outputDir, result);

        if (result.RankedProfiles.Count > 0)
        {
            var bestProfilePath = Path.Combine(outputDir, "best-profile.json");
            ProfileExporter.Export(bestProfilePath, result.RankedProfiles[0].Profile);
            Console.WriteLine($"Best profile: {bestProfilePath}");
        }

        Console.WriteLine($"Search complete: {result.CandidatesEvaluated} candidates");
        foreach (var top in result.RankedProfiles.Take(5))
        {
            Console.WriteLine($"  #{top.Rank} {top.ProfileId} fitness={top.FitnessScore:F2} mse={top.ProgressionMse:F2}");
            foreach (var change in top.RecommendedChanges)
            {
                Console.WriteLine($"    - {change}");
            }
        }

        if (parsed.ExportCharts && result.RankedProfiles.Count > 0)
        {
            var best = result.RankedProfiles[0];
            var agent = AgentRegistry.Create("Balanced");
            var metrics = new SimMetricsCollector(best.Profile, progression);
            var engine = new SimEngine(best.Profile, agent, metrics: metrics);
            engine.Run(ticks);
            var chartPath = ChartReportGenerator.Generate(outputDir, metrics.Points, progression);
            Console.WriteLine($"Charts: {chartPath}");
        }

        Console.WriteLine($"Output: {outputDir}");
        return 0;
    }

    public static int RunReport(string[] args)
    {
        var parsed = ParsedCliArgs.ParseReport(args);
        if (parsed.RunDir is null || !Directory.Exists(parsed.RunDir))
        {
            Console.Error.WriteLine("Usage: report --run-dir <path>  OR  report <path>");
            return 1;
        }

        var progression = ProgressionConfig.LoadDefault();
        var profile = LoadProfile(parsed.ProfilePath ?? Path.Combine(parsed.RunDir, "best-profile.json"));
        var metrics = new SimMetricsCollector(profile, progression);
        var engine = new SimEngine(profile, AgentRegistry.Create(parsed.Agent), metrics: metrics);
        engine.Run(progression.DefaultTicks);

        var report = ValidationReportGenerator.Generate(metrics, new ProgressionEvaluator(progression), parsed.Agent);
        var reportPath = Path.Combine(parsed.RunDir, "validation-report.md");
        File.WriteAllText(reportPath, report);
        Console.WriteLine(report);
        Console.WriteLine($"Report: {reportPath}");
        return 0;
    }

    private static BalanceProfile LoadProfile(string? profilePath)
    {
        if (profilePath is null)
        {
            return BalanceProfile.ForSimulation();
        }

        if (File.Exists(profilePath))
        {
            return ProfileExporter.Import(profilePath);
        }

        if (Directory.Exists(profilePath))
        {
            var best = Path.Combine(profilePath, "best-profile.json");
            if (File.Exists(best))
            {
                return ProfileExporter.Import(best);
            }

            var profile = Path.Combine(profilePath, "profile.json");
            if (File.Exists(profile))
            {
                return ProfileExporter.Import(profile);
            }

            Console.Error.WriteLine($"No profile JSON in {profilePath}; using default simulation profile.");
        }

        return BalanceProfile.ForSimulation();
    }

    private static string GetOutputRoot()
    {
        var root = Path.Combine(Directory.GetCurrentDirectory(), "BalanceSim", "output");
        Directory.CreateDirectory(root);
        return root;
    }
}
