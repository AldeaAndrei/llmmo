using llmmo.Api.Dtos;
using llmmo.Entities;
namespace llmmo.Api;

public static class CityMapper
{
    public static CityFullDto ToFullDto(City city, IEnumerable<Building>? buildings = null)
    {
        var buildingDtos = (buildings ?? city.Buildings)
            .OrderBy(b => b.Type)
            .Select(BuildingMapper.ToDto)
            .ToList();

        var troopDtos = (city.Troops ?? [])
            .OrderBy(t => t.Type)
            .Select(TroopMapper.ToCityDto)
            .ToList();

        return new CityFullDto(
            city.Id,
            city.PlayerId,
            city.X,
            city.Y,
            city.Name,
            city.Wood,
            city.Stone,
            city.Gold,
            city.Food,
            CityResourceCalculator.BuildResourcesView(city),
            city.DefenceFactor,
            city.SpyDieChance,
            troopDtos,
            buildingDtos,
            city.CreatedAt,
            city.UpdatedAt);
    }

    public static CityVisibilityDto ToVisibilityDto(
        City city,
        string visibility,
        CityResourcesDto? resources,
        IReadOnlyList<TroopStackEntryDto>? troops) => new(
        city.Id,
        city.PlayerId,
        city.X,
        city.Y,
        city.Name,
        visibility,
        resources,
        troops,
        city.DefenceFactor,
        city.SpyDieChance);

    public static CityMapDto ToMapDto(City city) => new(
        city.Id,
        city.PlayerId,
        city.X,
        city.Y);
}
