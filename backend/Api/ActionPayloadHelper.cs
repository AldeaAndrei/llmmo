using System.Text.Json;
using llmmo.Api.Dtos;

namespace llmmo.Api;

public static class ActionPayloadHelper
{
    public static JsonElement ToJsonElement(object payload)
    {
        if (payload is JsonElement element)
        {
            return element;
        }

        var json = JsonSerializer.Serialize(payload);
        return JsonSerializer.Deserialize<JsonElement>(json);
    }

    public static string? GetBuildingType(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!payload.TryGetProperty("buildingType", out var property))
        {
            return null;
        }

        return property.GetString();
    }

    public static int GetTrainCount(JsonElement payload, int defaultCount = 5)
    {
        if (payload.ValueKind != JsonValueKind.Object)
        {
            return defaultCount;
        }

        if (!payload.TryGetProperty("count", out var property))
        {
            return defaultCount;
        }

        return property.TryGetInt32(out var count) ? count : defaultCount;
    }

    public static Guid? GetTargetCityId(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!payload.TryGetProperty("targetCityId", out var property))
        {
            return null;
        }

        return property.TryGetGuid(out var cityId) ? cityId : null;
    }

    public static (int X, int Y)? GetTargetCoordinates(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!payload.TryGetProperty("targetX", out var xProperty)
            || !payload.TryGetProperty("targetY", out var yProperty))
        {
            return null;
        }

        if (!xProperty.TryGetInt32(out var x) || !yProperty.TryGetInt32(out var y))
        {
            return null;
        }

        return (x, y);
    }

    public static BuildingUpgradeCostDto? GetDeducted(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!payload.TryGetProperty("deducted", out var deducted)
            || deducted.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return new BuildingUpgradeCostDto(
            GetInt(deducted, "wood"),
            GetInt(deducted, "stone"),
            GetInt(deducted, "gold"),
            GetInt(deducted, "food"));
    }

    public static string SerializePayload(object payloadFields)
    {
        return JsonSerializer.Serialize(payloadFields);
    }

    private static int GetInt(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var property))
        {
            return 0;
        }

        return property.TryGetInt32(out var value) ? value : 0;
    }
}
