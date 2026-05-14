namespace Lexio.Identity.Application.Contracts.Auditing;

public sealed record AuditEvent(
    string EventType,
    Guid? UserId,
    string? IpAddress,
    IReadOnlyDictionary<string, object?> Payload);

public interface IAuditLogger
{
    Task LogAsync(AuditEvent @event, CancellationToken ct = default);
}
