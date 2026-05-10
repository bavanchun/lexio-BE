using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Lexio.BuildingBlocks.Web;

/// <summary>
/// Reads X-Correlation-Id from the incoming request or generates a new Guid.
/// Sets the value on the response and stores it in HttpContext.Items for downstream logging.
/// </summary>
internal sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    private const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");

        context.Items[HeaderName] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next(context).ConfigureAwait(false);
        }
    }
}
