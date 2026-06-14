using llmmo.Api.Dtos;
using llmmo.Api.Troops;
using llmmo.Entities;

namespace llmmo.Api;

public static class TroopMapper
{
    public static TroopCatalogDto ToCatalogDto(TroopDefinition def) => new(
        def.Type,
        def.Name,
        def.CapacityWood,
        def.CapacityStone,
        def.CapacityGold,
        def.CapacityFood,
        def.AttackMelee,
        def.AttackRange,
        def.Upkeep,
        def.Speed,
        def.CanScout,
        def.IsCombat,
        def.TrainAtBuilding,
        new BuildingUpgradeCostDto(
            def.TrainCostPerUnit.Wood,
            def.TrainCostPerUnit.Stone,
            def.TrainCostPerUnit.Gold,
            def.TrainCostPerUnit.Food));

    public static CityTroopDto ToCityDto(CityTroop troop) => ToCityDto(troop.Type, troop.Quantity);

    public static CityTroopDto ToCityDto(string type, int quantity)
    {
        var def = TroopCatalog.Get(type);
        return new CityTroopDto(
            def.Type,
            def.Name,
            quantity,
            def.CapacityWood,
            def.CapacityStone,
            def.CapacityGold,
            def.CapacityFood,
            def.AttackMelee,
            def.AttackRange,
            def.Upkeep,
            def.Speed,
            def.CanScout,
            def.IsCombat,
            Trainable: true,
            new BuildingUpgradeCostDto(
                def.TrainCostPerUnit.Wood,
                def.TrainCostPerUnit.Stone,
                def.TrainCostPerUnit.Gold,
                def.TrainCostPerUnit.Food));
    }

    public static TroopStackEntryDto ToStackDto(TroopStackEntry entry) =>
        new(entry.Type, entry.Count);
}
