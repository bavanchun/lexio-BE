using Lexio.SharedKernel.Primitives;
using Mediator;

namespace Lexio.Identity.Application.Features.Auth.Register;

public sealed record RegisterUserCommand(
    string Email,
    string Password,
    string DisplayName,
    string? IpAddress) : ICommand<Result<AuthResponseDto>>;
