using Lexio.Identity.Domain.Primitives;

namespace Lexio.Identity.Domain.Exceptions;

public sealed class UserAlreadyBannedException : DomainException
{
    public UserAlreadyBannedException(UserId userId)
        : base($"User {userId} is already banned.") { }
}
