using BalanceSim.Metrics;

namespace BalanceSim.Export;

public static class ChartDataBuilder
{
    public const int MaxDay = 14;

    public static int[] DayLabels() => Enumerable.Range(1, MaxDay).ToArray();

    public static double[] SampleWealth(
        IReadOnlyList<TimeseriesPoint> points,
        ProgressionConfig config,
        Func<WealthSnapshot, double> selector)
    {
        return DayLabels().Select(day => SampleAtDay(points, config, day, selector)).ToArray();
    }

    public static double[] TargetWealth(ProgressionConfig config, Func<ProgressionCheckpoint, double> selector) =>
        DayLabels().Select(day => selector(config.InterpolateTarget(day))).ToArray();

    public static int[] SampleInt(
        IReadOnlyList<TimeseriesPoint> points,
        ProgressionConfig config,
        Func<TimeseriesPoint, int> selector) =>
        DayLabels().Select(day => SamplePointAtDay(points, config, day, selector)).ToArray();

    private static double SampleAtDay(
        IReadOnlyList<TimeseriesPoint> points,
        ProgressionConfig config,
        int day,
        Func<WealthSnapshot, double> selector) =>
        selector(SamplePointAtDay(points, config, day, p => p.Wealth));

    private static T SamplePointAtDay<T>(
        IReadOnlyList<TimeseriesPoint> points,
        ProgressionConfig config,
        int day,
        Func<TimeseriesPoint, T> selector)
    {
        if (points.Count == 0)
        {
            return default!;
        }

        var targetDay = (double)day;
        var atOrAfter = points.FirstOrDefault(p => p.Day >= targetDay);
        if (atOrAfter is not null)
        {
            return selector(atOrAfter);
        }

        return selector(points[^1]);
    }
}
