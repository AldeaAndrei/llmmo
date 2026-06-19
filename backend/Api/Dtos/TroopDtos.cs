namespace llmmo.Api.Dtos;

public record TroopCatalogDto(
    string Type,
    string Name,
    int CapacityWood,
    int CapacityStone,
    int CapacityGold,
    int CapacityFood,
    int AttackMelee,
    int AttackRange,
    int Upkeep,
    int Speed,
    bool CanScout,
    bool IsCombat,
    string TrainAtBuilding,
    BuildingUpgradeCostDto TrainCostPerUnit);

public record CityTroopDto(
    string Type,
    string Name,
    int Quantity,
    int CapacityWood,
    int CapacityStone,
    int CapacityGold,
    int CapacityFood,
    int AttackMelee,
    int AttackRange,
    int Upkeep,
    int Speed,
    bool CanScout,
    bool IsCombat,
    bool Trainable,
    BuildingUpgradeCostDto? TrainCostPerUnit);

public record TroopStackEntryDto(string Type, int Count);

public record CreateAttackRequest(
    Guid SourceCityId,
    Guid? TargetCityId,
    int? TargetX,
    int? TargetY,
    string Type,
    IReadOnlyList<TroopStackEntryDto> Troops);

public record AttackPreviewDto(
    bool Valid,
    IReadOnlyList<string> Errors,
    int Manhattan,
    int PartySpeed,
    int OutboundTicks,
    int ReturnTicks,
    int ArrivesAtTick,
    int ReturnsAtTick);

public record AttackMapDto(
    Guid Id,
    string Type,
    string Status,
    string Phase,
    double Progress,
    int CurrentX,
    int CurrentY,
    AttackLocationDto Source,
    AttackLocationDto Target,
    IReadOnlyList<TroopStackEntryDto> Troops);

public record AttackMovementDto(
    Guid Id,
    string Type,
    string Status,
    string Phase,
    string Direction,
    AttackLocationDto Source,
    AttackLocationDto Target,
    IReadOnlyList<TroopStackEntryDto> Troops,
    int RemainingTicks);

public record TroopMovementsDto(
    IReadOnlyList<AttackMovementDto> Outgoing,
    IReadOnlyList<AttackMovementDto> Incoming);

public record AttackLocationDto(int X, int Y, Guid? CityId);

public record AttackCreatedDto(
    Guid Id,
    string Type,
    string Status,
    int OutboundTicks,
    int ReturnTicks,
    int DepartedAtTick,
    int ArrivesAtTick,
    IReadOnlyList<TroopStackEntryDto> Troops);

public record ReportDto(
    Guid Id,
    string Type,
    Guid? AttackId,
    Guid SourceCityId,
    Guid? TargetCityId,
    int TargetX,
    int TargetY,
    object Payload,
    DateTime CreatedAt,
    DateTime? ReadAt);

public record CityResourcesDto(int Wood, int Stone, int Gold, int Food);

public record CityVisibilityDto(
    Guid Id,
    Guid PlayerId,
    int X,
    int Y,
    string Name,
    string Visibility,
    CityResourcesDto? Resources,
    IReadOnlyList<TroopStackEntryDto>? Troops,
    double SpyDieChance);
