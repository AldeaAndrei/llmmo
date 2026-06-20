namespace BalanceSim.Metrics;

public record ProgressionCheckpoint(
    double Day,
    double TotalWealthPct,
    double EconomicWealthPct,
    double MilitaryWealthPct);

public sealed class ProgressionConfig
{
    public double TargetActiveHours { get; init; } = 70;
    public double ActiveHoursPerDay { get; init; } = 5;
    public int SecondsPerTick { get; init; } = 5;
    public List<ProgressionCheckpoint> Checkpoints { get; init; } = [];

    public int DefaultTicks =>
        (int)(TargetActiveHours * 3600 / SecondsPerTick);

    public int DayToTicks(double day) =>
        (int)(day * ActiveHoursPerDay * 3600 / SecondsPerTick);

    /// <summary>Linearly interpolates target wealth % between configured checkpoints.</summary>
    public ProgressionCheckpoint InterpolateTarget(double day)
    {
        if (Checkpoints.Count == 0)
        {
            return new ProgressionCheckpoint(day, 0, 0, 0);
        }

        var ordered = Checkpoints.OrderBy(c => c.Day).ToList();
        if (day <= ordered[0].Day)
        {
            return ordered[0] with { Day = day };
        }

        if (day >= ordered[^1].Day)
        {
            return ordered[^1] with { Day = day };
        }

        for (var i = 0; i < ordered.Count - 1; i++)
        {
            var a = ordered[i];
            var b = ordered[i + 1];
            if (day >= a.Day && day <= b.Day)
            {
                var t = (day - a.Day) / (b.Day - a.Day);
                return new ProgressionCheckpoint(
                    day,
                    Lerp(a.TotalWealthPct, b.TotalWealthPct, t),
                    Lerp(a.EconomicWealthPct, b.EconomicWealthPct, t),
                    Lerp(a.MilitaryWealthPct, b.MilitaryWealthPct, t));
            }
        }

        return ordered[^1] with { Day = day };
    }

    private static double Lerp(double a, double b, double t) => a + (b - a) * t;

    public static ProgressionConfig LoadDefault()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Config", "progression-default.json");
        if (!File.Exists(path))
        {
            return BuiltIn();
        }

        var json = File.ReadAllText(path);
        return System.Text.Json.JsonSerializer.Deserialize<ProgressionConfig>(json,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? BuiltIn();
    }

    public static ProgressionConfig BuiltIn() => new()
    {
        Checkpoints =
        [
            new(1, 12, 15, 4),
            new(2, 20, 24, 9),
            new(3, 28, 33, 15),
            new(4, 35, 41, 21),
            new(5, 42, 48, 28),
            new(6, 48, 54, 34),
            new(7, 55, 61, 41),
            new(8, 61, 67, 48),
            new(9, 67, 73, 54),
            new(10, 73, 79, 60),
            new(11, 80, 85, 66),
            new(12, 87, 91, 71),
            new(13, 94, 96, 76),
            new(14, 100, 100, 80),
        ],
    };
}

public sealed class ProgressionEvaluator
{
    private readonly ProgressionConfig _config;

    public ProgressionEvaluator(ProgressionConfig? config = null)
    {
        _config = config ?? ProgressionConfig.LoadDefault();
    }

    public double EvaluateMse(IReadOnlyList<TimeseriesPoint> points)
    {
        if (_config.Checkpoints.Count == 0 || points.Count == 0)
        {
            return double.MaxValue;
        }

        var sum = 0.0;
        foreach (var checkpoint in _config.Checkpoints)
        {
            var actual = InterpolateWealth(points, checkpoint.Day);
            var err = actual.TotalPct - checkpoint.TotalWealthPct;
            sum += err * err;
        }

        return sum / _config.Checkpoints.Count;
    }

