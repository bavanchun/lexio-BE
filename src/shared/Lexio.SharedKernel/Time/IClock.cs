namespace Lexio.SharedKernel.Time;

/// <summary>
/// Abstracts wall-clock access so domain and application code remains testable
/// without system clock dependency. Concrete impl lives in BuildingBlocks.Observability (phase 05).
/// </summary>
public interface IClock
{
    /// <summary>Current UTC time. Always use this; never DateTime.UtcNow in domain/application code.</summary>
    DateTimeOffset UtcNow { get; }
}
