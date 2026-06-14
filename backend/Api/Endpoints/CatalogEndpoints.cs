using llmmo.Api.Dtos;
using llmmo.Api.Troops;

namespace llmmo.Api.Endpoints;

public static class CatalogEndpoints
{
    public static RouteGroupBuilder MapCatalogEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/catalog/troops", GetTroopCatalog);
        return group;
    }

    private static IResult GetTroopCatalog()
    {
        var troops = TroopCatalog.AllTypes
            .Select(type => TroopMapper.ToCatalogDto(TroopCatalog.Get(type)))
            .ToList();

        return Results.Ok(troops);
    }
}
