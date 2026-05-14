using Lexio.BuildingBlocks.Abstractions.Persistence;
using Lexio.BuildingBlocks.Caching;
using Lexio.BuildingBlocks.Messaging;
using Lexio.Identity.Application.Contracts.Persistence;
using Lexio.Identity.Application.Contracts.Security;
using Lexio.Identity.Infrastructure.OpenIddict;
using Lexio.Identity.Infrastructure.Persistence;
using Lexio.Identity.Infrastructure.Persistence.Repositories;
using Lexio.Identity.Infrastructure.Security;
using Lexio.Identity.Infrastructure.Time;
using Lexio.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Lexio.Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var conn = configuration.GetConnectionString("DefaultConnection")
                   ?? configuration["Database:ConnectionString"]
                   ?? throw new InvalidOperationException("Identity database connection string is missing.");

        services.AddDbContext<IdentityDbContext>(opts =>
        {
            opts.UseNpgsql(conn, npg =>
                    npg.SetPostgresVersion(18, 0)
                       .MigrationsHistoryTable("__ef_migrations_history", "public"))
                .UseSnakeCaseNamingConvention();
            opts.UseOpenIddict();
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<IdentityDbContext>());
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();

        services.AddSingleton<IClock, SystemClock>();

        services.AddOptions<OpenIddictTokenIssuerOptions>()
            .Bind(configuration.GetSection(OpenIddictTokenIssuerOptions.SectionName));
        services.AddSingleton<SigningCertificateLoader>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<ITokenIssuer, OpenIddictTokenIssuer>();

        services.AddIdentityOpenIddict(environment);

        services.AddLexioCaching(configuration);
        services.AddLexioMessaging(configuration);

        return services;
    }
}
