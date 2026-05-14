using Lexio.BuildingBlocks.Abstractions.Persistence;
using Lexio.Identity.Application.Common.Errors;
using Lexio.Identity.Application.Contracts.Auditing;
using Lexio.Identity.Application.Contracts.Persistence;
using Lexio.Identity.Application.Contracts.Security;
using Lexio.Identity.Application.Features.Auth.Me;
using Lexio.Identity.Application.Features.Auth.Register;
using Lexio.Identity.Domain.Enums;
using Lexio.Identity.Domain.ValueObjects;
using Lexio.SharedKernel.Primitives;
using Lexio.SharedKernel.Time;
using MapsterMapper;
using Mediator;

namespace Lexio.Identity.Application.Features.Auth.Login;

public sealed class LoginCommandHandler(
    IUserRepository users,
    IRoleRepository roles,
    IPasswordHasher hasher,
    ITokenIssuer tokens,
    IAuditLogger audit,
    IUnitOfWork uow,
    IClock clock,
    IMapper mapper)
    : ICommandHandler<LoginCommand, Result<AuthResponseDto>>
{
    public async ValueTask<Result<AuthResponseDto>> Handle(LoginCommand cmd, CancellationToken cancellationToken)
    {
        var emailResult = Email.Create(cmd.Email);
        if (emailResult.IsFailure) { return Result.Failure<AuthResponseDto>(IdentityErrors.InvalidCredentials); }

        var user = await users.GetByEmailAsync(emailResult.Value, cancellationToken);

        // Always run Verify to keep timing constant regardless of user existence.
        var passwordOk = hasher.Verify(cmd.Password, user?.PasswordHash?.Value);

        if (user is null || !passwordOk)
        {
            await audit.LogAsync(new AuditEvent(
                "LoginFailed",
                user?.Id.Value,
                cmd.IpAddress,
                new Dictionary<string, object?> { ["email"] = cmd.Email }), cancellationToken);
            return Result.Failure<AuthResponseDto>(IdentityErrors.InvalidCredentials);
        }

        if (user.Status == UserStatus.Banned)
        {
            return Result.Failure<AuthResponseDto>(IdentityErrors.AccountBanned);
        }

        var role = await roles.GetByIdAsync(user.RoleId, cancellationToken)
            ?? throw new InvalidOperationException($"User {user.Id} references missing role {user.RoleId}.");

        user.RecordLogin(cmd.IpAddress, clock);
        var refresh = tokens.IssueRefreshToken();
        user.IssueRefreshToken(refresh.HashedForStorage, refresh.ExpiresAt, cmd.IpAddress, clock);
        var access = tokens.IssueAccessToken(user, role);

        await uow.SaveChangesAsync(cancellationToken);

        return Result.Success(new AuthResponseDto(
            access.Jwt, access.ExpiresInSeconds,
            refresh.RawToken, refresh.ExpiresAt,
            mapper.Map<UserDto>(user)));
    }
}
