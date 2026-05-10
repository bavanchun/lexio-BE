using System.Diagnostics;

namespace Lexio.BuildingBlocks.Observability;

/// <summary>
/// Static factory for named ActivitySource instances.
/// Services call LexioActivitySource.For("Lexio.Identity") to get their source.
/// </summary>
public static class LexioActivitySource
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, ActivitySource> Sources = new();

    /// <summary>
    /// Get or create an ActivitySource for the given service name.
    /// Use the service name as registered in AddLexioObservability.
    /// </summary>
    public static ActivitySource For(string serviceName) =>
        Sources.GetOrAdd(serviceName, name => new ActivitySource(name));
}
