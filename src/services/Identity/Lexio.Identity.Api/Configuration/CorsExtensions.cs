namespace Lexio.Identity.Api.Configuration;

/// <summary>
/// CORS for the Lexio FE. Origins come from <c>CORS:AllowedOrigins</c> (comma-separated)
/// or the <c>CORS_ALLOWED_ORIGINS</c> env var. Credentials are allowed because the FE
/// proxies refresh tokens via httpOnly cookie (phase-10).
/// </summary>
public static class CorsExtensions
{
    public const string PolicyName = "lexio-fe";

    public static IServiceCollection AddLexioCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = (configuration["CORS:AllowedOrigins"] ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        services.AddCors(opts =>
        {
            opts.AddPolicy(PolicyName, policy =>
            {
                if (origins.Length > 0)
                {
                    policy.WithOrigins(origins);
                }
                policy.AllowCredentials()
                      .AllowAnyHeader()
                      .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS");
            });
        });

        return services;
    }
}
