using Lexio.Identity.Application.Contracts.Persistence;
using Lexio.Identity.Domain.Entities;
using Lexio.Identity.Domain.Primitives;
using Lexio.Identity.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Lexio.Identity.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(IdentityDbContext db) : IUserRepository
{
    public Task<User?> GetByIdAsync(UserId id, CancellationToken ct = default) =>
        db.Users
            .Include(u => u.RefreshTokens)
            .Include(u => u.OAuthConnections)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByEmailAsync(Email email, CancellationToken ct = default)
    {
        var raw = email.Value;
        return db.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Email.Value == raw, ct);
    }

    public async Task<User?> GetByActiveRefreshTokenHashAsync(string tokenHash, CancellationToken ct = default)
    {
        var userId = await db.RefreshTokens
            .Where(t => t.TokenHash == tokenHash)
            .Select(t => (UserId?)t.UserId)
            .FirstOrDefaultAsync(ct);

        if (userId is null) { return null; }

        return await db.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == userId.Value, ct);
    }

    public Task<bool> EmailExistsAsync(Email email, CancellationToken ct = default)
    {
        var raw = email.Value;
        return db.Users.AnyAsync(u => u.Email.Value == raw, ct);
    }

    public Task<bool> IsBannedAsync(UserId id, CancellationToken ct = default) =>
        db.Users
            .Where(u => u.Id == id)
            .Select(u => u.Status == Domain.Enums.UserStatus.Banned)
            .FirstOrDefaultAsync(ct);

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        await db.Users.AddAsync(user, ct);
    }
}
