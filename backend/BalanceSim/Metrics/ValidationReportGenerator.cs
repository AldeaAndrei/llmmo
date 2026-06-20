using BalanceSim.Metrics;

namespace BalanceSim.Metrics;

public static class ValidationReportGenerator
{
    public static string Generate(
        SimMetricsCollector metrics,
        ProgressionEvaluator evaluator,
        string agentName)
    {
        var summary = metrics.BuildSummary();
        var checkpoints = evaluator.EvaluateCheckpoints(metrics.Points);
        var bottleneck = metrics.BottleneckPct();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# Balance Validation Report");
        sb.AppendLine();
        sb.AppendLine($"Agent: **{agentName}**");
        sb.AppendLine();
        sb.AppendLine("## Progression Checkpoints");
        sb.AppendLine("| Day | Target % | Actual % |");
        sb.AppendLine("|-----|----------|----------|");
        foreach (var cp in checkpoints)
        {
            sb.AppendLine($"| {cp.Day} | {cp.TargetTotalPct} | {cp.ActualTotalPct:F1} |");
        }

        sb.AppendLine();
        sb.AppendLine("## Diagnostics");
        sb.AppendLine($"- Dominant bottleneck: **{summary.DominantBottleneck}**");
        if (bottleneck.Count > 0)
        {
            sb.AppendLine($"- Food at cap: {bottleneck.GetValueOrDefault("food"):F1}% of ticks");
            sb.AppendLine($"- Gold at cap: {bottleneck.GetValueOrDefault("gold"):F1}% of ticks");
        }

        sb.AppendLine($"- Starvation events: {summary.StarvationEvents}");
        sb.AppendLine($"- Upgrade slot utilization: {summary.UpgradeSlotUtilizationPct:F1}%");
        sb.AppendLine($"- Train slot utilization: {summary.TrainSlotUtilizationPct:F1}%");
        sb.AppendLine($"- Ticks waiting for resources: {summary.TicksWaitingForResources}");
        sb.AppendLine($"- Attacks launched/won: {summary.AttacksLaunched}/{summary.AttacksWon}");

        if (summary.TicksToWealth100Pct is int ticks)
        {
            var hours = ticks * 5 / 3600.0;
            sb.AppendLine($"- Estimated hours to 100% wealth: {hours:F1}");
        }

        sb.AppendLine();
        sb.AppendLine("## Questions");
        sb.AppendLine($"- Is food always the bottleneck? {(summary.DominantBottleneck == "food" ? "Likely yes" : "No")}");
        sb.AppendLine($"- Is gold overproduced? {(bottleneck.GetValueOrDefault("gold") > 30 ? "Likely yes" : "Unclear / no")}");
        sb.AppendLine($"- Are troops too cheap? Compare Military vs Economic agents in batch runs.");

        return sb.ToString();
    }
}
