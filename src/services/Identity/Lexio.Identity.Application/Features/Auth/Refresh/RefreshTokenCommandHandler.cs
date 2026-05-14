using Lexio.BuildingBlocks.Abstractions.Persistence;
using Lexio.Identity.Application.Common.Errors;
using Lexio.Identity.Application.Contracts.Persistence;
using Lexio.Identity.Application.Contracts.Security;
using Lexio.Identity.Domain.Enums;
using Lexio.SharedKernel.Primitives;
using Lexio.SharedKernel.Time;
using Mediator;

namespace Lexio.Identity.Application.Features.Auth.Refresh;

public sealed class RefreshTokenCommandHandler(
    IUserRepository users,
    IRoleRepository roles,
    ITokenIssuer tokens,
    IUnitOfWork uow,
    IClock clock)
    : ICommandHandler<RefreshTokenCommand, Result<RefreshResponseDto>>
{
    public async ValueTask<Result<RefreshResponseDto>> Handle(RefreshTokenCommand cmd, CancellationToken cancellationToken)
    {
        // Deterministic, non-salted hash so storage rows can be looked up by raw input.
        var lookupHash = tokens.ComputeStorageHash(cmd.RefreshToken);
        var user = await users.GetByActiveRefreshTokenHashAsync(lookupHash, cancellationToken);

        if (user is null || user.Status == UserStatus.Banned)
        {
            return Result.Failure<RefreshResponseDto>(IdentityErrors.InvalidRefreshToken);
        }

        var current = user.RefreshTokens.FirstOrDefault(t => t.TokenHash == lookupHash && t.IsActive(clock));
        if (current is null)
        {
            return Result.Failure<RefreshResponseDto>(IdentityErrors.InvalidRefreshToken);
        }

        var role = await roles.GetByIdAsync(user.RoleId, cancellationToken)
            ?? throw new InvalidOperationException($"User {user.Id} references missing role {user.RoleId}.");

        user.RevokeRefreshToken(current.Id, clock);
        var refresh = tokens.IssueRefreshToken();
        user.IssueRefreshToken(refresh.HashedForStorage, refresh.ExpiresAt, cmd.IpAddress, clock);
        var access = tokens.IssueAccessToken(user, role);

        await uow.SaveChangesAsync(cancellationToken);

        return Result.Success(new RefreshResponseDto(
            access.Jwt, access.ExpiresInSeconds,
            refresh.RawToken, refresh.ExpiresAt));
    }
}
