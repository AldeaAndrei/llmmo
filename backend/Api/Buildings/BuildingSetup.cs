using llmmo.Entities;

namespace llmmo.Api.Buildings;

public static class BuildingSetup
{
    public const int StartingLevel = 1;

    public static IEnumerable<Building> CreateDefaults(Guid cityId)
    {
        foreach (var type in BuildingCatalog.AllTypes)
        {
            yield return new Building
            {
                Id = Guid.NewGuid(),
                CityId = cityId,
                Type = type,
                Level = StartingLevel,
            };
        }
    }
}
