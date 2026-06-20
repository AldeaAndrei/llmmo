using llmmo.Api.Buildings;
using llmmo.Api.GameRules;

namespace BalanceSim.Core;

public sealed class SimCityState
{
    public SimCityState(BalanceProfile profile)
    {
        Profile = profile;
        BuildingsEngine = new BuildingRulesEngine(profile);
        TroopsEngine = new TroopRulesEngine(profile, BuildingsEngine);

        foreach (var type in BuildingRules.AllTypes)
        {
            BuildingLevels[type] = 1;
        }

        foreach (var type in TroopRules.AllTypes)
        {
            TroopCounts[type] = 0;
        }

        var starter = profile.StarterCity;
        Wood = starter.Wood;
        Stone = starter.Stone;
        Gold = starter.Gold;
        Food = starter.Food;
        TroopCounts["soldier"] = starter.StarterSoldiers;

        ApplyBuildingEffects();
    }

    public BalanceProfile Profile { get; }
    public BuildingRulesEngine BuildingsEngine { get; }
    public TroopRulesEngine TroopsEngine { get; }

    public int Tick { get; set; }
    public int Wood { get; set; }
    public int Stone { get; set; }
    public int Gold { get; set; }
    public int Food { get; set; }
    public int MaxWood { get; private set; }
    public int MaxStone { get; private set; }
    public int MaxGold { get; private set; }
    public int MaxFood { get; private set; }
    public double SpyDieChance { get; private set; }

    public Dictionary<string, int> BuildingLevels { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, int> TroopCounts { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public PendingAction? PendingUpgrade { get; set; }
    public PendingAction? PendingTrain { get; set; }
    public MilitaryMission? ActiveMission { get; set; }

    public SpendLedger Spend { get; set; } = new(0, 0, 0, 0, 0, 0, 0, 0);
    public List<SimEvent> TickEvents { get; } = [];
    public List<string> UpgradeHistory { get; } = [];

    public bool UpgradeSlotFree => PendingUpgrade is null;
    public bool TrainSlotFree => PendingTrain is null && ActiveMission is null;

    public int BarracksLevel => BuildingLevels.GetValueOrDefault("barracks", 0);
    public int TrainCapacity => BuildingsEngine.TrainCapacityAtLevel(BarracksLevel);

    public void ApplyBuildingEffects()
    {
        var shedLevel = BuildingLevels.GetValueOrDefault("storage_shed", 0);
        var max = BuildingsEngine.StorageCapacityAtLevel(shedLevel);
        MaxWood = max;
        MaxStone = max;
        MaxGold = max;
        MaxFood = max;

        var academyLevel = BuildingLevels.GetValueOrDefault("spy_academy", 0);
        var survival = BuildingsEngine.SpySurvivalAtLevel(academyLevel);
        SpyDieChance = 1.0 - survival;

        ClampResources();
    }

    public bool CanAfford(BuildingUpgradeCost cost) =>
        Wood >= cost.Wood && Stone >= cost.Stone && Gold >= cost.Gold && Food >= cost.Food;

    public void Deduct(BuildingUpgradeCost cost)
    {
        Wood -= cost.Wood;
        Stone -= cost.Stone;
        Gold -= cost.Gold;
        Food -= cost.Food;
    }

    public void AddResources(int wood, int stone, int gold, int food)
    {
        Wood = Math.Min(MaxWood, Wood + wood);
        Stone = Math.Min(MaxStone, Stone + stone);
        Gold = Math.Min(MaxGold, Gold + gold);
        Food = Math.Min(MaxFood, Food + food);
    }

    public void RecordUpgradeSpend(BuildingUpgradeCost cost)
    {
        Spend = Spend with
        {
            UpgradeWood = Spend.UpgradeWood + cost.Wood,
            UpgradeStone = Spend.UpgradeStone + cost.Stone,
            UpgradeGold = Spend.UpgradeGold + cost.Gold,
            UpgradeFood = Spend.UpgradeFood + cost.Food,
        };
    }

    public void RecordTrainSpend(BuildingUpgradeCost cost)
    {
        Spend = Spend with
        {
            TrainWood = Spend.TrainWood + cost.Wood,
            TrainStone = Spend.TrainStone + cost.Stone,
            TrainGold = Spend.TrainGold + cost.Gold,
            TrainFood = Spend.TrainFood + cost.Food,
        };
    }

    public ResourceProduction CalculateProduction()
    {
        var gold = 0;
        var stone = 0;
        var wood = 0;
        var food = 0;

        foreach (var (type, level) in BuildingLevels)
        {
            var rule = BuildingRules.GetDefinition(type);
            if (rule.EffectKind != BuildingEffectKind.Production || level <= 0 || rule.Resource is null)
            {
                continue;
            }

            var amount = BuildingsEngine.ProductionAtLevel(type, level);
            switch (rule.Resource)
            {
                case BuildingResource.Gold: gold += amount; break;
                case BuildingResource.Stone: stone += amount; break;
                case BuildingResource.Wood: wood += amount; break;
                case BuildingResource.Food: food += amount; break;
            }
        }

        return new ResourceProduction(gold, stone, wood, food);
    }

    public int CalculateFoodUpkeep()
    {
        var total = 0;
        foreach (var (type, count) in TroopCounts)
        {
            if (count <= 0)
            {
                continue;
            }

            total += count * TroopsEngine.GetConfig(type).UpkeepFood;
        }

        return total;
    }

    public void ApplyProduction()
    {
        var production = CalculateProduction();

        if (production.Gold > 0)
        {
            Gold = Math.Min(MaxGold, Gold + production.Gold);
        }

        if (production.Stone > 0)
        {
            Stone = Math.Min(MaxStone, Stone + production.Stone);
        }

        if (production.Wood > 0)
        {
            Wood = Math.Min(MaxWood, Wood + production.Wood);
        }

        if (production.Food > 0)
        {
            Food = Math.Min(MaxFood, Food + production.Food);
        }
    }

    public void ApplyUpkeep()
    {
        var required = CalculateFoodUpkeep();
        if (required <= 0)
        {
            return;
        }

        if (Food >= required)
        {
            Food -= required;
            return;
        }

        Food = 0;
        TickEvents.Add(new SimEvent("starvation", $"required={required}"));

        foreach (var type in TroopCounts.Keys.ToList())
        {
            TroopCounts[type] = Math.Max(0, TroopCounts[type] - 1);
        }
    }

    public int GetBuildingLevel(string type) => BuildingLevels.GetValueOrDefault(type, 0);

    public int GetTroopCount(string type) => TroopCounts.GetValueOrDefault(type, 0);

    public int TotalSoldiers => GetTroopCount("soldier");

    public int TotalSpies => GetTroopCount("spy");

    public int CombatPower()
    {
        var power = 0;
        foreach (var (type, count) in TroopCounts)
        {
            power += TroopsEngine.CombatPower(type, count);
        }

        return power + BuildingsEngine.WallDefenceBonusAtLevel(GetBuildingLevel("wall"));
    }

    private void ClampResources()
    {
        Wood = Math.Min(Wood, MaxWood);
        Stone = Math.Min(Stone, MaxStone);
        Gold = Math.Min(Gold, MaxGold);
        Food = Math.Min(Food, MaxFood);
    }
}

public record ResourceProduction(int Gold, int Stone, int Wood, int Food);
