namespace Lexio.BuildingBlocks.Abstractions.Persistence;

/// <summary>
/// Marker interface for entities whose audit fields are stamped automatically
/// by <c>LexioDbContextBase</c> on insert/update. Has no infrastructure dependency
/// so Domain layers can implement it without referencing EF Core.
/// </summary>
public interface IAuditableEntity
{
    DateTimeOffset CreatedAt { get; set; }
    DateTimeOffset UpdatedAt { get; set; }
    string? CreatedBy { get; set; }
}
