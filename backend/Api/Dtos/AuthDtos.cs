namespace llmmo.Api.Dtos;

public record RegisterRequest(
    string Email,
    string Password,
    string PlayerName,
    int? X,
    int? Y);

public record LoginRequest(
    string Email,
    string Password);

public record AuthMeDto(
    Guid UserId,
    Guid PlayerId,
    string PlayerName,
    string PlayerType,
    string Email);

public record RegisterResponseDto(
    Guid UserId,
    Guid PlayerId,
    Guid CityId,
    string PlayerName,
    string Email);
