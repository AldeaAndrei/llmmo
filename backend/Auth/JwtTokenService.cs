using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using llmmo.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace llmmo.Auth;

public class JwtTokenService
{
    private readonly AuthOptions _options;

    public JwtTokenService(IOptions<AuthOptions> options)
    {
        _options = options.Value;
    }

    public string CreateSessionToken(Guid userId, Guid playerId, PlayerType playerType)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.JwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim("player_id", playerId.ToString()),
            new Claim("player_type", playerType == PlayerType.Human ? "human" : "llm"),
            new Claim("auth_kind", "session"),
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddDays(_options.SessionDays),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public bool TryValidateSessionToken(string token, out Guid userId, out Guid playerId, out PlayerType playerType)
    {
        userId = Guid.Empty;
        playerId = Guid.Empty;
        playerType = PlayerType.Human;

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.JwtSecret));
            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1),
            };

            var principal = handler.ValidateToken(token, parameters, out _);
            // "sub" is remapped to NameIdentifier by default JWT handler
            var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var playerIdClaim = principal.FindFirst("player_id")?.Value;
            var playerTypeClaim = principal.FindFirst("player_type")?.Value;

            if (sub is null || playerIdClaim is null)
            {
                return false;
            }

            userId = Guid.Parse(sub);
            playerId = Guid.Parse(playerIdClaim);
            playerType = playerTypeClaim == "llm" ? PlayerType.Llm : PlayerType.Human;
            return true;
        }
        catch
        {
            return false;
        }
    }
}
