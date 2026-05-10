namespace Lexio.BuildingBlocks.Persistence;

/// <summary>
/// Interface for entities that need audit fields stamped by LexioDbContextBase.
/// Apply to EF entity classes that inherit from AggregateRoot or Entity.
/// </summary>
public interface IAuditableEntity
{
    DateTimeOffset CreatedAt { get; set; }
    DateTimeOffset UpdatedAt { get; set; }
    string? CreatedBy { get; set; }
}
