using Lexio.Identity.Infrastructure.Persistence;
using Lexio.Identity.Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Lexio.Identity.Infrastructure.OpenIddict;

/// <summary>
/// Wires OpenIddict 6 core/server/validation against <see cref="IdentityDbContext"/>.
/// The server is configured for OAuth interop at <c>/connect/token</c>; internal
/// command handlers bypass it and call <c>ITokenIssuer</c> directly.
/// </summary>
public static class OpenIddictRegistrationExtensions
{
    public static IServiceCollection AddIdentityOpenIddict(
        this IServiceCollection services,
        IHostEnvironment env)
    {
        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                    .UseDbContext<IdentityDbContext>();
            })
            .AddServer(options =>
            {
                options.SetTokenEndpointUris("/connect/token");
                options.AllowPasswordFlow();
                options.AllowRefreshTokenFlow();

                if (env.IsDevelopment())
                {
                    options.AddDevelopmentEncryptionCertificate();
                    options.AddDevelopmentSigningCertificate();
                }
                else
                {
                    var loader = new SigningCertificateLoader(env);
                    options.AddSigningCertificate(loader.Certificate);
                    options.AddEncryptionCertificate(loader.Certificate);
                }

                var aspNet = options.UseAspNetCore()
                    .EnableTokenEndpointPassthrough();

                if (env.IsDevelopment())
                {
                    aspNet.DisableTransportSecurityRequirement();
                }
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        return services;
    }
}
