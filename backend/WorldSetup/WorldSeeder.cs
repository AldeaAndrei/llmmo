using llmmo.Api;
using llmmo.Api.Buildings;
using llmmo.Data;
using llmmo.Entities;
using Microsoft.EntityFrameworkCore;

namespace llmmo.WorldSetup;

public static class WorldSeeder
{
    private const int MapSize = 100;
    private const int LlmPlayerCount = 10;

    private const string AdminEmail = "admin@yahoo.com";
    private const string AdminPassword = "test1234";
    private const string AdminPlayerName = "Admin";

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

        Console.WriteLine("Seeding LLM cities (0 resources, buildings level 1)...");

        for (var i = 0; i < LlmPlayerCount; i++)
        {
            var playerName = BuildPlayerName(random, i);
            var (x, y) = PickUniqueTile(random, occupiedTiles);

            var playerId = Guid.NewGuid();
            var cityId = Guid.NewGuid();

            var player = new Player
            {
                Id = playerId,
                Name = playerName,
                PlayerType = PlayerType.Llm,
            };

            var city = CreateFreshCity(cityId, playerId, playerName, x, y);

            db.Players.Add(player);
            db.Cities.Add(city);
            db.Buildings.AddRange(BuildingSetup.CreateDefaults(cityId));

            Console.WriteLine(
                $"  {playerName,-16} llm    city @ ({x,2},{y,2})  troops=0  W=0 S=0 G=0 F=0");
        }

        Console.WriteLine();
        Console.WriteLine("Seeding default admin user...");

        var adminTile = PickUniqueTile(random, occupiedTiles);
        var adminUserId = Guid.NewGuid();
        var adminPlayerId = Guid.NewGuid();
        var adminCityId = Guid.NewGuid();

        var adminUser = new User
        {
            Id = adminUserId,
            Email = AdminEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(AdminPassword),
        };

        var adminPlayer = new Player
        {
            Id = adminPlayerId,
            OwnerUserId = adminUserId,
            Name = AdminPlayerName,
            PlayerType = PlayerType.Human,
        };

        var adminCity = CitySetup.CreateStartingCity(
            adminCityId,
            adminPlayerId,
            AdminPlayerName,
            adminTile.X,
            adminTile.Y);

        db.Users.Add(adminUser);
        db.Players.Add(adminPlayer);
        db.Cities.Add(adminCity);
        db.Buildings.AddRange(BuildingSetup.CreateDefaults(adminCityId));

        Console.WriteLine(
            $"  {AdminEmail} / {AdminPlayerName} @ ({adminTile.X,2},{adminTile.Y,2})  " +
            $"W={adminCity.Wood} S={adminCity.Stone} G={adminCity.Gold} F={adminCity.Food} troops={adminCity.TroopCount}");

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task ResetWorldAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        Console.WriteLine("Resetting world...");

        await db.Actions.ExecuteDeleteAsync(cancellationToken);
        await db.ApiKeys.ExecuteDeleteAsync(cancellationToken);
        await db.Buildings.ExecuteDeleteAsync(cancellationToken);
        await db.Cities.ExecuteDeleteAsync(cancellationToken);
        await db.Players.ExecuteDeleteAsync(cancellationToken);
        await db.Users.ExecuteDeleteAsync(cancellationToken);

        var utcNow = DateTime.UtcNow;
        var worldSeed = Random.Shared.Next(1, int.MaxValue);
        var worldState = await db.WorldState.FirstOrDefaultAsync(state => state.Id == 1, cancellationToken);
        if (worldState is null)
        {
            db.WorldState.Add(new WorldState
            {
                Id = 1,
                CurrentTick = 0,
                WorldSeed = worldSeed,
                MapSize = MapSize,
                LastTickAt = utcNow,
            });
        }
        else
        {
            worldState.CurrentTick = 0;
            worldState.WorldSeed = worldSeed;
            worldState.MapSize = MapSize;
            worldState.LastTickAt = utcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static City CreateFreshCity(Guid cityId, Guid playerId, string playerName, int x, int y) => new()
    {
        Id = cityId,
        PlayerId = playerId,
        X = x,
        Y = y,
        Name = CitySetup.BuildCityName(playerName),
        Wood = 0,
        Stone = 0,
        Gold = 0,
        Food = 0,
        TroopCount = 0,
    };

    private static string BuildPlayerName(Random random, int index)
    {
        var prefix = NamePrefixes[random.Next(NamePrefixes.Length)];
        var suffix = NameSuffixes[(index + random.Next(NameSuffixes.Length)) % NameSuffixes.Length];
        var name = $"{prefix}{suffix}";

        return name.Length > 30 ? name[..30] : name;
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
