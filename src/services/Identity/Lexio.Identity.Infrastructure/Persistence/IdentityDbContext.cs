using Lexio.BuildingBlocks.Persistence;
using Lexio.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;

namespace Lexio.Identity.Infrastructure.Persistence;

/// <summary>
/// Identity-specific DbContext. Inherits audit stamping, soft-delete filter,
/// and outbox event dispatch from LexioDbContextBase.
/// Add entity DbSets and model configuration here.
/// </summary>
public sealed class IdentityDbContext(
    DbContextOptions<IdentityDbContext> options,
    IClock clock)
    : LexioDbContextBase(options, clock)
{
    // Add DbSet<YourEntity> properties here as you implement domain entities.

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // Important: base wires outbox + soft-delete

        // Apply entity configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
    }
}
