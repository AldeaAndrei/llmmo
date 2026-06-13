namespace llmmo.Auth;

public static class EndpointAuthExtensions
{
    public static RouteHandlerBuilder RequireAuth(this RouteHandlerBuilder builder)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var httpContext = context.HttpContext;
            var auth = httpContext.GetPlayerAuth();
            if (auth is null || !auth.IsAuthenticated)
            {
                return Results.Unauthorized();
            }

            return await next(context);
        });
    }

    public static RouteHandlerBuilder RequireHumanSession(this RouteHandlerBuilder builder)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var auth = context.HttpContext.GetPlayerAuth();
            if (auth is null || !auth.IsHumanSession)
            {
                return auth?.IsAuthenticated == true
                    ? Results.StatusCode(StatusCodes.Status403Forbidden)
                    : Results.Unauthorized();
            }

            return await next(context);
        });
    }
}
