using Lexio.BuildingBlocks.Auth;
using Lexio.Identity.Api.Authorization;
using Lexio.Identity.Application.Contracts.Security;
using Microsoft.AspNetCore.Authorization;

namespace Lexio.Identity.Api.Configuration;

/// <summary>
/// Adds the <c>NotBanned</c> policy + DB-aware variant, the memory cache backing
/// <see cref="BanStatusCache"/>, and the admin policy. Bearer auth itself is wired
/// in <c>AddLexioAuth</c>.
/// </summary>
public static class AuthorizationExtensions
{
    public const string NotBannedPolicy = "NotBanned";
    public const string NotBannedWithDbCheckPolicy = "NotBanned.WriteAware";
    public const string AdminPolicy = AuthorizationPolicies.RequireAdmin;

    public static IServiceCollection AddLexioAuthorization(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddScoped<BanStatusCache>();
        services.AddScoped<IBanStatusCache>(sp => sp.GetRequiredService<BanStatusCache>());
        services.AddScoped<IAuthorizationHandler, BannedUserAuthorizationHandler>();

        services.AddAuthorizationBuilder()
            .AddPolicy(NotBannedPolicy, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(BannedUserRequirement.ClaimOnly);
            })
            .AddPolicy(NotBannedWithDbCheckPolicy, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(BannedUserRequirement.WithDbCheck);
            });

        return services;
    }
}
