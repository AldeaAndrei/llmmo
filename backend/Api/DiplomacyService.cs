using llmmo.Api.Dtos;
using llmmo.Data;
using llmmo.Entities;
using Microsoft.EntityFrameworkCore;

namespace llmmo.Api;

public sealed class DiplomacyCooldownException : Exception
{
    public DiplomacyCooldownException(string kind, int remainingTicks, int allowedAtTick)
        : base($"{kind} cooldown active.")
    {
        Kind = kind;
        RemainingTicks = remainingTicks;
        AllowedAtTick = allowedAtTick;
    }

    public string Kind { get; }

    public int RemainingTicks { get; }

    public int AllowedAtTick { get; }
}

public class DiplomacyService
{
    public const int CooldownTicks = 30;

    private readonly AppDbContext _db;

    public DiplomacyService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<int> GetCurrentTickAsync(CancellationToken cancellationToken)
    {
        var worldState = await _db.WorldState.AsNoTracking()
            .FirstOrDefaultAsync(state => state.Id == 1, cancellationToken);

        return worldState?.CurrentTick ?? 0;
    }

    public DiplomacyCooldownsDto BuildCooldowns(Player player, int currentTick)
    {
        return new DiplomacyCooldownsDto(
            BuildCooldown(player.LastMessageSentAtTick, currentTick),
            BuildCooldown(player.LastDiplomacyDeclaredAtTick, currentTick));
    }

    public async Task<DiplomacyCooldownsDto> GetCooldownStatusAsync(
        Guid playerId,
        CancellationToken cancellationToken)
    {
        var currentTick = await GetCurrentTickAsync(cancellationToken);
        var player = await _db.Players.AsNoTracking()
            .FirstAsync(player => player.Id == playerId, cancellationToken);

        return BuildCooldowns(player, currentTick);
    }

