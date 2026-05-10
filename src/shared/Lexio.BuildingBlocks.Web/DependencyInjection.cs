using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Lexio.BuildingBlocks.Web;

/// <summary>Service registration and pipeline extension for Web building block.</summary>
public static class DependencyInjection
{
    /// <summary>Register Web building-block services (problem-details, correlation ID).</summary>
    public static IServiceCollection AddLexioWeb(this IServiceCollection services)
    {
        services.AddProblemDetails();
        return services;
    }

    /// <summary>Wire up Web building-block middleware in the request pipeline.</summary>
    public static IApplicationBuilder UseLexioWeb(this IApplicationBuilder app)
    {
        app.UseMiddleware<LexioExceptionHandlingMiddleware>();
        app.UseMiddleware<CorrelationIdMiddleware>();
        return app;
    }
}
