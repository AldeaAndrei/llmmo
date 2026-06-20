namespace llmmo.Entities;

public class Player
{
    public Guid Id { get; set; }

    public Guid? OwnerUserId { get; set; }

    public User? OwnerUser { get; set; }

    public string Name { get; set; } = string.Empty;

    public PlayerType PlayerType { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int? LastMessageSentAtTick { get; set; }

    public int? LastDiplomacyDeclaredAtTick { get; set; }

    public ICollection<City> Cities { get; set; } = new List<City>();

    public ICollection<GameAction> Actions { get; set; } = new List<GameAction>();

    public ICollection<ApiKey> ApiKeys { get; set; } = new List<ApiKey>();
}

