namespace Lexio.BuildingBlocks.Persistence;

/// <summary>
/// Interface for entities with soft-delete support.
/// LexioDbContextBase applies a global query filter: e => !e.IsDeleted.
/// </summary>
public interface ISoftDeletableEntity
{
    bool IsDeleted { get; set; }
    DateTimeOffset? DeletedAt { get; set; }
}
