using Lexio.SharedKernel.Time;

namespace Lexio.TestUtils;

/// <summary>
/// Mutable IClock implementation for unit tests.
/// Set <see cref="UtcNow"/> directly to control time in tests.
/// </summary>
public sealed class TestClock : IClock
{
    /// <summary>
    /// Creates a TestClock with the given fixed time.
    /// Defaults to 2024-01-01 00:00:00 UTC if not specified.
    /// </summary>
    public TestClock(DateTimeOffset? now = null)
    {
        UtcNow = now ?? new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
    }

    /// <inheritdoc />
    public DateTimeOffset UtcNow { get; set; }

    /// <summary>Advances the clock by the given duration.</summary>
    public void Advance(TimeSpan duration) => UtcNow = UtcNow.Add(duration);
}
