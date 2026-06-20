namespace llmmo.Api.GameRules;

public static class SaturationScaling
{
    /// <summary>
    /// Normalized saturation: (1 - exp(-x/n)) / (1 - exp(-xMax/n)).
    /// Returns 0 when x &lt;= 0; returns 1 when x >= xMax (within tolerance).
    /// </summary>
    public static double Multiplier(double x, double n, double xMax)
    {
        if (x <= 0 || xMax <= 0)
        {
            return 0;
        }

        if (x >= xMax)
        {
            return 1;
        }

        if (n <= 0)
        {
            return x / xMax;
        }

        var numerator = 1.0 - Math.Exp(-x / n);
        var denominator = 1.0 - Math.Exp(-xMax / n);

        if (denominator <= 1e-12)
        {
            return x / xMax;
        }

        return numerator / denominator;
    }

    public static int ScaleCost(int baseAmount, double multiplier) =>
        (int)Math.Max(0, Math.Round(baseAmount * multiplier));
}
