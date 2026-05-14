using Lexio.SharedKernel.Primitives;
using Mediator;

namespace Lexio.Identity.Application.Features.Roles.List;

public sealed record GetRolesQuery : IQuery<Result<IReadOnlyList<RoleDto>>>;
