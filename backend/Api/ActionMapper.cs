using System.Text.Json;
using llmmo.Api.Dtos;
using llmmo.Entities;

namespace llmmo.Api;

public static class ActionMapper
{
    public static ActionListDto ToListDto(GameAction action) => new(
        action.Id,
        action.CityId,
        action.PlayerId,
        action.Type,
        StatusToString(action.Status),
        action.SubmittedAtTick,
        action.ReadyAtTick,
        action.DurationTicks,
        JsonSerializer.Deserialize<JsonElement>(action.Payload),
        action.CreatedAt);

    public static ActionCreatedDto ToCreatedDto(GameAction action) => new(
        action.Id,
        action.CityId,
        action.PlayerId,
        action.Type,
        StatusToString(action.Status),
        action.SubmittedAtTick,
        action.ReadyAtTick,
        action.DurationTicks);

    public static LlmActionFeedItemDto ToLlmFeedDto(GameAction action) => new(
        action.Id,
        action.Type,
        StatusToString(action.Status),
        action.SubmittedAtTick,
        action.ReadyAtTick,
        action.DurationTicks,
        JsonSerializer.Deserialize<JsonElement>(action.Payload),
        action.CreatedAt,
        action.PlayerId,
        action.Player.Name,
        action.CityId,
        action.City.Name,
        action.City.X,
        action.City.Y);

    private static string StatusToString(ActionStatus status) => status switch
    {
        ActionStatus.Queued => "queued",
        ActionStatus.InProgress => "in_progress",
        ActionStatus.Done => "done",
        ActionStatus.Failed => "failed",
        _ => "queued",
    };
}
