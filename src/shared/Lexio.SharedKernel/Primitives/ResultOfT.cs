namespace Lexio.SharedKernel.Primitives;

/// <summary>
/// Generic result type carrying a value on success.
/// Accessing <see cref="Value"/> on a failure throws <see cref="InvalidOperationException"/>
/// with an explicit message — never a NullReferenceException.
/// </summary>
/// <typeparam name="T">The success value type.</typeparam>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T value) : base(true, Error.None) => _value = value;
    private Result(Error error) : base(false, error) { }

    /// <summary>
    /// The success value. Throws if this result represents a failure.
    /// Always check <see cref="Result.IsSuccess"/> before accessing.
    /// </summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException(
            $"Cannot access Value on a failed Result. Error: [{Error.Code}] {Error.Message}");

    public static Result<T> Success(T value) => new(value);
    public new static Result<T> Failure(Error error) => new(error);

    /// <summary>Implicit lift: allows returning a T directly where Result&lt;T&gt; is expected.</summary>
    public static implicit operator Result<T>(T value) => Success(value);

    /// <summary>Implicit lift: allows returning an Error directly where Result&lt;T&gt; is expected.</summary>
    public static implicit operator Result<T>(Error error) => Failure(error);
}
