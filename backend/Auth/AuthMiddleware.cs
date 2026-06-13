namespace llmmo.Auth;

public class AuthMiddleware
{
    private readonly RequestDelegate _next;

    public AuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AuthService authService)
    {
        var auth = await authService.ResolveAsync(context, context.RequestAborted);
        context.SetPlayerAuth(auth);
        await _next(context);
    }
}
