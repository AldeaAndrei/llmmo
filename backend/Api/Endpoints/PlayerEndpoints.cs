using llmmo.Api.Dtos;
using llmmo.Data;
using llmmo.Entities;
using Microsoft.EntityFrameworkCore;

namespace llmmo.Api.Endpoints;

public static class PlayerEndpoints
{
    public static RouteGroupBuilder MapPlayerEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/players", CreatePlayer);
        return group;
    }

    private static async Task<IResult> CreatePlayer(
        CreatePlayerRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        // TODO: auth — resolve playerId from session; reject unauthenticated

        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 30)
        {
            return Results.BadRequest(new { error = "Name is required and must be at most 30 characters." });
        }

        if (!TryParsePlayerType(request.PlayerType, out var playerType))
        {
            return Results.BadRequest(new { error = "PlayerType must be 'human' or 'llm'." });
        }

        var x = request.X ?? 50;
        var y = request.Y ?? 50;

        if (x < 0 || y < 0)
        {
            return Results.BadRequest(new { error = "Coordinates must be non-negative." });
        }

        var tileTaken = await db.Cities.AnyAsync(city => city.X == x && city.Y == y, cancellationToken);
        if (tileTaken)
        {
            return Results.Conflict(new { error = "Tile is already occupied." });
        }

        var playerId = Guid.NewGuid();
        var cityId = Guid.NewGuid();
        var cityName = BuildCityName(request.Name.Trim());

        var player = new Player
        {
            Id = playerId,
            Name = request.Name.Trim(),
            PlayerType = playerType,
        };

        var city = new City
        {
            Id = cityId,
            PlayerId = playerId,
            X = x,
            Y = y,
            Name = cityName,
            Wood = 500,
            Stone = 500,
            Gold = 250,
            Food = 400,
            TroopCount = 25,
        };

        db.Players.Add(player);
        db.Cities.Add(city);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Results.Conflict(new { error = "Tile is already occupied." });
        }

        return Results.Created(
            $"/api/v1/cities/{cityId}",
            new PlayerCreatedDto(
                playerId,
                cityId,
                player.Name,
                playerType == PlayerType.Human ? "human" : "llm",
                cityName,
                x,
                y));
    }

    private static bool TryParsePlayerType(string value, out PlayerType playerType)
    {
        playerType = PlayerType.Human;
        if (string.Equals(value, "human", StringComparison.OrdinalIgnoreCase))
        {
            playerType = PlayerType.Human;
            return true;
        }

        if (string.Equals(value, "llm", StringComparison.OrdinalIgnoreCase))
        {
            playerType = PlayerType.Llm;
            return true;
        }

        return false;
    }

    private static string BuildCityName(string playerName)
    {
        var prefix = "City of ";
        var maxPlayerNameLength = 30 - prefix.Length;
        var trimmedName = playerName.Length > maxPlayerNameLength
            ? playerName[..maxPlayerNameLength]
            : playerName;

        return prefix + trimmedName;
    }
}
