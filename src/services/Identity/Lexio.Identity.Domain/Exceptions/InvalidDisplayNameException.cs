namespace Lexio.Identity.Domain.Exceptions;

public sealed class InvalidDisplayNameException : DomainException
{
    public InvalidDisplayNameException(string message) : base(message) { }
}
