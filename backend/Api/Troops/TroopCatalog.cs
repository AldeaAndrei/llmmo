using llmmo.Api.Buildings;

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
    public static readonly string[] AllTypes = ["soldier", "spy"];

    private static readonly Dictionary<string, TroopDefinition> ByType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["soldier"] = new TroopDefinition(
            "soldier", "Soldier",
            CapacityWood: 1, CapacityStone: 1, CapacityGold: 1, CapacityFood: 1,
            AttackMelee: 1, AttackRange: 2,
            Upkeep: 1, Speed: 1,
            CanScout: false, IsCombat: true,
            TrainAtBuilding: "barracks",
            TrainCostPerUnit: new BuildingUpgradeCost(0, 0, 1, 1)),
        ["spy"] = new TroopDefinition(
            "spy", "Spy",
            CapacityWood: 0, CapacityStone: 0, CapacityGold: 0, CapacityFood: 0,
            AttackMelee: 0, AttackRange: 0,
            Upkeep: 1, Speed: 2,
            CanScout: true, IsCombat: false,
            TrainAtBuilding: "barracks",
            TrainCostPerUnit: new BuildingUpgradeCost(0, 0, 2, 2)),
    };

    public static bool IsValidType(string type) => ByType.ContainsKey(type);

    public static TroopDefinition Get(string type) => ByType[type];

    public static int CombatPower(string type, int count)
    {
        var def = Get(type);
        return count * (def.AttackMelee + def.AttackRange);
    }

    public static int CarryCapacity(string type, int count)
    {
        var def = Get(type);
        return count * (def.CapacityWood + def.CapacityStone + def.CapacityGold + def.CapacityFood);
    }

    public static BuildingUpgradeCost TrainCostForCount(string type, int count)
    {
        var def = Get(type);
        return new BuildingUpgradeCost(
            def.TrainCostPerUnit.Wood * count,
            def.TrainCostPerUnit.Stone * count,
            def.TrainCostPerUnit.Gold * count,
            def.TrainCostPerUnit.Food * count);
    }

    public static bool IsTrainableAt(string troopType, string buildingType) =>
        Get(troopType).TrainAtBuilding.Equals(buildingType, StringComparison.OrdinalIgnoreCase);
}
