using llmmo.Entities;

namespace llmmo.Api.Troops;

public static class TroopSetup
{
    public static IEnumerable<CityTroop> CreateDefaults(Guid cityId)
    {
        foreach (var type in TroopCatalog.AllTypes)
        {
            yield return new CityTroop
            {
                Id = Guid.NewGuid(),
                CityId = cityId,
                Type = type,
                Quantity = 0,
            };
        }
    }
}
