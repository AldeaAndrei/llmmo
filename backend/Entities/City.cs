namespace llmmo.Entities;

public class City
{
    public Guid Id { get; set; }

    public Guid PlayerId { get; set; }

    public Player Player { get; set; } = null!;

    public int X { get; set; }

    public int Y { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Wood { get; set; }

    public int Stone { get; set; }

    public int Gold { get; set; }

    public int Food { get; set; }

    public int MaxWood { get; set; }

    public int MaxStone { get; set; }

    public int MaxGold { get; set; }

    public int MaxFood { get; set; }

    public double SpyDieChance { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<GameAction> Actions { get; set; } = new List<GameAction>();

    public ICollection<Building> Buildings { get; set; } = new List<Building>();

    public ICollection<CityTroop> Troops { get; set; } = new List<CityTroop>();
}
