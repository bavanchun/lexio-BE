using Lexio.Identity.Application.Contracts.Persistence;
using Lexio.SharedKernel.Primitives;
using MapsterMapper;
using Mediator;

namespace Lexio.Identity.Application.Features.Roles.List;

public sealed class GetRolesQueryHandler(IRoleRepository roles, IMapper mapper)
    : IQueryHandler<GetRolesQuery, Result<IReadOnlyList<RoleDto>>>
{
    public async ValueTask<Result<IReadOnlyList<RoleDto>>> Handle(GetRolesQuery q, CancellationToken cancellationToken)
    {
        var all = await roles.ListAllAsync(cancellationToken);
        return Result.Success<IReadOnlyList<RoleDto>>(
            all.Select(r => mapper.Map<RoleDto>(r)).ToList());
    }
}
