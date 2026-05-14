namespace Lexio.Identity.Domain.Primitives;

/// <summary>Strongly-typed identifier for <see cref="Entities.Role"/>.</summary>
public readonly record struct RoleId(Guid Value)
{
    public static RoleId New() => new(Guid.NewGuid());
    public static RoleId Empty { get; } = new(Guid.Empty);
    public override string ToString() => Value.ToString();
    public static implicit operator Guid(RoleId id) => id.Value;
}
