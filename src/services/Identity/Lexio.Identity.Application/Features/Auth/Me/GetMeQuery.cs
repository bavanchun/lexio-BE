using Lexio.Identity.Domain.Primitives;
using Lexio.SharedKernel.Primitives;
using Mediator;

namespace Lexio.Identity.Application.Features.Auth.Me;

public sealed record GetMeQuery(UserId UserId) : IQuery<Result<UserDto>>;
