using llmmo.Api;
using llmmo.Api.Dtos;
using llmmo.Auth;
using llmmo.Data;
using llmmo.Entities;
using Microsoft.EntityFrameworkCore;

namespace llmmo.Auth;

public class AgentManagementService
{
    private readonly AppDbContext _db;

    public AgentManagementService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<AgentSummaryDto>> ListAgentsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var agents = await _db.Players
            .AsNoTracking()
            .Where(p => p.OwnerUserId == userId && p.PlayerType == PlayerType.Llm)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        var result = new List<AgentSummaryDto>();
        foreach (var agent in agents)
        {
            var summary = await BuildSummaryAsync(agent, cancellationToken);
            if (summary is not null)
            {
                result.Add(summary);
            }
        }

        return result;
    }

    public async Task<AgentSummaryDto?> GetAgentAsync(Guid userId, Guid playerId, CancellationToken cancellationToken)
    {
        var agent = await FindOwnedAgentAsync(userId, playerId, cancellationToken);
        return agent is null ? null : await BuildSummaryAsync(agent, cancellationToken);
    }

    public async Task<(CreateAgentResponseDto Response, string ApiKey)?> CreateAgentAsync(
        Guid userId,
        CreateAgentRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 30)
        {
            return null;
        }

        var x = request.X ?? 50;
        var y = request.Y ?? 50;

        if (x < 0 || y < 0)
        {
            return null;
        }

        var tileTaken = await _db.Cities.AnyAsync(c => c.X == x && c.Y == y, cancellationToken);
        if (tileTaken)
        {
            return null;
        }

        var playerId = Guid.NewGuid();
        var cityId = Guid.NewGuid();
        var name = request.Name.Trim();
        var label = string.IsNullOrWhiteSpace(request.Label) ? name : request.Label.Trim()[..Math.Min(request.Label.Trim().Length, 64)];

        var player = new Player
        {
            Id = playerId,
            OwnerUserId = userId,
            Name = name,
            PlayerType = PlayerType.Llm,
        };

        var city = CitySetup.CreateStartingCity(cityId, playerId, name, x, y);
        var (apiKey, keyEntity) = CreateApiKeyEntity(playerId, label);

        _db.Players.Add(player);
        _db.Cities.Add(city);
        _db.ApiKeys.Add(keyEntity);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return null;
        }

        var summary = await BuildSummaryAsync(player, cancellationToken);
        if (summary is null)
        {
            return null;
        }

        return (new CreateAgentResponseDto(apiKey, summary), apiKey);
    }

    public async Task<(ReissueKeyResponseDto Response, string ApiKey)?> ReissueKeyAsync(
        Guid userId,
        Guid playerId,
        CancellationToken cancellationToken)
    {
        var agent = await FindOwnedAgentAsync(userId, playerId, cancellationToken);
        if (agent is null)
        {
            return null;
        }

        var activeKeys = await _db.ApiKeys
            .Where(k => k.PlayerId == playerId && k.RevokedAt == null)
            .ToListAsync(cancellationToken);

        var utcNow = DateTime.UtcNow;
        foreach (var key in activeKeys)
        {
            key.RevokedAt = utcNow;
        }

        var label = activeKeys.FirstOrDefault()?.Label ?? agent.Name;
        var (apiKey, keyEntity) = CreateApiKeyEntity(playerId, label);
        _db.ApiKeys.Add(keyEntity);
        await _db.SaveChangesAsync(cancellationToken);

        var summary = await BuildSummaryAsync(agent, cancellationToken);
        if (summary is null)
        {
            return null;
        }

        return (new ReissueKeyResponseDto(apiKey, summary), apiKey);
    }

    public async Task<bool> RevokeKeyAsync(Guid userId, Guid playerId, CancellationToken cancellationToken)
    {
        var agent = await FindOwnedAgentAsync(userId, playerId, cancellationToken);
        if (agent is null)
        {
            return false;
        }

        var activeKeys = await _db.ApiKeys
            .Where(k => k.PlayerId == playerId && k.RevokedAt == null)
            .ToListAsync(cancellationToken);

        if (activeKeys.Count == 0)
        {
            return false;
        }

        var utcNow = DateTime.UtcNow;
        foreach (var key in activeKeys)
        {
            key.RevokedAt = utcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAgentAsync(Guid userId, Guid playerId, CancellationToken cancellationToken)
    {
        var agent = await FindOwnedAgentAsync(userId, playerId, cancellationToken);
        if (agent is null)
        {
            return false;
        }

        await RevokeKeyAsync(userId, playerId, cancellationToken);
        return true;
    }

    private async Task<Player?> FindOwnedAgentAsync(Guid userId, Guid playerId, CancellationToken cancellationToken)
    {
        return await _db.Players
            .FirstOrDefaultAsync(
                p => p.Id == playerId && p.OwnerUserId == userId && p.PlayerType == PlayerType.Llm,
                cancellationToken);
    }

    private async Task<AgentSummaryDto?> BuildSummaryAsync(Player agent, CancellationToken cancellationToken)
    {
        var city = await _db.Cities.AsNoTracking()
            .FirstOrDefaultAsync(c => c.PlayerId == agent.Id, cancellationToken);

        if (city is null)
        {
            return null;
        }

        var activeKey = await _db.ApiKeys.AsNoTracking()
            .Where(k => k.PlayerId == agent.Id)
            .OrderByDescending(k => k.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var keyStatus = activeKey switch
        {
            null => "none",
            { RevokedAt: not null } => "revoked",
            _ => "active",
        };

        return new AgentSummaryDto(
            agent.Id,
            agent.Name,
            activeKey?.Label ?? agent.Name,
            city.Id,
            city.Name,
            city.X,
            city.Y,
            activeKey?.KeyPrefix ?? "",
            keyStatus,
            activeKey?.LastUsedAt,
            agent.CreatedAt);
    }

    private static (string Plaintext, ApiKey Entity) CreateApiKeyEntity(Guid playerId, string label)
    {
        var (apiKey, prefix) = ApiKeyHasher.GenerateKeyPair();

        var entity = new ApiKey
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            KeyHash = ApiKeyHasher.Hash(apiKey),
            KeyPrefix = prefix,
            Label = label,
        };

        return (apiKey, entity);
    }
}
