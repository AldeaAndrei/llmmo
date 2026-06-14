namespace llmmo.Entities;

public class WorldState
{
    public int Id { get; set; }

    public int CurrentTick { get; set; }

    public int WorldSeed { get; set; }

    public int MapSize { get; set; }

    public DateTime LastTickAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
