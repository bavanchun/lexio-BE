using Lexio.BuildingBlocks.Persistence;
using Lexio.Identity.Domain.Entities;
using Lexio.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;

namespace Lexio.Identity.Infrastructure.Persistence;

/// <summary>
/// Identity-specific DbContext. Inherits audit stamping, soft-delete filter,
/// and outbox event dispatch from <see cref="LexioDbContextBase"/>.
/// </summary>
public sealed class IdentityDbContext(
    DbContextOptions<IdentityDbContext> options,
    IClock clock)
    : LexioDbContextBase(options, clock)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<OAuthConnection> OAuthConnections => Set<OAuthConnection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
    }
}
