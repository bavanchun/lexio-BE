using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lexio.BuildingBlocks.Web;

/// <summary>
/// Global exception handler middleware — catches unhandled exceptions and maps them to
/// RFC 7807 ProblemDetails responses. Stack traces are only included in Development.
/// </summary>
internal sealed class LexioExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<LexioExceptionHandlingMiddleware> logger,
    IHostEnvironment env)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteProblemDetailsAsync(context, ex).ConfigureAwait(false);
        }
    }

    private Task WriteProblemDetailsAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
            Type = "https://tools.ietf.org/html/rfc7807",
            Detail = env.IsDevelopment() ? ex.Message : null,
        };

        if (env.IsDevelopment())
        {
            problem.Extensions["stackTrace"] = ex.StackTrace;
        }

        return context.Response.WriteAsJsonAsync(problem);
    }
}
