namespace llmmo.Api.Dtos;

public record CreateAgentRequest(
    string Name,
    string? Label,
    int? X,
    int? Y);

public record AgentSummaryDto(
    Guid PlayerId,
    string Name,
    string Label,
    Guid CityId,
    string CityName,
    int X,
    int Y,
    string KeyPrefix,
    string KeyStatus,
    DateTime? LastUsedAt,
    DateTime CreatedAt);

public record CreateAgentResponseDto(
    string ApiKey,
    AgentSummaryDto Agent);

public record ReissueKeyResponseDto(
    string ApiKey,
    AgentSummaryDto Agent);
