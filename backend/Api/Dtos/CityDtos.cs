namespace llmmo.Api.Dtos;

public record CityFullDto(
    Guid Id,
    Guid PlayerId,
    int X,
    int Y,
    string Name,
    int Wood,
    int Stone,
    int Gold,
    int Food,
    int TroopCount,
    IReadOnlyList<BuildingDto> Buildings,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CityMapDto(
    Guid Id,
    Guid PlayerId,
    int X,
    int Y);

public record CityPublicDto(
    Guid Id,
    Guid PlayerId,
    int X,
    int Y,
    string Name);
