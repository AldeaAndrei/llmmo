using llmmo.Api.Buildings;

namespace llmmo.Api.GameRules;

public enum BuildingEffectKind
{
    Production,
    Storage,
    Training,
    SpySurvival,
    Defence,
}

public record BuildingRule(
    string Type,
    string Name,
    string Description,
    BuildingEffectKind EffectKind,
    BuildingResource? Resource,
    BuildingUpgradeCost BaseUpgradeCost);

public static class BuildingRules
{
    public static readonly string[] AllTypes =
    [
        "gold_mine",
        "stone_mine",
        "timber_station",
        "bakery",
        "storage_shed",
        "barracks",
        "spy_academy",
        "wall",
    ];

    private static BalanceProfile? _engineProfile;
    private static BuildingRulesEngine? _engineInstance;

    // Bind to whatever profile is Active, rebuilding only when it changes (it is set
    // once at startup), so all live cost/duration/production math follows the config.
    private static BuildingRulesEngine DefaultEngine
    {
        get
        {
            var active = BalanceProfile.Active;
            if (_engineInstance is null || !ReferenceEquals(active, _engineProfile))
            {
                _engineProfile = active;
                _engineInstance = new BuildingRulesEngine(active);
            }

            return _engineInstance;
        }
    }

    /// <summary>Max building level from the active balance profile.</summary>
    public static int MaxBuildingLevel => BalanceProfile.Active.MaxBuildingLevel;

    /// <summary>Production gained per building level from the active balance profile.</summary>
    public static int ProductionPerLevel => BalanceProfile.Active.ProductionPerLevel;

