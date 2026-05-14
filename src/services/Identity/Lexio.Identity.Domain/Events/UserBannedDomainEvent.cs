using Lexio.Identity.Domain.Primitives;
using Lexio.SharedKernel.Events;

namespace Lexio.Identity.Domain.Events;

/// <summary>Raised when a user is banned by an admin. Drives ban-claim cache invalidation downstream.</summary>
public sealed record UserBannedDomainEvent(
    UserId UserId,
    UserId BannedByAdminId,
    string Reason,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
}
