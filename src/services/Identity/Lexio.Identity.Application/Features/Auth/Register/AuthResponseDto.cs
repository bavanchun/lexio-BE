using Lexio.Identity.Application.Features.Auth.Me;

namespace Lexio.Identity.Application.Features.Auth.Register;

public sealed record AuthResponseDto(
    string AccessToken,
    int ExpiresInSeconds,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    UserDto User);
