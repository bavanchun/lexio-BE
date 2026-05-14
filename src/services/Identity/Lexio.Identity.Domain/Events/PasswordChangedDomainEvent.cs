using Lexio.Identity.Domain.Primitives;
using Lexio.SharedKernel.Events;

namespace Lexio.Identity.Domain.Events;

/// <summary>Raised when a user's password hash changes. Carries no plaintext or hash material.</summary>
public sealed record PasswordChangedDomainEvent(
    UserId UserId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
}
