namespace llmmo.Api.Dtos;

public record DiplomacyCooldownDto(
    int RemainingTicks,
    int AllowedAtTick);

public record DiplomacyCooldownsDto(
    DiplomacyCooldownDto Message,
    DiplomacyCooldownDto Diplomacy);

public record DiplomacyPlayerDto(
    Guid Id,
    string Name,
    string PlayerType);

public record PlayerMessageDto(
    Guid Id,
    Guid FromPlayerId,
    string FromPlayerName,
    Guid ToPlayerId,
    string ToPlayerName,
    string Subject,
    string Body,
    DateTime SentAt,
    int SentAtTick,
    DateTime? ReadAt);

public record SendMessageRequest(
    Guid ToPlayerId,
    string Subject,
    string Body);

public record PlayerRelationDto(
    Guid OtherPlayerId,
    string OtherPlayerName,
    string OtherPlayerType,
    string Relation,
    DateTime UpdatedAt,
    int UpdatedAtTick);

public record SetRelationRequest(
    Guid ToPlayerId,
    string Relation);

public record DiplomacyOverviewRelationDto(
    Guid PlayerId,
    string Name,
    string PlayerType,
    string? Relation);

public record DiplomacyOverviewMessageDto(
    Guid Id,
    Guid FromPlayerId,
    string FromPlayerName,
    string Subject,
    string Body,
    DateTime SentAt,
    int SentAtTick);

public record DiplomacyOverviewDto(
    IReadOnlyList<DiplomacyOverviewRelationDto> Relations,
    DiplomacyOverviewMessageDto? LatestUnreadMessage,
    DiplomacyCooldownsDto Cooldowns);

public record LlmFeedItemDto(
    string Category,
    Guid Id,
    string Type,
    string? Status,
    int SubmittedAtTick,
    int? ReadyAtTick,
    int? DurationTicks,
    DateTime CreatedAt,
    Guid PlayerId,
    string PlayerName,
    Guid? CityId,
    string? CityName,
    int? CityX,
    int? CityY,
    object? Payload);
