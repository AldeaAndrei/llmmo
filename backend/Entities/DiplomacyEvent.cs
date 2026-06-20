namespace llmmo.Entities;

public class DiplomacyEvent
{
    public Guid Id { get; set; }

    public Guid DeclaredByPlayerId { get; set; }

    public Player DeclaredByPlayer { get; set; } = null!;

    public Guid TargetPlayerId { get; set; }

    public Player TargetPlayer { get; set; } = null!;

    /// <summary>ally, enemy, or clear_relation</summary>
    public string EventType { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public int CreatedAtTick { get; set; }
}
