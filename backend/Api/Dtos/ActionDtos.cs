using System.Text.Json;

namespace llmmo.Api.Dtos;

public record CreateActionRequest(
    Guid CityId,
    string Type,
    object Payload);

public record ActionCreatedDto(
    Guid Id,
    Guid CityId,
    Guid PlayerId,
    string Type,
    string Status,
    int SubmittedAtTick,
    int? ReadyAtTick,
    int DurationTicks);

public record ActionListDto(
    Guid Id,
    Guid CityId,
    Guid PlayerId,
    string Type,
    string Status,
    int SubmittedAtTick,
    int? ReadyAtTick,
    int DurationTicks,
    JsonElement Payload,
    DateTime CreatedAt);

public record LlmActionFeedItemDto(
    Guid Id,
    string Type,
    string Status,
    int SubmittedAtTick,
    int? ReadyAtTick,
    int DurationTicks,
    JsonElement Payload,
    DateTime CreatedAt,
    Guid PlayerId,
    string PlayerName,
    Guid CityId,
    string CityName,
    int CityX,
    int CityY);
