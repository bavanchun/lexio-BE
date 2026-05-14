namespace Lexio.Identity.Application.Features.Auth.Refresh;

public sealed record RefreshResponseDto(
    string AccessToken,
    int ExpiresInSeconds,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);
