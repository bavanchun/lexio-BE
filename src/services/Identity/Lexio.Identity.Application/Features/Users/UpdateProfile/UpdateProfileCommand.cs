using Lexio.Identity.Application.Features.Auth.Me;
using Lexio.Identity.Domain.Primitives;
using Lexio.SharedKernel.Primitives;
using Mediator;

namespace Lexio.Identity.Application.Features.Users.UpdateProfile;

public sealed record UpdateProfileCommand(
    UserId UserId,
    string? DisplayName,
    string? CurrentPassword,
    string? NewPassword) : ICommand<Result<UserDto>>;
