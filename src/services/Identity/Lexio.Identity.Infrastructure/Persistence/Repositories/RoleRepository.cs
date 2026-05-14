using Lexio.Identity.Application.Contracts.Persistence;
using Lexio.Identity.Domain.Entities;
using Lexio.Identity.Domain.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Lexio.Identity.Infrastructure.Persistence.Repositories;

public sealed class RoleRepository(IdentityDbContext db) : IRoleRepository
{
    public Task<Role?> GetByIdAsync(RoleId id, CancellationToken ct = default) =>
        db.Roles.FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<Role?> GetByNameAsync(string name, CancellationToken ct = default) =>
        db.Roles.FirstOrDefaultAsync(r => r.Name == name, ct);

    public Task<Role?> GetDefaultLearnerRoleAsync(CancellationToken ct = default) =>
        db.Roles.FirstOrDefaultAsync(r => r.Id == Role.SeedIds.Learner, ct);

    public async Task<IReadOnlyList<Role>> ListAllAsync(CancellationToken ct = default) =>
        await db.Roles.AsNoTracking().OrderBy(r => r.Name).ToListAsync(ct);
}
