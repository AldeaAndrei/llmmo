using llmmo.Api.Endpoints;

namespace llmmo.Api;

public static class ApiEndpoints
{
    public static WebApplication MapApiEndpoints(this WebApplication app)
    {
        // TODO: auth — UseAuthentication() / UseAuthorization()

        var api = app.MapGroup("/api/v1");

        api.MapPlayerEndpoints();
        api.MapCityEndpoints();
        api.MapMapEndpoints();
        api.MapActionEndpoints();

        return app;
    }
}
