using BalanceSim.Agents;

namespace BalanceSim.Cli;

public sealed record ParsedCliArgs
{
    public bool ExportCharts { get; init; }
    public bool Quick { get; init; }
    public string Agent { get; init; } = "Balanced";
    public int? Ticks { get; init; }
    public int? Candidates { get; init; }
    public int Seed { get; init; } = 42;
    public int SampleEvery { get; init; } = 36;
    public string? ProfilePath { get; init; }
    public string? RunDir { get; init; }

    public static ParsedCliArgs ParseSimulate(string[] args, int defaultTicks)
    {
        var parsed = ParseCommon(args);
        parsed = parsed with
        {
            Agent = GetFlag(args, "--agent") ?? parsed.Agent,
            Ticks = GetIntFlag(args, "--ticks") ?? parsed.Ticks ?? FindIntPositional(args, 0) ?? defaultTicks,
            Seed = GetIntFlag(args, "--seed") ?? parsed.Seed,
            SampleEvery = GetIntFlag(args, "--sample-every") ?? parsed.SampleEvery,
            ProfilePath = GetFlag(args, "--profile") ?? FindExistingPath(args),
        };

        if (TryParseAgent(args, out var positionalAgent))
        {
            parsed = parsed with { Agent = positionalAgent };
        }

        return parsed;
    }

    public static ParsedCliArgs ParseSearch(string[] args, int defaultTicks)
    {
        var parsed = ParseCommon(args);
        var positionalInts = args.Where(a => int.TryParse(a, out _)).Select(int.Parse).ToList();

        parsed = parsed with
        {
            Candidates = GetIntFlag(args, "--candidates")
                ?? GetIntFlag(args, "-c")
                ?? (positionalInts.Count > 0 ? positionalInts[0] : 20),
            Ticks = GetIntFlag(args, "--ticks") ?? defaultTicks,
        };

        return parsed;
    }

    public static ParsedCliArgs ParseReport(string[] args)
    {
        var parsed = ParseCommon(args);
        return parsed with
        {
            RunDir = GetFlag(args, "--run-dir") ?? FindExistingPath(args),
            Agent = GetFlag(args, "--agent") ?? parsed.Agent,
            ProfilePath = GetFlag(args, "--profile") ?? FindExistingPath(args),
        };
    }

    private static ParsedCliArgs ParseCommon(string[] args) => new()
    {
        ExportCharts = args.Any(IsChartsFlag),
        Quick = args.Any(a => a.Equals("--quick", StringComparison.OrdinalIgnoreCase)
            || a.Equals("quick", StringComparison.OrdinalIgnoreCase)),
    };

    private static bool IsChartsFlag(string arg) =>
        arg.Equals("--export-charts", StringComparison.OrdinalIgnoreCase)
        || arg.Equals("charts", StringComparison.OrdinalIgnoreCase)
        || arg.Equals("-charts", StringComparison.OrdinalIgnoreCase);

    private static string? GetFlag(string[] args, string name)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                return args[i + 1];
            }

            if (args[i].StartsWith(name + "=", StringComparison.OrdinalIgnoreCase))
            {
                return args[i][(name.Length + 1)..];
            }
        }

        return null;
    }

    private static int? GetIntFlag(string[] args, string name)
    {
        var value = GetFlag(args, name);
        return value is not null && int.TryParse(value, out var n) ? n : null;
    }

    private static int? FindIntPositional(string[] args, int index)
    {
        var ints = args.Where(a => int.TryParse(a, out _)).Select(int.Parse).ToList();
        return ints.Count > index ? ints[index] : null;
    }

    private static string? FindExistingPath(string[] args)
    {
        foreach (var arg in args)
        {
            if (arg.StartsWith('-') || IsChartsFlag(arg) || arg.Equals("quick", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (int.TryParse(arg, out _))
            {
                continue;
            }

            if (!AgentRegistry.AllNames.Contains(arg, StringComparer.OrdinalIgnoreCase)
                && (Directory.Exists(arg) || File.Exists(arg)))
            {
                return arg;
            }
        }

        return null;
    }

    private static bool TryParseAgent(string[] args, out string agent)
    {
        agent = "Balanced";
        foreach (var arg in args)
        {
            if (AgentRegistry.AllNames.Contains(arg, StringComparer.OrdinalIgnoreCase))
            {
                agent = arg;
                return true;
            }
        }

        return false;
    }
}
