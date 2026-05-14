namespace Lexio.Identity.Application.Features.Roles.List;

public sealed record RoleDto(
    Guid Id,
    string Name,
    string Description,
    IReadOnlyList<string> Permissions);
