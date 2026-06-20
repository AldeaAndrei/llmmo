using llmmo.Api.Buildings;

namespace llmmo.Api.GameRules;

public sealed class BuildingRulesEngine
{
    private readonly BalanceProfile _profile;

    public BuildingRulesEngine(BalanceProfile profile)
    {
        _profile = profile;
    }

    public BalanceProfile Profile => _profile;

    public int ProductionAtLevel(string type, int level)
    {
        var rule = BuildingRules.GetDefinition(type);
        if (rule.EffectKind != BuildingEffectKind.Production || level <= 0)
        {
            return 0;
        }

        return _profile.ProductionPerLevel * level;
    }

    public int StorageCapacityAtLevel(int level) =>
        _profile.BaseMaxResource + _profile.StorageBonusPerLevel * Math.Max(0, level);

    public double SpySurvivalAtLevel(int academyLevel)
    {
        if (academyLevel <= 0)
        {
            return _profile.SpySurvival.Base;
        }

        var survival = _profile.SpySurvival.Base
            + _profile.SpySurvival.BonusPerLevel * (academyLevel - 1);

        return Math.Min(_profile.SpySurvival.Cap, survival);
    }

    public int WallDefenceBonusAtLevel(int wallLevel) =>
        _profile.WallDefencePerLevel * Math.Max(0, wallLevel);

    public int TrainCapacityAtLevel(int barracksLevel) =>
        _profile.BarracksTrainCapPerLevel * Math.Max(0, barracksLevel);

    public BuildingUpgradeCost UpgradeCostForLevel(string type, int targetLevel)
    {
        var rule = BuildingRules.GetDefinition(type);
        var baseCost = _profile.BaseUpgradeCosts.TryGetValue(type, out var overrideCost)
            ? overrideCost
            : rule.BaseUpgradeCost;

        var multiplier = CostMultiplier(targetLevel);

        var cost = new BuildingUpgradeCost(
            SaturationScaling.ScaleCost(baseCost.Wood, multiplier),
            SaturationScaling.ScaleCost(baseCost.Stone, multiplier),
            SaturationScaling.ScaleCost(baseCost.Gold, multiplier),
            SaturationScaling.ScaleCost(baseCost.Food, multiplier));

        if (type.Equals("bakery", StringComparison.OrdinalIgnoreCase))
        {
            cost = cost with { Food = 0 };
        }

        return cost;
    }

    public int UpgradeDurationTicks(int currentLevel)
    {
        if (_profile.ScalingMode == BalanceScalingMode.Linear)
        {
            return Math.Max(1, currentLevel) * _profile.UpgradeTicksPerLevel;
        }

        var multiplier = SaturationScaling.Multiplier(
            currentLevel,
            _profile.NUpgradeTime,
            _profile.MaxBuildingLevel);

        return Math.Max(1, (int)Math.Round(_profile.BaseUpgradeTicks * multiplier));
    }

    public int TrainDurationTicks(int count, int barracksCapacity)
    {
        var effectiveCount = Math.Max(1, count);

        if (_profile.ScalingMode == BalanceScalingMode.Linear)
        {
            return Math.Max(1, _profile.BaseTrainTicks * effectiveCount);
        }

        var multiplier = SaturationScaling.Multiplier(
            effectiveCount,
            _profile.NTrainTime,
            Math.Max(1, barracksCapacity));

        return Math.Max(1, (int)Math.Round(_profile.BaseTrainTicks * effectiveCount * multiplier));
    }

    private double CostMultiplier(int level)
    {
        var effectiveLevel = Math.Max(1, level);

        if (_profile.ScalingMode == BalanceScalingMode.Linear)
        {
            return effectiveLevel;
        }

        return SaturationScaling.Multiplier(
            effectiveLevel,
            _profile.NCost,
            _profile.MaxBuildingLevel);
    }
}
