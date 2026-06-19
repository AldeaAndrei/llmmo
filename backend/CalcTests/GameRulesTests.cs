using llmmo.Api.GameRules;
using llmmo.Api.Buildings;
using llmmo.Api.Troops;
using llmmo.Entities;

namespace CalcTests;

public class BuildingRulesTests
{
    [Theory]
    [InlineData(1, 3)]
    [InlineData(5, 15)]
    public void ProductionAtLevel_Scales(int level, int expected)
    {
        Assert.Equal(expected, BuildingRules.ProductionAtLevel("gold_mine", level));
    }

    [Theory]
    [InlineData(1, 1150)]
    [InlineData(5, 1750)]
    public void StorageCapacityAtLevel_Scales(int level, int expected)
    {
        Assert.Equal(expected, BuildingRules.StorageCapacityAtLevel(level));
    }

    [Theory]
    [InlineData(1, 0.50)]
    [InlineData(2, 0.515)]
    [InlineData(20, 0.785)]
    public void SpySurvivalAtLevel_Scales(int level, double expected)
    {
        Assert.Equal(expected, BuildingRules.SpySurvivalAtLevel(level), precision: 3);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(5, 50)]
    public void WallDefenceBonusAtLevel_Flat(int level, int expected)
    {
        Assert.Equal(expected, BuildingRules.WallDefenceBonusAtLevel(level));
    }

    [Theory]
    [InlineData(1, 4)]
    [InlineData(5, 20)]
    public void TrainCapacityAtLevel_Scales(int level, int expected)
    {
        Assert.Equal(expected, BuildingRules.TrainCapacityAtLevel(level));
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(5, 10)]
    [InlineData(19, 38)]
    public void UpgradeDurationTicks_UsesCurrentLevel(int currentLevel, int expected)
    {
        Assert.Equal(expected, BuildingRules.UpgradeDurationTicks(currentLevel));
    }
}

public class CityBuildingEffectsTests
{
    [Fact]
    public void Apply_SetsMaxAndSpyDieChance()
    {
        var cityId = Guid.NewGuid();
        var city = new City
        {
            Id = cityId,
            Buildings =
            [
                new Building { CityId = cityId, Type = "storage_shed", Level = 1 },
                new Building { CityId = cityId, Type = "spy_academy", Level = 1 },
            ],
            Troops = [],
        };

        CityBuildingEffects.Apply(city);

        Assert.Equal(1150, city.MaxWood);
        Assert.Equal(0.50, city.SpyDieChance);
    }

    [Fact]
    public void Apply_ClampResourcesWhenMaxShrinks()
    {
        var cityId = Guid.NewGuid();
        var city = new City
        {
            Id = cityId,
            Wood = 2000,
            Buildings = [new Building { CityId = cityId, Type = "storage_shed", Level = 1 }],
            Troops = [],
        };

        CityBuildingEffects.Apply(city);

        Assert.Equal(1150, city.Wood);
    }
}

public class CombatResolverTests
{
    [Fact]
    public void Resolve_WallBonusWithNoTroops()
    {
        var cityId = Guid.NewGuid();
        var defender = new City
        {
            Id = cityId,
            Troops = [],
            Buildings = [new Building { CityId = cityId, Type = "wall", Level = 5 }],
        };

        var resolver = new llmmo.Tick.CombatResolver();
        var result = resolver.Resolve(
            [new TroopStackEntry("soldier", 20)],
            defender);

        Assert.True(result.AttackerWins);
        Assert.Equal(50, result.DefenderPower);
    }
}

public class CityResourcesTests
{
    [Fact]
    public void Refund_ClampsToMax()
    {
        var city = new City
        {
            Wood = 1100,
            MaxWood = 1150,
            Stone = 0,
            MaxStone = 1150,
            Gold = 0,
            MaxGold = 1150,
            Food = 0,
            MaxFood = 1150,
        };

        CityResources.Refund(city, new BuildingUpgradeCost(100, 0, 0, 0));

        Assert.Equal(1150, city.Wood);
    }
}
