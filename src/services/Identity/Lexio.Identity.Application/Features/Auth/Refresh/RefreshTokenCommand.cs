using Lexio.SharedKernel.Primitives;
using Mediator;

namespace Lexio.Identity.Application.Features.Auth.Refresh;

public sealed record RefreshTokenCommand(
    string RefreshToken,
    string? IpAddress) : ICommand<Result<RefreshResponseDto>>;
