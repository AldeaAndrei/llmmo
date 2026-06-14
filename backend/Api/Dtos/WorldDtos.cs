namespace llmmo.Api.Dtos;

public record WorldDto(
    int CurrentTick,
    int TickIntervalSeconds,
    DateTime LastTickAt,
    DateTime NextTickAt,
    int WorldSeed,
    int MapSize);
