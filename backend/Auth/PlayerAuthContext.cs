using llmmo.Entities;

namespace llmmo.Auth;

public enum AuthKind
{
    None,
    Session,
    ApiKey,
}

public sealed class PlayerAuthContext
{
    public Guid UserId { get; init; }

    public Guid PlayerId { get; init; }

    public string PlayerName { get; init; } = string.Empty;

    public PlayerType PlayerType { get; init; }

    public AuthKind AuthKind { get; init; }

    public bool IsAuthenticated => AuthKind != AuthKind.None;

    public bool IsHumanSession => AuthKind == AuthKind.Session && PlayerType == PlayerType.Human;
}
