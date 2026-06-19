using llmmo.Api.Buildings;

namespace llmmo.Api.GameRules;

public record TroopRule(
    string Type,
    string Name,
    string Description,
    int CapacityWood,
    int CapacityStone,
    int CapacityGold,
    int CapacityFood,
    int AttackMelee,
    int AttackRange,
    int UpkeepFood,
    int Speed,
    bool CanScout,
    bool IsCombat,
    string TrainAtBuilding,
    BuildingUpgradeCost TrainCostPerUnit);

public static class TroopRules
{
    public static readonly string[] AllTypes = ["soldier", "spy"];

    private static readonly Dictionary<string, TroopRule> ByType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["soldier"] = new TroopRule(
            "soldier",
            "Soldier",
            "Combat troop with carry capacity and food upkeep.",
            CapacityWood: 1,
            CapacityStone: 1,
            CapacityGold: 1,
            CapacityFood: 1,
            AttackMelee: 1,
            AttackRange: 2,
            UpkeepFood: 1,
            Speed: 1,
            CanScout: false,
            IsCombat: true,
            TrainAtBuilding: "barracks",
            TrainCostPerUnit: new BuildingUpgradeCost(0, 0, 1, 1)),
        ["spy"] = new TroopRule(
            "spy",
            "Spy",
            "Scouts enemy cities; no combat power but consumes food.",
            CapacityWood: 0,
            CapacityStone: 0,
            CapacityGold: 0,
            CapacityFood: 0,
            AttackMelee: 0,
            AttackRange: 0,
            UpkeepFood: 1,
            Speed: 2,
            CanScout: true,
            IsCombat: false,
            TrainAtBuilding: "barracks",
            TrainCostPerUnit: new BuildingUpgradeCost(0, 0, 2, 2)),
    };

    public static bool IsValidType(string type) => ByType.ContainsKey(type);

    public static TroopRule Get(string type) => ByType[type];

    public static int CombatPower(string type, int count)
    {
        var rule = Get(type);
        return count * (rule.AttackMelee + rule.AttackRange);
    }

    public static int CarryCapacity(string type, int count)
    {
        var rule = Get(type);
        return count * (rule.CapacityWood + rule.CapacityStone + rule.CapacityGold + rule.CapacityFood);
    }

    public static BuildingUpgradeCost TrainCostForCount(string type, int count)
    {
        var rule = Get(type);
        return new BuildingUpgradeCost(
            rule.TrainCostPerUnit.Wood * count,
            rule.TrainCostPerUnit.Stone * count,
            rule.TrainCostPerUnit.Gold * count,
            rule.TrainCostPerUnit.Food * count);
    }

    public static bool IsTrainableAt(string troopType, string buildingType) =>
        Get(troopType).TrainAtBuilding.Equals(buildingType, StringComparison.OrdinalIgnoreCase);
}