    public async Task<IReadOnlyList<DiplomacyPlayerDto>> ListPlayersAsync(
        Guid authPlayerId,
        CancellationToken cancellationToken)
    {
        return await _db.Players.AsNoTracking()
            .Where(player => player.Id != authPlayerId)
            .OrderBy(player => player.Name)
            .Select(player => new DiplomacyPlayerDto(
                player.Id,
                player.Name,
                player.PlayerType == PlayerType.Human ? "human" : "llm"))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PlayerMessageDto>> ListMessagesAsync(
        Guid authPlayerId,
        CancellationToken cancellationToken)
    {
        var messages = await _db.PlayerMessages.AsNoTracking()
            .Include(message => message.FromPlayer)
            .Include(message => message.ToPlayer)
            .Where(message => message.FromPlayerId == authPlayerId || message.ToPlayerId == authPlayerId)
            .OrderByDescending(message => message.SentAt)
            .ToListAsync(cancellationToken);

        return messages.Select(ToMessageDto).ToList();
    }

    public async Task<PlayerMessageDto> SendMessageAsync(
        Guid authPlayerId,
        Guid toPlayerId,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        var (player, currentTick) = await LoadPlayerWithTickAsync(authPlayerId, cancellationToken);
        EnsureMessageCooldown(player, currentTick);

        var trimmedSubject = ValidateSubject(subject);
        var trimmedBody = ValidateBody(body);
        await EnsureTargetPlayerAsync(authPlayerId, toPlayerId, cancellationToken);

        var message = new PlayerMessage
        {
            Id = Guid.NewGuid(),
            FromPlayerId = authPlayerId,
            ToPlayerId = toPlayerId,
            Subject = trimmedSubject,
            Body = trimmedBody,
            SentAtTick = currentTick,
        };

        player.LastMessageSentAtTick = currentTick;
        _db.PlayerMessages.Add(message);
        await _db.SaveChangesAsync(cancellationToken);

        await _db.Entry(message).Reference(m => m.FromPlayer).LoadAsync(cancellationToken);
        await _db.Entry(message).Reference(m => m.ToPlayer).LoadAsync(cancellationToken);

        return ToMessageDto(message);
    }

    public async Task<PlayerMessageDto?> MarkMessageReadAsync(
        Guid authPlayerId,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        var message = await _db.PlayerMessages
            .Include(m => m.FromPlayer)
            .Include(m => m.ToPlayer)
            .FirstOrDefaultAsync(
                m => m.Id == messageId && m.ToPlayerId == authPlayerId,
                cancellationToken);

        if (message is null)
        {
            return null;
        }

        if (message.ReadAt is null)
        {
            message.ReadAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return ToMessageDto(message);
    }

    public async Task<IReadOnlyList<PlayerRelationDto>> ListRelationsAsync(
        Guid authPlayerId,
        CancellationToken cancellationToken)
    {
        var relations = await _db.PlayerRelations.AsNoTracking()
            .Include(relation => relation.ToPlayer)
            .Where(relation => relation.FromPlayerId == authPlayerId)
            .OrderBy(relation => relation.ToPlayer.Name)
            .ToListAsync(cancellationToken);

        return relations.Select(relation => new PlayerRelationDto(
            relation.ToPlayerId,
            relation.ToPlayer.Name,
            relation.ToPlayer.PlayerType == PlayerType.Human ? "human" : "llm",
            relation.Relation == DiplomacyRelationType.Ally ? "ally" : "enemy",
            relation.UpdatedAt,
            relation.UpdatedAtTick)).ToList();
    }

    public async Task<PlayerRelationDto> SetRelationAsync(
        Guid authPlayerId,
        Guid toPlayerId,
        string relationValue,
        CancellationToken cancellationToken)
    {
        var relationType = ParseRelation(relationValue);
        var (player, currentTick) = await LoadPlayerWithTickAsync(authPlayerId, cancellationToken);
        EnsureDiplomacyCooldown(player, currentTick);
        await EnsureTargetPlayerAsync(authPlayerId, toPlayerId, cancellationToken);

        await UpsertMutualRelationAsync(authPlayerId, toPlayerId, relationType, currentTick, cancellationToken);

        player.LastDiplomacyDeclaredAtTick = currentTick;

        var eventType = relationType == DiplomacyRelationType.Ally ? "ally" : "enemy";
        if (player.PlayerType == PlayerType.Llm)
        {
            _db.DiplomacyEvents.Add(new DiplomacyEvent
            {
                Id = Guid.NewGuid(),
                DeclaredByPlayerId = authPlayerId,
                TargetPlayerId = toPlayerId,
                EventType = eventType,
                CreatedAtTick = currentTick,
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        var target = await _db.Players.AsNoTracking()
            .FirstAsync(p => p.Id == toPlayerId, cancellationToken);

        var stored = await _db.PlayerRelations.AsNoTracking()
            .FirstAsync(r => r.FromPlayerId == authPlayerId && r.ToPlayerId == toPlayerId, cancellationToken);

        return new PlayerRelationDto(
            toPlayerId,
            target.Name,
            target.PlayerType == PlayerType.Human ? "human" : "llm",
            eventType,
            stored.UpdatedAt,
            stored.UpdatedAtTick);
    }

    public async Task ClearRelationAsync(
        Guid authPlayerId,
        Guid toPlayerId,
        CancellationToken cancellationToken)
    {
        var (player, currentTick) = await LoadPlayerWithTickAsync(authPlayerId, cancellationToken);
        EnsureDiplomacyCooldown(player, currentTick);
        await EnsureTargetPlayerAsync(authPlayerId, toPlayerId, cancellationToken);

        var rows = await _db.PlayerRelations
            .Where(relation =>
                (relation.FromPlayerId == authPlayerId && relation.ToPlayerId == toPlayerId) ||
                (relation.FromPlayerId == toPlayerId && relation.ToPlayerId == authPlayerId))
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            throw new InvalidOperationException("No relation exists with that player.");
        }

        _db.PlayerRelations.RemoveRange(rows);
        player.LastDiplomacyDeclaredAtTick = currentTick;

        if (player.PlayerType == PlayerType.Llm)
        {
            _db.DiplomacyEvents.Add(new DiplomacyEvent
            {
                Id = Guid.NewGuid(),
                DeclaredByPlayerId = authPlayerId,
                TargetPlayerId = toPlayerId,
                EventType = "clear_relation",
                CreatedAtTick = currentTick,
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PossibleDiplomacyActionsDto> BuildPossibleDiplomacyAsync(
        Guid authPlayerId,
        CancellationToken cancellationToken)
    {
        var currentTick = await GetCurrentTickAsync(cancellationToken);
        var player = await _db.Players
            .FirstAsync(p => p.Id == authPlayerId, cancellationToken);

        var cooldowns = BuildCooldowns(player, currentTick);
        var canSendMessage = cooldowns.Message.RemainingTicks == 0;
        var canDeclareDiplomacy = cooldowns.Diplomacy.RemainingTicks == 0;

        var allPlayers = await _db.Players.AsNoTracking()
            .Where(p => p.Id != authPlayerId)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        var myRelations = await _db.PlayerRelations.AsNoTracking()
            .Where(r => r.FromPlayerId == authPlayerId)
            .ToDictionaryAsync(r => r.ToPlayerId, r => r.Relation, cancellationToken);

        var players = allPlayers.Select(other => new DiplomacyOverviewRelationDto(
            other.Id,
            other.Name,
            other.PlayerType == PlayerType.Human ? "human" : "llm",
            myRelations.TryGetValue(other.Id, out var relation)
                ? relation == DiplomacyRelationType.Ally ? "ally" : "enemy"
                : null)).ToList();

        var latestUnread = await _db.PlayerMessages
            .Include(message => message.FromPlayer)
            .Where(message => message.ToPlayerId == authPlayerId && message.ReadAt == null)
            .OrderByDescending(message => message.SentAt)
            .FirstOrDefaultAsync(cancellationToken);

        DiplomacyOverviewMessageDto? latestUnreadDto = null;
        if (latestUnread is not null)
        {
            latestUnread.ReadAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            latestUnreadDto = new DiplomacyOverviewMessageDto(
                latestUnread.Id,
                latestUnread.FromPlayerId,
                latestUnread.FromPlayer.Name,
                latestUnread.Subject,
                latestUnread.Body,
                latestUnread.SentAt,
                latestUnread.SentAtTick);
        }

        return new PossibleDiplomacyActionsDto(
            players,
            canSendMessage,
            canDeclareDiplomacy,
            latestUnreadDto);
    }

    public async Task<DiplomacyOverviewDto> GetOverviewAsync(
        Guid authPlayerId,
        CancellationToken cancellationToken)
    {
        var currentTick = await GetCurrentTickAsync(cancellationToken);
        var player = await _db.Players.AsNoTracking()
            .FirstAsync(p => p.Id == authPlayerId, cancellationToken);

        var diplomacy = await BuildPossibleDiplomacyAsync(authPlayerId, cancellationToken);

        return new DiplomacyOverviewDto(
            diplomacy.Players,
            diplomacy.LatestUnreadMessage,
            BuildCooldowns(player, currentTick));
    }

    public async Task<IReadOnlyList<LlmFeedItemDto>> ListLlmDiplomacyFeedAsync(
        int limit,
        CancellationToken cancellationToken)
    {
        var take = Math.Clamp(limit, 1, 100);

        var messages = await _db.PlayerMessages.AsNoTracking()
            .Include(message => message.FromPlayer)
            .Include(message => message.ToPlayer)
            .Where(message => message.FromPlayer.PlayerType == PlayerType.Llm)
            .OrderByDescending(message => message.SentAt)
            .Take(take)
            .ToListAsync(cancellationToken);

        var events = await _db.DiplomacyEvents.AsNoTracking()
            .Include(diplomacyEvent => diplomacyEvent.DeclaredByPlayer)
            .Include(diplomacyEvent => diplomacyEvent.TargetPlayer)
            .Where(diplomacyEvent => diplomacyEvent.DeclaredByPlayer.PlayerType == PlayerType.Llm)
            .OrderByDescending(diplomacyEvent => diplomacyEvent.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

        var feed = new List<LlmFeedItemDto>();

        feed.AddRange(messages.Select(message => new LlmFeedItemDto(
            "diplomacy",
            message.Id,
            "message",
            null,
            message.SentAtTick,
            null,
            null,
            message.SentAt,
            message.FromPlayerId,
            message.FromPlayer.Name,
            null,
            null,
            null,
            null,
            new
            {
                toPlayerId = message.ToPlayerId,
                toPlayerName = message.ToPlayer.Name,
                subject = message.Subject,
                body = message.Body,
            })));

        feed.AddRange(events.Select(diplomacyEvent => new LlmFeedItemDto(
            "diplomacy",
            diplomacyEvent.Id,
            diplomacyEvent.EventType,
            null,
            diplomacyEvent.CreatedAtTick,
            null,
            null,
            diplomacyEvent.CreatedAt,
            diplomacyEvent.DeclaredByPlayerId,
            diplomacyEvent.DeclaredByPlayer.Name,
            null,
            null,
            null,
            null,
            new
            {
                targetPlayerId = diplomacyEvent.TargetPlayerId,
                targetPlayerName = diplomacyEvent.TargetPlayer.Name,
            })));

        return feed
            .OrderByDescending(item => item.SubmittedAtTick)
            .ThenByDescending(item => item.CreatedAt)
            .Take(take)
            .ToList();
    }

    private async Task<(Player Player, int CurrentTick)> LoadPlayerWithTickAsync(
        Guid authPlayerId,
        CancellationToken cancellationToken)
    {
        var currentTick = await GetCurrentTickAsync(cancellationToken);
        var player = await _db.Players
            .FirstAsync(p => p.Id == authPlayerId, cancellationToken);

        return (player, currentTick);
    }

    private async Task EnsureTargetPlayerAsync(
        Guid authPlayerId,
        Guid toPlayerId,
        CancellationToken cancellationToken)
    {
        if (toPlayerId == authPlayerId)
        {
            throw new InvalidOperationException("Cannot target yourself.");
        }

        var exists = await _db.Players.AsNoTracking()
            .AnyAsync(player => player.Id == toPlayerId, cancellationToken);

        if (!exists)
        {
            throw new InvalidOperationException("Target player not found.");
        }
    }

    private async Task UpsertMutualRelationAsync(
        Guid authPlayerId,
        Guid toPlayerId,
        DiplomacyRelationType relationType,
        int currentTick,
        CancellationToken cancellationToken)
    {
        await UpsertRelationRowAsync(authPlayerId, toPlayerId, relationType, currentTick, cancellationToken);
        await UpsertRelationRowAsync(toPlayerId, authPlayerId, relationType, currentTick, cancellationToken);
    }

    private async Task UpsertRelationRowAsync(
        Guid fromPlayerId,
        Guid toPlayerId,
        DiplomacyRelationType relationType,
        int currentTick,
        CancellationToken cancellationToken)
    {
        var existing = await _db.PlayerRelations
            .FirstOrDefaultAsync(
                relation => relation.FromPlayerId == fromPlayerId && relation.ToPlayerId == toPlayerId,
                cancellationToken);

        if (existing is null)
        {
            _db.PlayerRelations.Add(new PlayerRelation
            {
                FromPlayerId = fromPlayerId,
                ToPlayerId = toPlayerId,
                Relation = relationType,
                UpdatedAtTick = currentTick,
            });
        }
        else
        {
            existing.Relation = relationType;
            existing.UpdatedAtTick = currentTick;
        }
    }

    private static DiplomacyRelationType ParseRelation(string relationValue)
    {
        return relationValue.Trim().ToLowerInvariant() switch
        {
            "ally" => DiplomacyRelationType.Ally,
            "enemy" => DiplomacyRelationType.Enemy,
            _ => throw new InvalidOperationException("Relation must be 'ally' or 'enemy'."),
        };
    }

    private static string ValidateSubject(string subject)
    {
        var trimmed = subject.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            throw new InvalidOperationException("Subject is required.");
        }

        if (trimmed.Length > 100)
        {
            throw new InvalidOperationException("Subject must be at most 100 characters.");
        }

        return trimmed;
    }

    private static string ValidateBody(string body)
    {
        var trimmed = body.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            throw new InvalidOperationException("Message body is required.");
        }

        if (trimmed.Length > 500)
        {
            throw new InvalidOperationException("Message body must be at most 500 characters.");
        }

        return trimmed;
    }

    private static DiplomacyCooldownDto BuildCooldown(int? lastActionTick, int currentTick)
    {
        if (lastActionTick is null)
        {
            return new DiplomacyCooldownDto(0, currentTick);
        }

        var allowedAtTick = lastActionTick.Value + CooldownTicks;
        var remaining = Math.Max(0, allowedAtTick - currentTick);
        return new DiplomacyCooldownDto(remaining, allowedAtTick);
    }

    private static void EnsureMessageCooldown(Player player, int currentTick)
    {
        var cooldown = BuildCooldown(player.LastMessageSentAtTick, currentTick);
        if (cooldown.RemainingTicks > 0)
        {
            throw new DiplomacyCooldownException("message", cooldown.RemainingTicks, cooldown.AllowedAtTick);
        }
    }

    private static void EnsureDiplomacyCooldown(Player player, int currentTick)
    {
        var cooldown = BuildCooldown(player.LastDiplomacyDeclaredAtTick, currentTick);
        if (cooldown.RemainingTicks > 0)
        {
            throw new DiplomacyCooldownException("diplomacy", cooldown.RemainingTicks, cooldown.AllowedAtTick);
        }
    }

    private static PlayerMessageDto ToMessageDto(PlayerMessage message) => new(
        message.Id,
        message.FromPlayerId,
        message.FromPlayer.Name,
        message.ToPlayerId,
        message.ToPlayer.Name,
        message.Subject,
        message.Body,
        message.SentAt,
        message.SentAtTick,
        message.ReadAt);
}
