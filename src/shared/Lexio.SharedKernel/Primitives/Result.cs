namespace Lexio.SharedKernel.Primitives;

/// <summary>
/// Non-generic result type for operations that return no value on success.
/// Universal error transport — every Application handler returns Result or Result&lt;T&gt;.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new ArgumentException("Success result cannot carry an error.", nameof(error));
        }

        if (!isSuccess && error == Error.None)
        {
            throw new ArgumentException("Failure result must carry an error.", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>True when the operation succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>True when the operation failed.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>The error, if any. <see cref="Error.None"/> on success.</summary>
    public Error Error { get; }

    // S4136: all Success/Failure overloads (non-generic + generic) placed adjacently below.

    /// <summary>Creates a successful non-value result.</summary>
    public static Result Success() => new(true, Error.None);

    /// <summary>Lift a value into a successful <see cref="Result{T}"/>.</summary>
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);

    /// <summary>Creates a failed non-value result.</summary>
    public static Result Failure(Error error) => new(false, error);

    /// <summary>Lift an error into a failed <see cref="Result{T}"/>.</summary>
    public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);
}
