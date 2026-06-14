namespace llmmo.Entities;

public class CityTroop
{
    public Guid Id { get; set; }

    public Guid CityId { get; set; }

    public City City { get; set; } = null!;

    public string Type { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
