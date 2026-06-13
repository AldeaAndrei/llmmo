namespace llmmo.Api.Dtos;

public record CreateActionRequest(
    Guid PlayerId,
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
