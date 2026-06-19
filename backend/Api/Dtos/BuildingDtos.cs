namespace llmmo.Api.Dtos;

public record BuildingUpgradeCostDto(int Wood, int Stone, int Gold, int Food);

public record BuildingDto(
    string Type,
    string Name,
    int Level,
    string Description,
    string CurrentEffect,
    string? NextLevelEffect,
    int? UpgradeDurationTicks,
    int? ProductionPerTick,
    string? ProductionResource,
    bool CanTrainTroops,
    BuildingUpgradeCostDto? NextUpgradeCost,
    BuildingUpgradeCostDto? TrainCostPerTroop = null,
    int? TrainCapacity = null);

public record BuildingCatalogDto(
    string Type,
    string Name,
    string Description,
    string EffectKind,
    string EffectFormula,
    int MaxLevel,
    BuildingUpgradeCostDto BaseUpgradeCost,
    int? ProductionPerLevel,
    string? ProductionResource);
