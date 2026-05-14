using FluentValidation;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;

namespace Lexio.Identity.Application;

/// <summary>Registers application layer services: validators, Mapster mappings.</summary>
/// <remarks>
/// Mediator is registered via source generator — add [assembly: MediatorOptions(...)] or
/// call services.AddMediator() from the Mediator NuGet in the Api layer.
/// </remarks>
public static class DependencyInjection
{
    public static IServiceCollection AddIdentityApplication(this IServiceCollection services)
    {
        // Register all FluentValidation validators in this assembly
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // Mapster global config scan
        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(typeof(DependencyInjection).Assembly);
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();

        return services;
    }
}
