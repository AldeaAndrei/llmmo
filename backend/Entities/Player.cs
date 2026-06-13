namespace llmmo.Entities;

public class Player
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public PlayerType PlayerType { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<City> Cities { get; set; } = new List<City>();
}

