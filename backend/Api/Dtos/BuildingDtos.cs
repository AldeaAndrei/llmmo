namespace llmmo.Api.Dtos;

public record BuildingUpgradeCostDto(int Wood, int Stone, int Gold, int Food);

public record BuildingDto(
    string Type,
    string Name,
    int Level,
    int? ProductionPerTick,
    string? ProductionResource,
    bool CanTrainTroops,
    BuildingUpgradeCostDto? NextUpgradeCost);
