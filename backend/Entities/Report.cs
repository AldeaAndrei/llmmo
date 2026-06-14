namespace llmmo.Entities;

public class Report
{
    public Guid Id { get; set; }

    public Guid PlayerId { get; set; }

    public Player Player { get; set; } = null!;

    public string Type { get; set; } = string.Empty;

    public Guid? AttackId { get; set; }

    public MilitaryAttack? Attack { get; set; }

    public Guid SourceCityId { get; set; }

    public Guid? TargetCityId { get; set; }

    public int TargetX { get; set; }

    public int TargetY { get; set; }

    public string Payload { get; set; } = "{}";

    public DateTime CreatedAt { get; set; }

    public DateTime? ReadAt { get; set; }
}
