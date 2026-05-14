using Lexio.BuildingBlocks.Abstractions.Persistence;
using Lexio.Identity.Application.Common.Errors;
using Lexio.Identity.Application.Contracts.Auditing;
using Lexio.Identity.Application.Contracts.Persistence;
using Lexio.Identity.Application.Contracts.Security;
using Lexio.Identity.Application.Features.Auth.Me;
using Lexio.Identity.Domain.Entities;
using Lexio.Identity.Domain.ValueObjects;
using Lexio.SharedKernel.Primitives;
using Lexio.SharedKernel.Time;
using MapsterMapper;
using Mediator;

namespace Lexio.Identity.Application.Features.Auth.Register;

public sealed class RegisterUserCommandHandler(
    IUserRepository users,
    IRoleRepository roles,
    IPasswordHasher hasher,
    ITokenIssuer tokens,
    IAuditLogger audit,
    IUnitOfWork uow,
    IClock clock,
    IMapper mapper)
    : ICommandHandler<RegisterUserCommand, Result<AuthResponseDto>>
{
    public async ValueTask<Result<AuthResponseDto>> Handle(RegisterUserCommand cmd, CancellationToken cancellationToken)
    {
        var emailResult = Email.Create(cmd.Email);
        if (emailResult.IsFailure) { return Result.Failure<AuthResponseDto>(emailResult.Error); }

        var displayResult = DisplayName.Create(cmd.DisplayName);
        if (displayResult.IsFailure) { return Result.Failure<AuthResponseDto>(displayResult.Error); }

        if (await users.EmailExistsAsync(emailResult.Value, cancellationToken))
        {
            return Result.Failure<AuthResponseDto>(IdentityErrors.EmailAlreadyExists);
        }

        var defaultRole = await roles.GetDefaultLearnerRoleAsync(cancellationToken)
            ?? throw new InvalidOperationException("Default learner role is missing — seed must run before registration.");

        var hashResult = PasswordHash.Create(hasher.Hash(cmd.Password));
        if (hashResult.IsFailure) { return Result.Failure<AuthResponseDto>(hashResult.Error); }

        var user = User.Register(emailResult.Value, hashResult.Value, displayResult.Value, defaultRole.Id, clock);
        await users.AddAsync(user, cancellationToken);

        var refresh = tokens.IssueRefreshToken();
        user.IssueRefreshToken(refresh.HashedForStorage, refresh.ExpiresAt, cmd.IpAddress, clock);
        var access = tokens.IssueAccessToken(user, defaultRole);

        await uow.SaveChangesAsync(cancellationToken);

        await audit.LogAsync(new AuditEvent(
            "UserRegistered",
            user.Id.Value,
            cmd.IpAddress,
            new Dictionary<string, object?> { ["email"] = user.Email.Value }), cancellationToken);

        return Result.Success(new AuthResponseDto(
            access.Jwt, access.ExpiresInSeconds,
            refresh.RawToken, refresh.ExpiresAt,
            mapper.Map<UserDto>(user)));
    }
}
