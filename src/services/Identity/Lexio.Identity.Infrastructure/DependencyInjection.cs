using Lexio.BuildingBlocks.Caching;
using Lexio.BuildingBlocks.Messaging;
using Lexio.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lexio.Identity.Infrastructure;

/// <summary>Registers infrastructure layer services: EF Core, caching, messaging, auth.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<IdentityDbContext>(opts =>
            opts.UseNpgsql(configuration["Database:ConnectionString"]));

        services.AddLexioCaching(configuration);
        services.AddLexioMessaging(configuration);

        return services;
    }
}
