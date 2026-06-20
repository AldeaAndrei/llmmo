using BalanceSim.Cli;

if (args.Length == 0)
{
    PrintUsage();
    return 1;
}

return args[0].ToLowerInvariant() switch
{
    "simulate" => SimCommands.RunSimulate(args[1..]),
    "search" => SimCommands.RunSearch(args[1..]),
    "report" => SimCommands.RunReport(args[1..]),
    _ => PrintUsage(),
};

static int PrintUsage()
{
    Console.WriteLine("""
        BalanceSim — headless balance simulation

        Commands (npm on Windows: use positional args — npm strips --flags):

          simulate [agent] [ticks] [charts] [--profile path.json|search-dir]
          search   [candidates] [charts] [quick]
          report   [run-dir]

        Examples:
          dotnet run --project backend/BalanceSim -- simulate Balanced 50400 charts
          dotnet run --project backend/BalanceSim -- search 50 charts
          dotnet run --project backend/BalanceSim -- simulate Balanced 50400 charts --profile BalanceSim/output/search-xxx

          npm run balance:sim simulate Balanced 50400 charts
          npm run balance:sim search 50 charts
        """);
    return 1;
}
