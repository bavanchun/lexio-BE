using Lexio.BuildingBlocks.Abstractions.Persistence;
using Lexio.Identity.Application.Common.Errors;
using Lexio.Identity.Application.Contracts.Persistence;
using Lexio.SharedKernel.Primitives;
using Lexio.SharedKernel.Time;
using Mediator;

namespace Lexio.Identity.Application.Features.Users.ChangeRole;

public sealed class ChangeUserRoleCommandHandler(
    IUserRepository users,
    IRoleRepository roles,
    IUnitOfWork uow,
    IClock clock)
    : ICommandHandler<ChangeUserRoleCommand, Result>
{
    public async ValueTask<Result> Handle(ChangeUserRoleCommand cmd, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(cmd.TargetUserId, cancellationToken);
        if (user is null) { return Result.Failure(IdentityErrors.UserNotFound); }

        var role = await roles.GetByIdAsync(cmd.NewRoleId, cancellationToken);
        if (role is null) { return Result.Failure(IdentityErrors.RoleNotFound); }

        user.ChangeRole(cmd.NewRoleId, cmd.AdminUserId, clock);
        await uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
