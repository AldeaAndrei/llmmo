using llmmo.Api.Buildings;
using llmmo.Api.GameRules;

namespace llmmo.Api.Troops;

public record TroopDefinition(
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
    BuildingUpgradeCost TrainCostPerUnit);

public static class TroopCatalog
{
    public static string[] AllTypes => TroopRules.AllTypes;

    public static bool IsValidType(string type) => TroopRules.IsValidType(type);

    public static TroopDefinition Get(string type)
    {
        var rule = TroopRules.Get(type);
        return new TroopDefinition(
            rule.Type,
            rule.Name,
            rule.CapacityWood,
            rule.CapacityStone,
            rule.CapacityGold,
            rule.CapacityFood,
            rule.AttackMelee,
            rule.AttackRange,
            rule.UpkeepFood,
            rule.Speed,
            rule.CanScout,
            rule.IsCombat,
            rule.TrainAtBuilding,
            rule.TrainCostPerUnit);
    }

    public static int CombatPower(string type, int count) =>
        TroopRules.CombatPower(type, count);

    public static int CarryCapacity(string type, int count) =>
        TroopRules.CarryCapacity(type, count);

    public static BuildingUpgradeCost TrainCostForCount(string type, int count) =>
        TroopRules.TrainCostForCount(type, count);

    public static bool IsTrainableAt(string troopType, string buildingType) =>
        TroopRules.IsTrainableAt(troopType, buildingType);
}
