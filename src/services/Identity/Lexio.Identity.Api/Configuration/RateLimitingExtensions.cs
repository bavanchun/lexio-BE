using System.Threading.RateLimiting;

namespace Lexio.Identity.Api.Configuration;

/// <summary>
/// Three named rate-limit policies:
/// <list type="bullet">
///   <item><c>login</c>: 5/min, partitioned by IP (anti brute-force).</item>
///   <item><c>default</c>: 100/min, partitioned by IP.</item>
///   <item><c>authenticated</c>: 1000/min, partitioned by <c>sub</c> claim.</item>
/// </list>
/// All policies reject with 429 + <c>Retry-After</c> header.
/// </summary>
public static class RateLimitingExtensions
{
    public const string Login = "login";
    public const string Default = "default";
    public const string Authenticated = "authenticated";

    public static IServiceCollection AddLexioRateLimits(this IServiceCollection services)
    {
        services.AddRateLimiter(opts =>
        {
            opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            opts.OnRejected = static (ctx, _) =>
            {
                ctx.HttpContext.Response.Headers["Retry-After"] = "60";
                return ValueTask.CompletedTask;
            };

            opts.AddPolicy(Login, ctx => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: ClientIp(ctx),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                }));

            opts.AddPolicy(Default, ctx => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: ClientIp(ctx),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                }));

            opts.AddPolicy(Authenticated, ctx => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: ctx.User.FindFirst("sub")?.Value ?? ClientIp(ctx),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 1000,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                }));
        });

        return services;
    }

    private static string ClientIp(HttpContext ctx) =>
        ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
        ?? ctx.Connection.RemoteIpAddress?.ToString()
        ?? "unknown";
}
