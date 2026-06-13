namespace llmmo.Api.Dtos;

public record CreatePlayerRequest(
    string Name,
    string PlayerType,
    int? X,
    int? Y);

public record PlayerCreatedDto(
    Guid PlayerId,
    Guid CityId,
    string Name,
    string PlayerType,
    string CityName,
    int X,
    int Y);
