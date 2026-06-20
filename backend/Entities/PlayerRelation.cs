namespace llmmo.Entities;

public class PlayerRelation
{
    public Guid FromPlayerId { get; set; }

    public Player FromPlayer { get; set; } = null!;

    public Guid ToPlayerId { get; set; }

    public Player ToPlayer { get; set; } = null!;

    public DiplomacyRelationType Relation { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int UpdatedAtTick { get; set; }
}
