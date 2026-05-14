using Lexio.Identity.Domain.Primitives;
using Lexio.SharedKernel.Events;

namespace Lexio.Identity.Domain.Events;

/// <summary>Raised on admin role change. Carries old/new so downstream caches can invalidate cleanly.</summary>
public sealed record RoleChangedDomainEvent(
    UserId UserId,
    RoleId OldRoleId,
    RoleId NewRoleId,
    UserId ChangedByAdminId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
}
