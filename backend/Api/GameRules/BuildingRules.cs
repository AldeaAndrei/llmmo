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
            new BuildingUpgradeCost(35, 25, 15, 10)),
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

    public static bool IsValidType(string type) => ByType.ContainsKey(type);

    public static BuildingRule Get(string type) => ByType[type];

    public static int ProductionAtLevel(string type, int level)
    {
        var rule = Get(type);
        if (rule.EffectKind != BuildingEffectKind.Production || level <= 0)
        {
            return 0;
        }

        return GameBalance.ProductionPerLevel * level;
    }

    public static int StorageCapacityAtLevel(int level) =>
        GameBalance.BaseMaxResource + GameBalance.StorageBonusPerLevel * Math.Max(0, level);

    public static double SpySurvivalAtLevel(int academyLevel)
    {
        if (academyLevel <= 0)
        {
            return GameBalance.SpySurvivalBase;
        }

        var survival = GameBalance.SpySurvivalBase
            + GameBalance.SpySurvivalBonusPerLevel * (academyLevel - 1);

        return Math.Min(GameBalance.SpySurvivalCap, survival);
    }

    public static int WallDefenceBonusAtLevel(int wallLevel) =>
        GameBalance.WallDefencePerLevel * Math.Max(0, wallLevel);

    public static int TrainCapacityAtLevel(int barracksLevel) =>
        GameBalance.BarracksTrainCapPerLevel * Math.Max(0, barracksLevel);

    public static BuildingUpgradeCost UpgradeCostForLevel(string type, int targetLevel)
    {
        var rule = Get(type);
        var multiplier = Math.Max(1, targetLevel);

        return new BuildingUpgradeCost(
            rule.BaseUpgradeCost.Wood * multiplier,
            rule.BaseUpgradeCost.Stone * multiplier,
            rule.BaseUpgradeCost.Gold * multiplier,
            rule.BaseUpgradeCost.Food * multiplier);
    }

    public static int UpgradeDurationTicks(int currentLevel) =>
        Math.Max(1, currentLevel) * GameBalance.UpgradeTicksPerLevel;

    public static string EffectFormulaText(string type)
    {
        var rule = Get(type);
        return rule.EffectKind switch
        {
            BuildingEffectKind.Production =>
                $"+{GameBalance.ProductionPerLevel} {rule.Resource!.Value.ToString().ToLowerInvariant()} per level per tick",
            BuildingEffectKind.Storage =>
                $"{GameBalance.BaseMaxResource} + {GameBalance.StorageBonusPerLevel} per level storage per resource",
            BuildingEffectKind.Training =>
                $"{GameBalance.BarracksTrainCapPerLevel} troops per train action per level",
            BuildingEffectKind.SpySurvival =>
                $"{GameBalance.SpySurvivalBase:P0} + {GameBalance.SpySurvivalBonusPerLevel:P1} per level above 1 spy survival (max {GameBalance.SpySurvivalCap:P0})",
            BuildingEffectKind.Defence =>
                $"+{GameBalance.WallDefencePerLevel} defence power per level",
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
        if (currentLevel >= GameBalance.MaxBuildingLevel)
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
                $"{StorageCapacityAtLevel(nextLevel)} storage per resource (+{GameBalance.StorageBonusPerLevel})",
            BuildingEffectKind.Training =>
                $"Train up to {TrainCapacityAtLevel(nextLevel)} troops per action (+{GameBalance.BarracksTrainCapPerLevel})",
            BuildingEffectKind.SpySurvival =>
                $"{SpySurvivalAtLevel(nextLevel):P1} spy survival (+{GameBalance.SpySurvivalBonusPerLevel:P1})",
            BuildingEffectKind.Defence =>
                $"+{WallDefenceBonusAtLevel(nextLevel)} defence power (+{GameBalance.WallDefencePerLevel})",
            _ => null,
        };
    }
}
