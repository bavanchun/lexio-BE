using Lexio.Identity.Domain.Primitives;
using Lexio.SharedKernel.Events;

namespace Lexio.Identity.Domain.Events;

/// <summary>Raised on successful authentication. <c>IpAddress</c> may be null when source isn't an HTTP request.</summary>
public sealed record UserLoggedInDomainEvent(
    UserId UserId,
    string? IpAddress,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
}
