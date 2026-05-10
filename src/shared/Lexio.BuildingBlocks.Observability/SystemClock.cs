using Lexio.SharedKernel.Time;

namespace Lexio.BuildingBlocks.Observability;

/// <summary>
/// Production IClock implementation backed by DateTimeOffset.UtcNow.
/// Registered as singleton in AddLexioObservability.
/// </summary>
internal sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
