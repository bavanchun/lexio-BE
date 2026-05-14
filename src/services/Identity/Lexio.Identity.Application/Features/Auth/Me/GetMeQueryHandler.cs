using Lexio.Identity.Application.Common.Errors;
using Lexio.Identity.Application.Contracts.Persistence;
using Lexio.SharedKernel.Primitives;
using MapsterMapper;
using Mediator;

namespace Lexio.Identity.Application.Features.Auth.Me;

public sealed class GetMeQueryHandler(IUserRepository users, IMapper mapper)
    : IQueryHandler<GetMeQuery, Result<UserDto>>
{
    public async ValueTask<Result<UserDto>> Handle(GetMeQuery q, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(q.UserId, cancellationToken);
        return user is null
            ? Result.Failure<UserDto>(IdentityErrors.UserNotFound)
            : Result.Success(mapper.Map<UserDto>(user));
    }
}
