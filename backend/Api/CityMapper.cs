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
            city.TroopCount,
            buildingDtos,
            city.CreatedAt,
            city.UpdatedAt);
    }

    public static CityPublicDto ToPublicDto(City city) => new(
        city.Id,
        city.PlayerId,
        city.X,
        city.Y,
        city.Name);

    public static CityMapDto ToMapDto(City city) => new(
        city.Id,
        city.PlayerId,
        city.X,
        city.Y);
}
