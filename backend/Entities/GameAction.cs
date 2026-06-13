namespace llmmo.Entities;

public class GameAction
{
    public Guid Id { get; set; }

    public Guid PlayerId { get; set; }

    public Player Player { get; set; } = null!;

    public Guid CityId { get; set; }

    public City City { get; set; } = null!;

    public string Type { get; set; } = string.Empty;

    public string Payload { get; set; } = "{}";

    public ActionStatus Status { get; set; }

    public int SubmittedAtTick { get; set; }

    public int? ReadyAtTick { get; set; }

    public int DurationTicks { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
