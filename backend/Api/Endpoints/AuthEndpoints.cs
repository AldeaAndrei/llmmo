using llmmo.Api.Buildings;
using llmmo.Api;
using llmmo.Api.Dtos;
using llmmo.Auth;
using llmmo.Data;
using llmmo.Entities;
using Microsoft.EntityFrameworkCore;

namespace llmmo.Api.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder group)
    {
        var auth = group.MapGroup("/auth");

        auth.MapPost("/register", Register);
        auth.MapPost("/login", Login);
        auth.MapPost("/logout", Logout).RequireAuth();
        auth.MapGet("/me", GetMe).RequireAuth();

        return group;
    }

    private static async Task<IResult> Register(
        RegisterRequest request,
        HttpContext httpContext,
        AppDbContext db,
        AuthService authService,
        JwtTokenService jwt,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
        {
            return Results.BadRequest(new { error = "Valid email is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            return Results.BadRequest(new { error = "Password must be at least 8 characters." });
        }

        if (string.IsNullOrWhiteSpace(request.PlayerName) || request.PlayerName.Length > 30)
        {
            return Results.BadRequest(new { error = "PlayerName is required and must be at most 30 characters." });
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var emailTaken = await db.Users.AnyAsync(u => u.Email == email, cancellationToken);
        if (emailTaken)
        {
            return Results.Conflict(new { error = "Email is already registered." });
        }

        var x = request.X ?? 50;
        var y = request.Y ?? 50;

        if (x < 0 || y < 0)
        {
            return Results.BadRequest(new { error = "Coordinates must be non-negative." });
        }

        var tileTaken = await db.Cities.AnyAsync(c => c.X == x && c.Y == y, cancellationToken);
        if (tileTaken)
        {
            return Results.Conflict(new { error = "Tile is already occupied." });
        }

        var userId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var cityId = Guid.NewGuid();
        var playerName = request.PlayerName.Trim();

        var user = new User
        {
            Id = userId,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
        };

        var player = new Player
        {
            Id = playerId,
            OwnerUserId = userId,
            Name = playerName,
            PlayerType = PlayerType.Human,
        };

        var city = CitySetup.CreateStartingCity(cityId, playerId, playerName, x, y);

        db.Users.Add(user);
        db.Players.Add(player);
        db.Cities.Add(city);
        CityBootstrap.AddDefaults(db, cityId, soldierCount: CitySetup.StarterSoldiers);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Results.Conflict(new { error = "Registration failed. Email or tile may be taken." });
        }

        var token = jwt.CreateSessionToken(userId, playerId, PlayerType.Human);
        authService.SetSessionCookie(httpContext, token);

        return Results.Created("/api/v1/auth/me", new RegisterResponseDto(userId, playerId, cityId, playerName, email));
    }

    private static async Task<IResult> Login(
        LoginRequest request,
        HttpContext httpContext,
        AppDbContext db,
        AuthService authService,
        JwtTokenService jwt,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new { error = "Email and password are required." });
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Results.Unauthorized();
        }

        var player = await db.Players
            .FirstOrDefaultAsync(
                p => p.OwnerUserId == user.Id && p.PlayerType == PlayerType.Human,
                cancellationToken);

        if (player is null)
        {
            return Results.Problem("Human player not found for account.", statusCode: StatusCodes.Status500InternalServerError);
        }

        var token = jwt.CreateSessionToken(user.Id, player.Id, PlayerType.Human);
        authService.SetSessionCookie(httpContext, token);

        return Results.Ok(new AuthMeDto(
            user.Id,
            player.Id,
            player.Name,
            "human",
            user.Email));
    }

    private static Task<IResult> Logout(HttpContext httpContext, AuthService authService)
    {
        authService.ClearSessionCookie(httpContext);
        return Task.FromResult(Results.NoContent());
    }

    private static async Task<IResult> GetMe(
        HttpContext httpContext,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var auth = httpContext.GetPlayerAuth();
        if (auth is null || !auth.IsAuthenticated)
        {
            return Results.Unauthorized();
        }

        var email = await db.Users.AsNoTracking()
            .Where(u => u.Id == auth.UserId)
            .Select(u => u.Email)
            .FirstOrDefaultAsync(cancellationToken) ?? "";

        return Results.Ok(new AuthMeDto(
            auth.UserId,
            auth.PlayerId,
            auth.PlayerName,
            auth.PlayerType == PlayerType.Human ? "human" : "llm",
            email));
    }
}
