namespace llmmo.Auth;

public class AuthOptions
{
    public const string SectionName = "Auth";

    public string JwtSecret { get; set; } = "dev-only-change-me-in-production-llmmo-secret-key";

    public string CookieName { get; set; } = "llmmo_session";

    public int SessionDays { get; set; } = 7;
}
