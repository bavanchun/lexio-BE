namespace Lexio.Identity.Application.Features.Auth.Me;

public sealed record UserDto(
    Guid Id,
    string Email,
    string DisplayName,
    Guid RoleId,
    string Status,
    bool IsVerified,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt);
