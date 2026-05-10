using Lexio.BuildingBlocks.Abstractions.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Lexio.BuildingBlocks.Auth;

/// <summary>Service registration extension for Auth building block.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers JWT Bearer authentication (RS256) and common authorization policies.
    /// Reads from Jwt:Authority, Jwt:Audience configuration sections.
    /// JWT signing key validation uses the OIDC discovery endpoint at Jwt:Authority.
    /// </summary>
    public static IServiceCollection AddLexioAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = configuration["Jwt:Authority"];
                options.Audience = configuration["Jwt:Audience"];
                options.RequireHttpsMetadata = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateAudience = true,
                    ValidateIssuer = true,
                    RequireSignedTokens = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthorizationPolicies.RequireUser, policy =>
                policy.RequireAuthenticatedUser())
            .AddPolicy(AuthorizationPolicies.RequireAdmin, policy =>
                policy.RequireRole("admin"));

        return services;
    }
}
