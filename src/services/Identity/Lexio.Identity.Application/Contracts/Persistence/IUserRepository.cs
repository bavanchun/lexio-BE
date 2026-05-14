using Lexio.Identity.Domain.Entities;
using Lexio.Identity.Domain.Primitives;
using Lexio.Identity.Domain.ValueObjects;

namespace Lexio.Identity.Application.Contracts.Persistence;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(UserId id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(Email email, CancellationToken ct = default);
    Task<User?> GetByActiveRefreshTokenHashAsync(string tokenHash, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(Email email, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
}
