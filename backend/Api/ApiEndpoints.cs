using llmmo.Api.Endpoints;

namespace llmmo.Api;

public static class ApiEndpoints
{
    public static WebApplication MapApiEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1");

        api.MapAuthEndpoints();
        api.MapAgentEndpoints();
        api.MapCityEndpoints();
        api.MapMapEndpoints();
        api.MapWorldEndpoints();
        api.MapActionEndpoints();
        api.MapCatalogEndpoints();
        api.MapAttackEndpoints();
        api.MapReportEndpoints();
        api.MapDiplomacyEndpoints();

        return app;
    }
}
