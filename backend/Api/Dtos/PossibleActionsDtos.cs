namespace llmmo.Api.Dtos;

public record PossibleUpgradeDto(
    string BuildingType,
    int FromLevel,
    int ToLevel,
    BuildingUpgradeCostDto Cost);

public record PossibleTrainDto(
    string TroopType,
    int Count,
    BuildingUpgradeCostDto Cost);

public record PossibleTargetDto(
    Guid TargetCityId,
    Guid TargetPlayerId,
    string TargetName,
    int Distance,
    int TravelTicks,
    bool CanAttack,
    bool CanScout);

public record PossibleDiplomacyActionsDto(
    IReadOnlyList<DiplomacyOverviewRelationDto> Players,
    bool CanSendMessage,
    bool CanDeclareDiplomacy,
    DiplomacyOverviewMessageDto? LatestUnreadMessage);

public record PossibleActionsDto(
    int CurrentTick,
    Guid CityId,
    CityResourcesDto Resources,
    int FoodProductionPerTick,
    int FoodUpkeepPerTick,
    IReadOnlyList<TroopStackEntryDto> Troops,
    IReadOnlyList<PossibleUpgradeDto> Upgrades,
    IReadOnlyList<PossibleTrainDto> Train,
    IReadOnlyList<PossibleTargetDto> Targets,
    PossibleDiplomacyActionsDto Diplomacy);
