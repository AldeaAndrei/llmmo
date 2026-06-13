using llmmo.Data;
using llmmo.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace llmmo.Auth;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly JwtTokenService _jwt;
    private readonly AuthOptions _options;

    public AuthService(AppDbContext db, JwtTokenService jwt, IOptions<AuthOptions> options)
    {
        _db = db;
        _jwt = jwt;
        _options = options.Value;
    }

    public async Task<PlayerAuthContext?> ResolveAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        if (TryGetBearerKey(httpContext, out var apiKey))
        {
            return await ResolveApiKeyAsync(apiKey, cancellationToken);
        }

        if (httpContext.Request.Cookies.TryGetValue(_options.CookieName, out var cookieToken)
            && !string.IsNullOrWhiteSpace(cookieToken)
            && _jwt.TryValidateSessionToken(cookieToken, out var userId, out var playerId, out var playerType))
        {
            var player = await _db.Players.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == playerId && p.OwnerUserId == userId, cancellationToken);

            if (player is null)
            {
                return null;
            }

            return new PlayerAuthContext
            {
                UserId = userId,
                PlayerId = player.Id,
                PlayerName = player.Name,
                PlayerType = player.PlayerType,
                AuthKind = AuthKind.Session,
            };
        }

        return null;
    }

    public void SetSessionCookie(HttpContext httpContext, string token)
    {
        httpContext.Response.Cookies.Append(_options.CookieName, token, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = httpContext.Request.IsHttps,
            Expires = DateTimeOffset.UtcNow.AddDays(_options.SessionDays),
            Path = "/",
        });
    }

    public void ClearSessionCookie(HttpContext httpContext)
    {
        httpContext.Response.Cookies.Delete(_options.CookieName, new CookieOptions
        {
            Path = "/",
        });
    }

    private static bool TryGetBearerKey(HttpContext httpContext, out string apiKey)
    {
        apiKey = string.Empty;
        var header = httpContext.Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        apiKey = header["Bearer ".Length..].Trim();
        return apiKey.StartsWith("llmmo_", StringComparison.Ordinal);
    }

    private async Task<PlayerAuthContext?> ResolveApiKeyAsync(string apiKey, CancellationToken cancellationToken)
    {
        var hash = ApiKeyHasher.Hash(apiKey);

        var keyRow = await _db.ApiKeys
            .Include(k => k.Player)
            .FirstOrDefaultAsync(k => k.KeyHash == hash && k.RevokedAt == null, cancellationToken);

        if (keyRow?.Player is null || keyRow.Player.OwnerUserId is null)
        {
            return null;
        }

        keyRow.LastUsedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return new PlayerAuthContext
        {
            UserId = keyRow.Player.OwnerUserId.Value,
            PlayerId = keyRow.PlayerId,
            PlayerName = keyRow.Player.Name,
            PlayerType = keyRow.Player.PlayerType,
            AuthKind = AuthKind.ApiKey,
        };
    }
}
