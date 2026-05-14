using Mediator;
using Microsoft.Extensions.Logging;

namespace Lexio.Identity.Application.Common.Behaviors;

/// <summary>Logs request name + duration at the application boundary. No payload (PII safety).</summary>
public sealed partial class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMessage
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger) => _logger = logger;

    public async ValueTask<TResponse> Handle(
        TRequest message,
        CancellationToken cancellationToken,
        MessageHandlerDelegate<TRequest, TResponse> next)
    {
        var name = typeof(TRequest).Name;
        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        try
        {
            return await next(message, cancellationToken);
        }
        finally
        {
            var elapsedMs = System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            LogHandled(_logger, name, elapsedMs);
        }
    }

    [LoggerMessage(EventId = 1000, Level = LogLevel.Information, Message = "{Request} handled in {ElapsedMs:F1} ms")]
    private static partial void LogHandled(ILogger logger, string request, double elapsedMs);
}
