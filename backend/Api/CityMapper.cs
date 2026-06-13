using llmmo.Api.Dtos;
using llmmo.Entities;

namespace llmmo.Api;

public static class CityMapper
{
    public static CityFullDto ToFullDto(City city) => new(
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
        city.CreatedAt,
        city.UpdatedAt);

    public static CityMapDto ToMapDto(City city) => new(
        city.Id,
        city.PlayerId,
        city.X,
        city.Y);
}
