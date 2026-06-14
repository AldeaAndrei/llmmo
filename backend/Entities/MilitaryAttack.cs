namespace llmmo.Entities;

public class MilitaryAttack
{
    public Guid Id { get; set; }

    public Guid PlayerId { get; set; }

    public Player Player { get; set; } = null!;

    public Guid SourceCityId { get; set; }

    public City SourceCity { get; set; } = null!;

    public Guid? TargetCityId { get; set; }

    public City? TargetCity { get; set; }

    public int TargetX { get; set; }

    public int TargetY { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Troops { get; set; } = "[]";

    public string? Survivors { get; set; }

    public int OutboundDurationTicks { get; set; }

    public int ReturnDurationTicks { get; set; }

    public int DepartedAtTick { get; set; }

    public int ArrivesAtTick { get; set; }

    public int? ReturnsAtTick { get; set; }

    public int LootWood { get; set; }

    public int LootStone { get; set; }

    public int LootGold { get; set; }

    public int LootFood { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
