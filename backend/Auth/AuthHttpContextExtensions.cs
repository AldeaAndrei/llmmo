namespace llmmo.Auth;

public static class AuthHttpContextExtensions
{
    public const string AuthItemKey = "PlayerAuthContext";

    public static PlayerAuthContext? GetPlayerAuth(this HttpContext context)
    {
        return context.Items.TryGetValue(AuthItemKey, out var value) ? value as PlayerAuthContext : null;
    }

    public static void SetPlayerAuth(this HttpContext context, PlayerAuthContext? auth)
    {
        if (auth is null)
        {
            context.Items.Remove(AuthItemKey);
        }
        else
        {
            context.Items[AuthItemKey] = auth;
        }
    }
}