    /// <summary>
    /// Search fitness error (lower is better): curve-fit MSE plus shaping penalties
    /// that steer the search away from flat plateaus and dead-end profiles.
    /// </summary>
    public double EvaluateFitnessError(IReadOnlyList<TimeseriesPoint> points)
    {
        if (_config.Checkpoints.Count == 0 || points.Count == 0)
        {
            return double.MaxValue;
        }

        var mse = EvaluateMse(points);

        var ordered = _config.Checkpoints.OrderBy(c => c.Day).ToList();
        var firstDay = ordered[0].Day;
        var lastDay = ordered[^1].Day;
        var finalTarget = ordered[^1].TotalWealthPct;

        var early = InterpolateWealth(points, firstDay).TotalPct;
        var late = InterpolateWealth(points, lastDay).TotalPct;

        // Penalise ending far short of (or wildly past) the final target.
        var endpointGap = late - finalTarget;
        var endpointPenalty = endpointGap * endpointGap * 0.5;

        // Penalise flat curves: a healthy run should grow substantially day1 -> day14.
        var growth = late - early;
        var flatnessPenalty = growth < 30 ? (30 - growth) * (30 - growth) * 1.5 : 0;

        // Penalise non-monotonic regressions (wealth should never go backwards much).
        var regressionPenalty = 0.0;
        var prev = double.MinValue;
        foreach (var p in points)
        {
            var v = p.Wealth.TotalPct;
            if (prev > double.MinValue && v < prev)
            {
                var drop = prev - v;
                regressionPenalty += drop * drop;
            }
            prev = v;
        }
        regressionPenalty *= 0.05;

        return mse + endpointPenalty + flatnessPenalty + regressionPenalty;
    }

    public List<CheckpointResult> EvaluateCheckpoints(IReadOnlyList<TimeseriesPoint> points) =>
        _config.Checkpoints.Select(cp =>
        {
            var actual = InterpolateWealth(points, cp.Day);
            return new CheckpointResult(cp.Day, cp.TotalWealthPct, actual.TotalPct, actual.EconomicPct, actual.MilitaryPct);
        }).ToList();

    private WealthSnapshot InterpolateWealth(IReadOnlyList<TimeseriesPoint> points, double day)
    {
        if (points.Count == 0)
        {
            return new WealthSnapshot(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        }

        TimeseriesPoint? before = null;
        TimeseriesPoint? after = null;
        foreach (var p in points)
        {
            if (p.Day <= day)
            {
                before = p;
            }

            if (p.Day >= day)
            {
                after = p;
                break;
            }
        }

        if (before is null)
        {
            return after?.Wealth ?? points[0].Wealth;
        }

        if (after is null || ReferenceEquals(before, after) || after.Day <= before.Day)
        {
            return before.Wealth;
        }

        var t = (day - before.Day) / (after.Day - before.Day);
        return new WealthSnapshot(
            Lerp(before.Wealth.Total, after.Wealth.Total, t),
            Lerp(before.Wealth.Economic, after.Wealth.Economic, t),
            Lerp(before.Wealth.Military, after.Wealth.Military, t),
            Lerp(before.Wealth.SunkCapital, after.Wealth.SunkCapital, t),
            Lerp(before.Wealth.ProductiveCapacity, after.Wealth.ProductiveCapacity, t),
            Lerp(before.Wealth.MilitaryPower, after.Wealth.MilitaryPower, t),
            Lerp(before.Wealth.LiquidAssets, after.Wealth.LiquidAssets, t),
            Lerp(before.Wealth.TotalPct, after.Wealth.TotalPct, t),
            Lerp(before.Wealth.EconomicPct, after.Wealth.EconomicPct, t),
            Lerp(before.Wealth.MilitaryPct, after.Wealth.MilitaryPct, t));
    }

    private static double Lerp(double a, double b, double t) => a + (b - a) * t;
}

public record CheckpointResult(
    double Day,
    double TargetTotalPct,
    double ActualTotalPct,
    double ActualEconomicPct,
    double ActualMilitaryPct);
