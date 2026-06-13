using llmmo.Data;
using llmmo.Entities;
using Microsoft.EntityFrameworkCore;

namespace llmmo.WorldSetup;

public static class WorldSeeder
{
    private const int MapSize = 100;
    private const int PlayerCount = 10;

    private static readonly string[] NamePrefixes =
    [
        "Iron", "Storm", "Shadow", "Golden", "Frost", "Wild", "Silent", "Bright", "Dark", "Swift",
    ];

    private static readonly string[] NameSuffixes =
    [
        "Wolf", "Hawk", "Blade", "Crown", "Forge", "Star", "Oak", "Fang", "Keep", "Raven",
    ];

    public static async Task RunAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        await ResetWorldAsync(db, cancellationToken);

        var random = Random.Shared;
        var occupiedTiles = new HashSet<(int X, int Y)>();

        for (var i = 0; i < PlayerCount; i++)
        {
            var playerName = BuildPlayerName(random, i);
            var playerType = random.Next(2) == 0 ? PlayerType.Human : PlayerType.Llm;
            var (x, y) = PickUniqueTile(random, occupiedTiles);

            var playerId = Guid.NewGuid();
            var cityId = Guid.NewGuid();

            var player = new Player
            {
                Id = playerId,
                Name = playerName,
                PlayerType = playerType,
            };

            var city = new City
            {
                Id = cityId,
                PlayerId = playerId,
                X = x,
                Y = y,
                Name = BuildCityName(playerName),
                Wood = random.Next(200, 5000),
                Stone = random.Next(200, 5000),
                Gold = random.Next(100, 3000),
                Food = random.Next(200, 4000),
                TroopCount = random.Next(10, 250),
            };

            db.Players.Add(player);
            db.Cities.Add(city);

            Console.WriteLine(
                $"  {playerName,-16} ({(playerType == PlayerType.Human ? "human" : "llm"),-5}) " +
                $"city @ ({x,2},{y,2})  troops={city.TroopCount,3}  " +
                $"W={city.Wood} S={city.Stone} G={city.Gold} F={city.Food}");
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task ResetWorldAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        await db.Actions.ExecuteDeleteAsync(cancellationToken);
        await db.Cities.ExecuteDeleteAsync(cancellationToken);
        await db.Players.ExecuteDeleteAsync(cancellationToken);

        var worldState = await db.WorldState.FirstOrDefaultAsync(state => state.Id == 1, cancellationToken);
        if (worldState is null)
        {
            db.WorldState.Add(new WorldState
            {
                Id = 1,
                CurrentTick = 0,
            });
        }
        else
        {
            worldState.CurrentTick = 0;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static string BuildPlayerName(Random random, int index)
    {
        var prefix = NamePrefixes[random.Next(NamePrefixes.Length)];
        var suffix = NameSuffixes[(index + random.Next(NameSuffixes.Length)) % NameSuffixes.Length];
        var name = $"{prefix}{suffix}";

        return name.Length > 30 ? name[..30] : name;
    }

    private static string BuildCityName(string playerName)
    {
        const string prefix = "City of ";
        var maxPlayerNameLength = 30 - prefix.Length;
        var trimmedName = playerName.Length > maxPlayerNameLength
            ? playerName[..maxPlayerNameLength]
            : playerName;

        return prefix + trimmedName;
    }

    private static (int X, int Y) PickUniqueTile(Random random, HashSet<(int X, int Y)> occupiedTiles)
    {
        for (var attempt = 0; attempt < 500; attempt++)
        {
            var x = random.Next(MapSize);
            var y = random.Next(MapSize);
            if (occupiedTiles.Add((x, y)))
            {
                return (x, y);
            }
        }

        throw new InvalidOperationException("Could not find a free tile on the map.");
    }
}
