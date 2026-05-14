using Lexio.Identity.Domain.Primitives;
using Lexio.SharedKernel.Primitives;
using Mediator;

namespace Lexio.Identity.Application.Features.Users.ChangeRole;

public sealed record ChangeUserRoleCommand(
    UserId TargetUserId,
    RoleId NewRoleId,
    UserId AdminUserId) : ICommand<Result>;
