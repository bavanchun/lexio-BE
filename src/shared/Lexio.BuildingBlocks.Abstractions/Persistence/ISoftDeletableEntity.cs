namespace Lexio.BuildingBlocks.Abstractions.Persistence;

/// <summary>
/// Marker interface for entities supporting soft-delete. <c>LexioDbContextBase</c>
/// applies a global query filter <c>e =&gt; !e.IsDeleted</c> and converts hard
/// deletes into a state update. Lives in Abstractions so Domain layers can
/// implement it without referencing EF Core.
/// </summary>
public interface ISoftDeletableEntity
{
    bool IsDeleted { get; set; }
    DateTimeOffset? DeletedAt { get; set; }
}
