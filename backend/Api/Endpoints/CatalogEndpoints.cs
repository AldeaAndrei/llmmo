using llmmo.Api.Dtos;
using llmmo.Api.GameRules;
using llmmo.Api.Troops;

namespace llmmo.Api.Endpoints;

public static class CatalogEndpoints
{
    public static RouteGroupBuilder MapCatalogEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/catalog/troops", GetTroopCatalog);
        group.MapGet("/catalog/buildings", GetBuildingCatalog);
        return group;
    }

    private static IResult GetTroopCatalog()
    {
        var troops = TroopCatalog.AllTypes
            .Select(type => TroopMapper.ToCatalogDto(TroopCatalog.Get(type)))
            .ToList();

        return Results.Ok(troops);
    }

    private static IResult GetBuildingCatalog()
    {
        var buildings = BuildingRules.AllTypes
            .Select(BuildingMapper.ToCatalogDto)
            .ToList();

        return Results.Ok(buildings);
    }
}
