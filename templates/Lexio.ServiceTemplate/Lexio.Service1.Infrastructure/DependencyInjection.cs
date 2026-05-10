using Lexio.BuildingBlocks.Caching;
using Lexio.BuildingBlocks.Messaging;
using Lexio.BuildingBlocks.Persistence;
using Lexio.Service1.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lexio.Service1.Infrastructure;

/// <summary>Registers infrastructure layer services: EF Core, caching, messaging, auth.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddService1Infrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<Service1DbContext>(opts =>
            opts.UseNpgsql(configuration["Database:ConnectionString"]));

        services.AddLexioCaching(configuration);
        services.AddLexioMessaging(configuration);

        return services;
    }
}
