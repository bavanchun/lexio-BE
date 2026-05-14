namespace Lexio.Identity.Domain.Exceptions;

/// <summary>
/// Base type for Identity domain invariant violations. Thrown by aggregate methods
/// (state-transition guards). Factory creation paths return <c>Result&lt;T&gt;</c>
/// instead of throwing.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
    protected DomainException(string message, Exception inner) : base(message, inner) { }
}
