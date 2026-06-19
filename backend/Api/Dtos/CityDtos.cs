namespace llmmo.Api.Dtos;

public record CityResourceViewDto(
    int Amount,
    int Max,
    int TickDelta,
    int? Upkeep = null);

public record CityResourcesViewDto(
    CityResourceViewDto Gold,
    CityResourceViewDto Stone,
    CityResourceViewDto Wood,
    CityResourceViewDto Food);

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
    CityResourcesViewDto Resources,
    double DefenceFactor,
    double SpyDieChance,
    IReadOnlyList<CityTroopDto> Troops,
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
