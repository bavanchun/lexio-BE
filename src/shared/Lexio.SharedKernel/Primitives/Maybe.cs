namespace Lexio.SharedKernel.Primitives;

/// <summary>
/// Optional wrapper — explicit alternative to nullable reference types for domain use.
/// Prevents accidental null propagation in query handlers that may return "not found".
/// Works correctly for both reference types and value types.
/// </summary>
/// <typeparam name="T">The wrapped value type.</typeparam>
public sealed class Maybe<T>
{
    private readonly T? _value;
    private readonly bool _hasValue;

    private Maybe()
    {
        _hasValue = false;
    }

    private Maybe(T value)
    {
        _value = value;
        _hasValue = true;
    }

    /// <summary>True when a value is present.</summary>
    public bool HasValue => _hasValue;

    /// <summary>True when no value is present.</summary>
    public bool HasNoValue => !_hasValue;

    /// <summary>
    /// The wrapped value. Throws <see cref="InvalidOperationException"/> when <see cref="HasNoValue"/>.
    /// Always check <see cref="HasValue"/> before accessing.
    /// </summary>
    public T Value => _hasValue
        ? _value!
        : throw new InvalidOperationException("Maybe has no value.");

    /// <summary>Creates a Maybe wrapping <paramref name="value"/>. Returns empty when value is null.</summary>
    public static Maybe<T> From(T? value) =>
        value is null ? new Maybe<T>() : new Maybe<T>(value);

    /// <summary>Returns an empty Maybe (no value).</summary>
    public static Maybe<T> None => new();

    /// <summary>Implicit conversion from T: allows returning a value directly where Maybe&lt;T&gt; is expected.</summary>
    public static implicit operator Maybe<T>(T? value) => From(value);

    /// <inheritdoc />
    public override string ToString() => _hasValue ? _value!.ToString() ?? string.Empty : "None";
}
