namespace llmmo.Api;

public static class MapDistance
{
    public static int Manhattan(int x1, int y1, int x2, int y2) =>
        Math.Abs(x1 - x2) + Math.Abs(y1 - y2);

    public static int TravelTicks(int manhattan, int partySpeedTilesPerTick)
    {
        var speed = Math.Max(1, partySpeedTilesPerTick);
        return Math.Max(1, (int)Math.Ceiling(manhattan / (double)speed));
    }
}
