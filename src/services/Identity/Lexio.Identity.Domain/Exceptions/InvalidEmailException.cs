namespace Lexio.Identity.Domain.Exceptions;

public sealed class InvalidEmailException : DomainException
{
    public InvalidEmailException(string message) : base(message) { }
}
