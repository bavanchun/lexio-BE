using Lexio.Identity.Application.Features.Auth.Register;
using Lexio.SharedKernel.Primitives;
using Mediator;

namespace Lexio.Identity.Application.Features.Auth.Login;

public sealed record LoginCommand(
    string Email,
    string Password,
    string? IpAddress) : ICommand<Result<AuthResponseDto>>;
