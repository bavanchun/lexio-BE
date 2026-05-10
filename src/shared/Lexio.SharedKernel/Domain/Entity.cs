namespace Lexio.SharedKernel.Domain;

/// <summary>
/// Base class for all domain entities. Identity-based equality — two entities are
/// equal iff their Id values are equal (regardless of reference).
/// </summary>
/// <typeparam name="TId">Strongly-typed identifier (e.g. a record struct wrapping Guid).</typeparam>
// S4035 suppressed: Entity<TId> is intentionally abstract/unsealed — it IS the base class
// for all aggregate roots and leaf entities. Sealing it would defeat its purpose.
// IEquatable<Entity<TId>> is implemented, satisfying the equality contract.
#pragma warning disable S4035
public abstract class Entity<TId> : IEquatable<Entity<TId>>
#pragma warning restore S4035
    where TId : notnull
{
    /// <summary>Entity identifier. Set once at construction; never mutated.</summary>
    public TId Id { get; protected set; }

    /// <summary>Parameterless ctor required by EF Core. Protected to prevent direct instantiation.</summary>
    protected Entity() => Id = default!;

    /// <summary>Primary constructor setting the entity identity.</summary>
    protected Entity(TId id) => Id = id;

    /// <inheritdoc />
    public bool Equals(Entity<TId>? other)
    {
        if (other is null) { return false; }
        if (ReferenceEquals(this, other)) { return true; }
        if (GetType() != other.GetType()) { return false; }
        return Id.Equals(other.Id);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Entity<TId> entity && Equals(entity);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    /// <summary>Value equality via <see cref="Equals(Entity{TId}?)"/>.</summary>
    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Negation of ==.</summary>
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !(left == right);
}
