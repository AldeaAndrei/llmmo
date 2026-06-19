using System.Text.Json;
using llmmo.Api;

namespace CalcTests;

public class ActionPayloadHelperTests
{
    [Fact]
    public void GetReason_ReturnsTrimmedString()
    {
        var payload = JsonDocument.Parse(
            """{"buildingType":"gold_mine","reason":"  Need more gold  "}"""
        ).RootElement;

        Assert.Equal("Need more gold", ActionPayloadHelper.GetReason(payload));
    }

    [Fact]
    public void GetReason_ReturnsNullWhenMissing()
    {
        var payload = JsonDocument.Parse("""{"buildingType":"gold_mine"}""").RootElement;

        Assert.Null(ActionPayloadHelper.GetReason(payload));
    }

    [Fact]
    public void ValidateReason_RejectsOverMaxLength()
    {
        var longReason = new string('x', ActionPayloadHelper.MaxReasonLength + 1);

        var error = ActionPayloadHelper.ValidateReason(longReason);

        Assert.NotNull(error);
        Assert.Contains("500", error);
    }

    [Fact]
    public void ValidateReason_AllowsNull()
    {
        Assert.Null(ActionPayloadHelper.ValidateReason(null));
    }
}

public class ActionReasonSerializationTests
{
    [Fact]
    public void SerializePayload_IncludesReasonWhenSet()
    {
        var json = ActionPayloadHelper.SerializePayload(new
        {
            buildingType = "gold_mine",
            reason = "Gold is lowest resource",
            deducted = new { Wood = 10, Stone = 0, Gold = 0, Food = 0 },
        });

        var element = JsonDocument.Parse(json).RootElement;

        Assert.Equal("gold_mine", element.GetProperty("buildingType").GetString());
        Assert.Equal("Gold is lowest resource", ActionPayloadHelper.GetReason(element));
    }
}
