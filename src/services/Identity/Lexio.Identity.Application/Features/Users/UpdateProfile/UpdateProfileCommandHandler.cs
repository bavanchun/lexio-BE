using Lexio.BuildingBlocks.Abstractions.Persistence;
using Lexio.Identity.Application.Common.Errors;
using Lexio.Identity.Application.Contracts.Persistence;
using Lexio.Identity.Application.Contracts.Security;
using Lexio.Identity.Application.Features.Auth.Me;
using Lexio.Identity.Domain.ValueObjects;
using Lexio.SharedKernel.Primitives;
using Lexio.SharedKernel.Time;
using MapsterMapper;
using Mediator;

namespace Lexio.Identity.Application.Features.Users.UpdateProfile;

public sealed class UpdateProfileCommandHandler(
    IUserRepository users,
    IPasswordHasher hasher,
    IUnitOfWork uow,
    IClock clock,
    IMapper mapper)
    : ICommandHandler<UpdateProfileCommand, Result<UserDto>>
{
    public async ValueTask<Result<UserDto>> Handle(UpdateProfileCommand cmd, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(cmd.UserId, cancellationToken);
        if (user is null) { return Result.Failure<UserDto>(IdentityErrors.UserNotFound); }

        if (!string.IsNullOrWhiteSpace(cmd.NewPassword))
        {
            if (user.PasswordHash is null) { return Result.Failure<UserDto>(IdentityErrors.PasswordNotSet); }
            if (!hasher.Verify(cmd.CurrentPassword, user.PasswordHash.Value))
            {
                return Result.Failure<UserDto>(IdentityErrors.CurrentPasswordMismatch);
            }

            var newHashResult = PasswordHash.Create(hasher.Hash(cmd.NewPassword));
            if (newHashResult.IsFailure) { return Result.Failure<UserDto>(newHashResult.Error); }
            user.ChangePassword(newHashResult.Value, clock);
        }

        if (!string.IsNullOrWhiteSpace(cmd.DisplayName))
        {
            var dnResult = DisplayName.Create(cmd.DisplayName);
            if (dnResult.IsFailure) { return Result.Failure<UserDto>(dnResult.Error); }
            user.ChangeDisplayName(dnResult.Value);
        }

        await uow.SaveChangesAsync(cancellationToken);
        return Result.Success(mapper.Map<UserDto>(user));
    }
}
