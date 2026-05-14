using Lexio.BuildingBlocks.Abstractions.Persistence;
using Lexio.Identity.Application.Common.Errors;
using Lexio.Identity.Application.Contracts.Persistence;
using Lexio.SharedKernel.Primitives;
using Lexio.SharedKernel.Time;
using Mediator;

namespace Lexio.Identity.Application.Features.Auth.Logout;

public sealed class LogoutCommandHandler(
    IUserRepository users,
    IUnitOfWork uow,
    IClock clock)
    : ICommandHandler<LogoutCommand, Result>
{
    public async ValueTask<Result> Handle(LogoutCommand cmd, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(cmd.UserId, cancellationToken);
        if (user is null) { return Result.Failure(IdentityErrors.UserNotFound); }

        user.RevokeAllRefreshTokens(clock);
        await uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
