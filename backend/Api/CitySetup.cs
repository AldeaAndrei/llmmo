using llmmo.Entities;

namespace llmmo.Api;

public static class CitySetup
{
    public const int StarterWood = 500;
    public const int StarterStone = 500;
    public const int StarterGold = 250;
    public const int StarterFood = 400;
    public const int StarterTroops = 25;

    public static string BuildCityName(string playerName)
    {
        const string prefix = "City of ";
        var maxPlayerNameLength = 30 - prefix.Length;
        var trimmedName = playerName.Length > maxPlayerNameLength
            ? playerName[..maxPlayerNameLength]
            : playerName;

        return prefix + trimmedName;
    }

    public static City CreateStartingCity(Guid cityId, Guid playerId, string playerName, int x, int y) => new()
    {
        Id = cityId,
        PlayerId = playerId,
        X = x,
        Y = y,
        Name = BuildCityName(playerName),
        Wood = StarterWood,
        Stone = StarterStone,
        Gold = StarterGold,
        Food = StarterFood,
        TroopCount = StarterTroops,
    };
}
