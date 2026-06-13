namespace llmmo.Entities;

public class Building
{
    public Guid Id { get; set; }

    public Guid CityId { get; set; }

    public City City { get; set; } = null!;

    public string Type { get; set; } = string.Empty;

    public int Level { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
