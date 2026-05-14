using Lexio.Identity.Domain.Primitives;
using Lexio.SharedKernel.Primitives;
using Mediator;

namespace Lexio.Identity.Application.Features.Auth.Logout;

public sealed record LogoutCommand(UserId UserId) : ICommand<Result>;
