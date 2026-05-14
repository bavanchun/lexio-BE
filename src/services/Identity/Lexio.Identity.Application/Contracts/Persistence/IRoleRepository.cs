using Lexio.Identity.Domain.Entities;
using Lexio.Identity.Domain.Primitives;

namespace Lexio.Identity.Application.Contracts.Persistence;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(RoleId id, CancellationToken ct = default);
    Task<Role?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<Role?> GetDefaultLearnerRoleAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Role>> ListAllAsync(CancellationToken ct = default);
}