    private static readonly Dictionary<string, BuildingRule> ByType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["gold_mine"] = new BuildingRule(
            "gold_mine", "Gold Mine",
            "Produces gold each tick.",
            BuildingEffectKind.Production, BuildingResource.Gold,
            new BuildingUpgradeCost(50, 30, 0, 20)),
        ["stone_mine"] = new BuildingRule(
            "stone_mine", "Stone Mine",
            "Produces stone each tick.",
            BuildingEffectKind.Production, BuildingResource.Stone,
            new BuildingUpgradeCost(40, 20, 10, 15)),
        ["timber_station"] = new BuildingRule(
            "timber_station", "Timber Station",
            "Produces wood each tick.",
            BuildingEffectKind.Production, BuildingResource.Wood,
            new BuildingUpgradeCost(30, 20, 10, 15)),
        ["bakery"] = new BuildingRule(
            "bakery", "Bakery",
            "Produces food each tick.",
            BuildingEffectKind.Production, BuildingResource.Food,
            new BuildingUpgradeCost(50, 35, 20, 0)),
        ["storage_shed"] = new BuildingRule(
            "storage_shed", "Storage Shed",
            "Increases storage capacity for all resources.",
            BuildingEffectKind.Storage, null,
            new BuildingUpgradeCost(40, 35, 20, 15)),
        ["barracks"] = new BuildingRule(
            "barracks", "Barracks",
            "Train soldiers and spies.",
            BuildingEffectKind.Training, null,
            new BuildingUpgradeCost(60, 40, 25, 30)),
        ["spy_academy"] = new BuildingRule(
            "spy_academy", "Spy Academy",
            "Improves spy survival chance when scouting.",
            BuildingEffectKind.SpySurvival, null,
            new BuildingUpgradeCost(50, 35, 30, 20)),
        ["wall"] = new BuildingRule(
            "wall", "Wall",
            "Adds flat defence power when your city is attacked.",
            BuildingEffectKind.Defence, null,
            new BuildingUpgradeCost(45, 50, 15, 10)),
    };

    public static BuildingRulesEngine CreateEngine(BalanceProfile profile) => new(profile);

    public static bool IsValidType(string type) => ByType.ContainsKey(type);

    public static BuildingRule Get(string type) => ByType[type];

    public static BuildingRule GetDefinition(string type) => Get(type);

    public static int ProductionAtLevel(string type, int level) =>
        DefaultEngine.ProductionAtLevel(type, level);

    public static int StorageCapacityAtLevel(int level) =>
        DefaultEngine.StorageCapacityAtLevel(level);

    public static double SpySurvivalAtLevel(int academyLevel) =>
        DefaultEngine.SpySurvivalAtLevel(academyLevel);

    public static int WallDefenceBonusAtLevel(int wallLevel) =>
        DefaultEngine.WallDefenceBonusAtLevel(wallLevel);

    public static int TrainCapacityAtLevel(int barracksLevel) =>
        DefaultEngine.TrainCapacityAtLevel(barracksLevel);

    public static BuildingUpgradeCost UpgradeCostForLevel(string type, int targetLevel) =>
        DefaultEngine.UpgradeCostForLevel(type, targetLevel);

    public static int UpgradeDurationTicks(int currentLevel) =>
        DefaultEngine.UpgradeDurationTicks(currentLevel);

    public static int TrainDurationTicks(int count, int barracksCapacity) =>
        DefaultEngine.TrainDurationTicks(count, barracksCapacity);

    public static string EffectFormulaText(string type)
    {
        var rule = Get(type);
        var profile = BalanceProfile.Active;
        return rule.EffectKind switch
        {
            BuildingEffectKind.Production =>
                $"+{profile.ProductionPerLevel} {rule.Resource!.Value.ToString().ToLowerInvariant()} per level per tick",
            BuildingEffectKind.Storage =>
                $"{profile.BaseMaxResource} + {profile.StorageBonusPerLevel} per level storage per resource",
            BuildingEffectKind.Training =>
                $"{profile.BarracksTrainCapPerLevel} troops per train action per level",
            BuildingEffectKind.SpySurvival =>
                $"{profile.SpySurvival.Base:P0} + {profile.SpySurvival.BonusPerLevel:P1} per level above 1 spy survival (max {profile.SpySurvival.Cap:P0})",
            BuildingEffectKind.Defence =>
                $"+{profile.WallDefencePerLevel} defence power per level",
            _ => string.Empty,
        };
    }

    public static string FormatCurrentEffect(string type, int level)
    {
        if (level <= 0)
        {
            return "No effect";
        }

        var rule = Get(type);
        return rule.EffectKind switch
        {
            BuildingEffectKind.Production =>
                $"+{ProductionAtLevel(type, level)} {rule.Resource!.Value.ToString().ToLowerInvariant()} per tick",
            BuildingEffectKind.Storage =>
                $"{StorageCapacityAtLevel(level)} storage per resource",
            BuildingEffectKind.Training =>
                $"Train up to {TrainCapacityAtLevel(level)} troops per action",
            BuildingEffectKind.SpySurvival =>
                $"{SpySurvivalAtLevel(level):P1} spy survival",
            BuildingEffectKind.Defence =>
                $"+{WallDefenceBonusAtLevel(level)} defence power",
            _ => string.Empty,
        };
    }

    public static string? FormatNextLevelEffect(string type, int currentLevel)
    {
        var profile = BalanceProfile.Active;
        if (currentLevel >= profile.MaxBuildingLevel)
        {
            return null;
        }

        var nextLevel = currentLevel + 1;
        var rule = Get(type);
        return rule.EffectKind switch
        {
            BuildingEffectKind.Production =>
                $"+{ProductionAtLevel(type, nextLevel)} {rule.Resource!.Value.ToString().ToLowerInvariant()} per tick",
            BuildingEffectKind.Storage =>
                $"{StorageCapacityAtLevel(nextLevel)} storage per resource (+{profile.StorageBonusPerLevel})",
            BuildingEffectKind.Training =>
                $"Train up to {TrainCapacityAtLevel(nextLevel)} troops per action (+{profile.BarracksTrainCapPerLevel})",
            BuildingEffectKind.SpySurvival =>
                $"{SpySurvivalAtLevel(nextLevel):P1} spy survival (+{profile.SpySurvival.BonusPerLevel:P1})",
            BuildingEffectKind.Defence =>
                $"+{WallDefenceBonusAtLevel(nextLevel)} defence power (+{profile.WallDefencePerLevel})",
            _ => null,
        };
    }
}
