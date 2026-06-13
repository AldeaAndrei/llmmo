using llmmo.Data;
using llmmo.Entities;
using llmmo.WorldSetup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var backendDir = FindBackendDirectory();

var configuration = new ConfigurationBuilder()
    .SetBasePath(backendDir)
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var connectionString = configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseNpgsql(connectionString)
    .Options;

await using var db = new AppDbContext(options);

Console.WriteLine("Resetting world and seeding demo data...");
Console.WriteLine();

await WorldSeeder.RunAsync(db);

var playerCount = await db.Players.CountAsync();
var llmCount = await db.Players.CountAsync(player => player.PlayerType == PlayerType.Llm);
var userCount = await db.Users.CountAsync();
var cityCount = await db.Cities.CountAsync();
var world = await db.WorldState.AsNoTracking().FirstAsync(state => state.Id == 1);

Console.WriteLine();
Console.WriteLine(
    $"Done. {playerCount} players ({llmCount} llm), {userCount} user(s), {cityCount} cities, tick={world.CurrentTick}.");
Console.WriteLine("Default login: admin@yahoo.com / test1234");
Console.WriteLine("Try GET http://localhost:5000/api/v1/map");

static string FindBackendDirectory()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null)
    {
        if (File.Exists(Path.Combine(dir.FullName, "llmmo.csproj")))
        {
            return dir.FullName;
        }

        dir = dir.Parent;
    }

    throw new InvalidOperationException("Could not find backend directory (llmmo.csproj).");
}
