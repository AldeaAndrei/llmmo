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

public record PossibleAttackDto(
    Guid TargetCityId,
    string TargetName,
    int TargetX,
    int TargetY,
    IReadOnlyList<TroopStackEntryDto> Troops);

public record PossibleActionsDto(
    int CurrentTick,
    Guid CityId,
    CityResourcesDto Resources,
    int FoodProductionPerTick,
    int FoodUpkeepPerTick,
    IReadOnlyList<TroopStackEntryDto> Troops,
    IReadOnlyList<PossibleUpgradeDto> Upgrades,
    IReadOnlyList<PossibleTrainDto> Train,
    IReadOnlyList<PossibleAttackDto> Attacks);
