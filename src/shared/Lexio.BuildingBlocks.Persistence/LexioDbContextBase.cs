using System.Text.Json;
using Lexio.BuildingBlocks.Abstractions.Persistence;
using Lexio.SharedKernel.Domain;
using Lexio.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;

namespace Lexio.BuildingBlocks.Persistence;

/// <summary>
/// Base DbContext for all Lexio service databases.
/// Responsibilities:
///   1. Stamp audit fields (CreatedAt, UpdatedAt, CreatedBy) on SaveChanges.
///   2. Collect domain events from AggregateRoot entities.
///   3. Persist them atomically as OutboxMessage rows.
///   4. Apply soft-delete global query filter for ISoftDeletableEntity.
///
/// IMPORTANT: Always use IUnitOfWork.SaveChangesAsync — do NOT call base.SaveChangesAsync
/// directly from application code, as that bypasses the outbox + event dispatch.
/// </summary>
public abstract class LexioDbContextBase : DbContext, IUnitOfWork
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IClock _clock;
    private readonly string? _currentUserId;

    /// <summary>Outbox messages table — written atomically with aggregate changes.</summary>
    public DbSet<OutboxMessageEntity> OutboxMessages => Set<OutboxMessageEntity>();

    /// <summary>Constructor for dependency injection.</summary>
    protected LexioDbContextBase(
        DbContextOptions options,
        IClock clock,
        string? currentUserId = null)
        : base(options)
    {
        _clock = clock;
        _currentUserId = currentUserId;
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new OutboxMessageEntityTypeConfiguration());

        // Apply soft-delete query filter to all ISoftDeletableEntity types
        foreach (var clrType in modelBuilder.Model.GetEntityTypes()
            .Select(t => t.ClrType)
            .Where(t => typeof(ISoftDeletableEntity).IsAssignableFrom(t)))
        {
            modelBuilder.Entity(clrType).HasQueryFilter(BuildSoftDeleteFilter(clrType));
        }
    }

    /// <summary>
    /// Persist all pending changes, stamp audit fields, and write domain events to the outbox —
    /// all within a single database transaction.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;

        StampAuditFields(now);

        var outboxMessages = CollectOutboxMessages();

        if (outboxMessages.Count > 0)
        {
            OutboxMessages.AddRange(outboxMessages);
        }

        return await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void StampAuditFields(DateTimeOffset now)
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is IAuditableEntity auditable)
            {
                if (entry.State == EntityState.Added)
                {
                    auditable.CreatedAt = now;
                    auditable.CreatedBy = _currentUserId;
                }

                if (entry.State is EntityState.Added or EntityState.Modified)
                {
                    auditable.UpdatedAt = now;
                }
            }

            // Soft-delete: intercept Delete → set IsDeleted + Modified
            if (entry.State == EntityState.Deleted && entry.Entity is ISoftDeletableEntity softDeletable)
            {
                entry.State = EntityState.Modified;
                softDeletable.IsDeleted = true;
                softDeletable.DeletedAt = now;
            }
        }
    }

    private List<OutboxMessageEntity> CollectOutboxMessages()
    {
        var aggregates = ChangeTracker.Entries<AggregateRoot<object>>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        var outboxMessages = aggregates
            .SelectMany(a => a.DomainEvents)
            .Select(domainEvent => new OutboxMessageEntity
            {
                Id = domainEvent.Id,
                Type = domainEvent.GetType().FullName ?? domainEvent.GetType().Name,
                Payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), JsonOptions),
                OccurredAt = domainEvent.OccurredAt,
                ProcessedAt = null,
            })
            .ToList();

        // Clear events after collecting — before base.SaveChanges
        foreach (var aggregate in aggregates)
        {
            aggregate.ClearDomainEvents();
        }

        return outboxMessages;
    }

    private static System.Linq.Expressions.LambdaExpression BuildSoftDeleteFilter(Type entityType)
    {
        var param = System.Linq.Expressions.Expression.Parameter(entityType, "e");
        var prop = System.Linq.Expressions.Expression.Property(param, nameof(ISoftDeletableEntity.IsDeleted));
        var body = System.Linq.Expressions.Expression.Not(prop);
        return System.Linq.Expressions.Expression.Lambda(body, param);
    }
}
