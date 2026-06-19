using llmmo.Api;
using llmmo.Entities;

namespace CalcTests;

public class PossibleActionsCalculatorTests
{
    [Fact]
    public void GetAffordableUpgrades_OnlyListsBuildingsCityCanPayFor()
    {
        var city = CreateCity(wood: 100, stone: 100, gold: 100, food: 25);
        city.Buildings =
        [
            new Building { Type = "bakery", Level = 1 },
            new Building { Type = "timber_station", Level = 1 },
        ];

        var upgrades = PossibleActionsCalculator.GetAffordableUpgrades(city, upgradeSlotAvailable: true);

        Assert.Contains(upgrades, upgrade => upgrade.BuildingType == "bakery" && upgrade.FromLevel == 1 && upgrade.ToLevel == 2);
        Assert.DoesNotContain(upgrades, upgrade => upgrade.BuildingType == "timber_station");
    }

    [Fact]
    public void GetAffordableUpgrades_ReturnsEmptyWhenUpgradeSlotBusy()
    {
        var city = CreateCity(wood: 500, stone: 500, gold: 500, food: 500);
        city.Buildings = [new Building { Type = "bakery", Level = 1 }];

        var upgrades = PossibleActionsCalculator.GetAffordableUpgrades(city, upgradeSlotAvailable: false);

        Assert.Empty(upgrades);
    }

    [Fact]
    public void GetAffordableTrain_OnlyOffersSoldierAndSpyWithCountOne()
    {
        var city = CreateCity(wood: 0, stone: 0, gold: 10, food: 5);
        city.Buildings = [new Building { Type = "barracks", Level = 8 }];
        city.Troops = [];

        var train = PossibleActionsCalculator.GetAffordableTrain(city, trainSlotAvailable: true);

        Assert.Equal(2, train.Count);
        Assert.Contains(train, option => option.TroopType == "soldier" && option.Count == 1);
        Assert.Contains(train, option => option.TroopType == "spy" && option.Count == 1);
    }

    [Fact]
    public void GetAffordableTrain_SkipsOptionsThatNeedMoreFood()
    {
        var city = CreateCity(wood: 0, stone: 0, gold: 10, food: 0);
        city.Buildings = [new Building { Type = "barracks", Level = 8 }];
        city.Troops = [];

        var train = PossibleActionsCalculator.GetAffordableTrain(city, trainSlotAvailable: true);

        Assert.Empty(train);
    }

    private static City CreateCity(int wood, int stone, int gold, int food) =>
        new()
        {
            Id = Guid.NewGuid(),
            PlayerId = Guid.NewGuid(),
            Wood = wood,
            Stone = stone,
            Gold = gold,
            Food = food,
            MaxWood = 1000,
            MaxStone = 1000,
            MaxGold = 1000,
            MaxFood = 1000,
            Buildings = [],
            Troops = [],
        };
}
