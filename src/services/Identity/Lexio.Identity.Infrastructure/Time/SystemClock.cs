using Lexio.SharedKernel.Time;

namespace Lexio.Identity.Infrastructure.Time;

/// <summary>Production <see cref="IClock"/>. Always returns wall-clock UTC.</summary>
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
