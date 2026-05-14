using Lexio.Identity.Domain.Primitives;
using Lexio.SharedKernel.Events;

namespace Lexio.Identity.Domain.Events;

/// <summary>Raised when a new <c>User</c> is registered (email/password or external OAuth).</summary>
public sealed record UserRegisteredDomainEvent(
    UserId UserId,
    string Email,
    string DisplayName,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
}
