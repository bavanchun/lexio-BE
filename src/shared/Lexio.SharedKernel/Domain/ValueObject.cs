namespace Lexio.SharedKernel.Domain;

/// <summary>
/// Base class for value objects. Structural equality — two value objects are equal
/// iff all their equality components are equal in sequence.
/// </summary>
// S4035 suppressed: ValueObject is intentionally abstract/unsealed. Derived types ARE the concrete
// "sealed" value objects (e.g. Money, Email). Implementing IEqualityComparer<T> here is not the
// right pattern for a DDD base class; each subtype provides its own components via the template method.
#pragma warning disable S4035
public abstract class ValueObject : IEquatable<ValueObject>
#pragma warning restore S4035
{
    /// <summary>
    /// Return all components that contribute to equality.
    /// Order matters — components are compared sequentially.
    /// </summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    /// <inheritdoc />
    public bool Equals(ValueObject? other)
    {
        if (other is null) { return false; }
        if (ReferenceEquals(this, other)) { return true; }
        if (GetType() != other.GetType()) { return false; }
        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ValueObject vo && Equals(vo);

    /// <inheritdoc />
    public override int GetHashCode() =>
        GetEqualityComponents()
            .Aggregate(new HashCode(), (hc, o) => { hc.Add(o); return hc; }, hc => hc.ToHashCode());

    /// <summary>Value equality.</summary>
    public static bool operator ==(ValueObject? left, ValueObject? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Negation of ==.</summary>
    public static bool operator !=(ValueObject? left, ValueObject? right) => !(left == right);
}
