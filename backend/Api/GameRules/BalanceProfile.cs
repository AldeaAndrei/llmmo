using llmmo.Api.Buildings;

namespace llmmo.Api.GameRules;

public enum BalanceScalingMode
{
    Linear,
    Saturation,
}

public record SpySurvivalConfig(
    double Base,
    double BonusPerLevel,
    double Cap);

public record TroopTrainConfig(
    BuildingUpgradeCost TrainCostPerUnit,
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
    string TrainAtBuilding);

public record StarterCityConfig(
    int Wood,
    int Stone,
    int Gold,
    int Food,
    int StarterSoldiers);

public record BalanceProfile
{
    public int MaxBuildingLevel { get; init; } = GameBalance.MaxBuildingLevel;
    public int BaseMaxResource { get; init; } = GameBalance.BaseMaxResource;
    public int StorageBonusPerLevel { get; init; } = GameBalance.StorageBonusPerLevel;
    public int ProductionPerLevel { get; init; } = GameBalance.ProductionPerLevel;
    public BalanceScalingMode ScalingMode { get; init; } = BalanceScalingMode.Linear;
    public double NCost { get; init; } = 5.0;
    public double NUpgradeTime { get; init; } = 5.0;
    public double NTrainTime { get; init; } = 3.0;
    public int BaseUpgradeTicks { get; init; } = GameBalance.UpgradeTicksPerLevel;
    public int BaseTrainTicks { get; init; } = GameBalance.TrainDurationTicks;
    public int WallDefencePerLevel { get; init; } = GameBalance.WallDefencePerLevel;
    public int BarracksTrainCapPerLevel { get; init; } = GameBalance.BarracksTrainCapPerLevel;
    public int UpgradeTicksPerLevel { get; init; } = GameBalance.UpgradeTicksPerLevel;
    public int TrainDurationTicks { get; init; } = GameBalance.TrainDurationTicks;
    public double CombatWinnerLossFactor { get; init; } = GameBalance.CombatWinnerLossFactor;
    public double CombatLoserWipeFactor { get; init; } = GameBalance.CombatLoserWipeFactor;
    public SpySurvivalConfig SpySurvival { get; init; } = new(
        GameBalance.SpySurvivalBase,
        GameBalance.SpySurvivalBonusPerLevel,
        GameBalance.SpySurvivalCap);
    public IReadOnlyDictionary<string, BuildingUpgradeCost> BaseUpgradeCosts { get; init; } =
        CreateDefaultBaseUpgradeCosts();
    public IReadOnlyDictionary<string, TroopTrainConfig> TroopConfigs { get; init; } =
        CreateDefaultTroopConfigs();
    public StarterCityConfig StarterCity { get; init; } = new(50, 50, 2, 20, 1);
    public int ReferenceEndgameSoldiers { get; init; } = 50;

    public static BalanceProfile Default { get; } = new();

    private static BalanceProfile _active = Default;

    /// <summary>
    /// The profile the live game runs on. Defaults to the built-in <see cref="Default"/>
    /// but can be overridden at startup from a balance-profile JSON file (the same format
    /// the BalanceSim search produces).
    /// </summary>
    public static BalanceProfile Active => _active;

    public static void UseActive(BalanceProfile profile) => _active = profile ?? Default;

    /// <summary>
    /// Loads a balance profile JSON into <see cref="Active"/>. Returns false (leaving the
    /// current Active untouched) if the path is missing or the file cannot be parsed.
    /// </summary>
    public static bool TryLoadActiveFromFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return false;
        }

        try
        {
            var json = File.ReadAllText(path);
            var loaded = System.Text.Json.JsonSerializer.Deserialize<BalanceProfile>(json, LoadOptions);
            if (loaded is null)
            {
                return false;
            }

            _active = loaded;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static readonly System.Text.Json.JsonSerializerOptions LoadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static BalanceProfile ForSimulation(BalanceProfile? seed = null)
    {
        var baseProfile = seed ?? Default;
        return baseProfile with { ScalingMode = BalanceScalingMode.Saturation };
    }

    private static Dictionary<string, BuildingUpgradeCost> CreateDefaultBaseUpgradeCosts() =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["gold_mine"] = new(50, 30, 0, 20),
            ["stone_mine"] = new(40, 20, 10, 15),
            ["timber_station"] = new(30, 20, 10, 15),
            ["bakery"] = new(50, 35, 20, 0),
            ["storage_shed"] = new(40, 35, 20, 15),
            ["barracks"] = new(60, 40, 25, 30),
            ["spy_academy"] = new(50, 35, 30, 20),
            ["wall"] = new(45, 50, 15, 10),
        };

    private static Dictionary<string, TroopTrainConfig> CreateDefaultTroopConfigs() =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["soldier"] = new(
                new BuildingUpgradeCost(0, 0, 1, 1),
                1, 1, 1, 1,
                1, 2, 1, 1,
                false, true, "barracks"),
            ["spy"] = new(
                new BuildingUpgradeCost(0, 0, 2, 2),
                0, 0, 0, 0,
                0, 0, 1, 2,
                true, false, "barracks"),
        };
}
