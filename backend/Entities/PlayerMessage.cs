namespace llmmo.Entities;

public class PlayerMessage
{
    public Guid Id { get; set; }

    public Guid FromPlayerId { get; set; }

    public Player FromPlayer { get; set; } = null!;

    public Guid ToPlayerId { get; set; }

    public Player ToPlayer { get; set; } = null!;

    public string Subject { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public DateTime SentAt { get; set; }

    public int SentAtTick { get; set; }

    public DateTime? ReadAt { get; set; }
}
