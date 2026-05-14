using Lexio.SharedKernel.Time;

namespace Lexio.Identity.Infrastructure.Tests.Security;

internal sealed class MutableClock(DateTimeOffset start) : IClock
{
    public DateTimeOffset UtcNow { get; private set; } = start;

    public void Advance(TimeSpan delta) => UtcNow += delta;
}
