using llmmo.Api.Buildings;

namespace llmmo.Api.GameRules;

public sealed class TroopRulesEngine
{
    private readonly BalanceProfile _profile;
    private readonly BuildingRulesEngine _buildings;

    public TroopRulesEngine(BalanceProfile profile, BuildingRulesEngine? buildings = null)
    {
        _profile = profile;
        _buildings = buildings ?? new BuildingRulesEngine(profile);
    }

    public TroopTrainConfig GetConfig(string type)
    {
        if (_profile.TroopConfigs.TryGetValue(type, out var config))
        {
            return config;
        }

        var rule = TroopRules.GetDefinition(type);
        return new TroopTrainConfig(
            rule.TrainCostPerUnit,
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
            rule.TrainAtBuilding);
    }

    public int CombatPower(string type, int count)
    {
        var config = GetConfig(type);
        return count * (config.AttackMelee + config.AttackRange);
    }

    public int CarryCapacity(string type, int count)
    {
        var config = GetConfig(type);
        return count * (config.CapacityWood + config.CapacityStone + config.CapacityGold + config.CapacityFood);
    }

    public BuildingUpgradeCost TrainCostForCount(string type, int count, int barracksCapacity)
    {
        var config = GetConfig(type);

        if (_profile.ScalingMode == BalanceScalingMode.Linear)
        {
            return TrainCostForCountLinear(type, count);
        }

        var sat = SaturationScaling.Multiplier(
            count,
            _profile.NCost,
            Math.Max(1, barracksCapacity));

        return new BuildingUpgradeCost(
            SaturationScaling.ScaleCost(config.TrainCostPerUnit.Wood * count, sat),
            SaturationScaling.ScaleCost(config.TrainCostPerUnit.Stone * count, sat),
            SaturationScaling.ScaleCost(config.TrainCostPerUnit.Gold * count, sat),
            SaturationScaling.ScaleCost(config.TrainCostPerUnit.Food * count, sat));
    }

    public BuildingUpgradeCost TrainCostForCountLinear(string type, int count)
    {
        var config = GetConfig(type);
        return new BuildingUpgradeCost(
            config.TrainCostPerUnit.Wood * count,
            config.TrainCostPerUnit.Stone * count,
            config.TrainCostPerUnit.Gold * count,
            config.TrainCostPerUnit.Food * count);
    }

    public bool IsTrainableAt(string troopType, string buildingType) =>
        GetConfig(troopType).TrainAtBuilding.Equals(buildingType, StringComparison.OrdinalIgnoreCase);
}
