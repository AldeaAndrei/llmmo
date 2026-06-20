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

    private static BalanceProfile? _engineProfile;
    private static TroopRulesEngine? _engineInstance;

    private static TroopRulesEngine DefaultEngine
    {
        get
        {
            var active = BalanceProfile.Active;
            if (_engineInstance is null || !ReferenceEquals(active, _engineProfile))
            {
                _engineProfile = active;
                _engineInstance = new TroopRulesEngine(active);
            }

            return _engineInstance;
        }
    }

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

    public static TroopRulesEngine CreateEngine(BalanceProfile profile) => new(profile);

    public static bool IsValidType(string type) => ByType.ContainsKey(type);

    public static TroopRule Get(string type) => ByType[type];

    public static TroopRule GetDefinition(string type) => Get(type);

    public static int CombatPower(string type, int count) =>
        DefaultEngine.CombatPower(type, count);

    public static int CarryCapacity(string type, int count) =>
        DefaultEngine.CarryCapacity(type, count);

    public static BuildingUpgradeCost TrainCostForCount(string type, int count)
    {
        var barracksCap = BalanceProfile.Active.BarracksTrainCapPerLevel;
        return DefaultEngine.TrainCostForCount(type, count, barracksCap);
    }

    public static bool IsTrainableAt(string troopType, string buildingType) =>
        DefaultEngine.IsTrainableAt(troopType, buildingType);
}
