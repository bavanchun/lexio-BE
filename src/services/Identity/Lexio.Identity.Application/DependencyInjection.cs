using FluentValidation;
using Lexio.Identity.Application.Common.Behaviors;
using Mapster;
using MapsterMapper;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace Lexio.Identity.Application;

/// <summary>
/// Registers application-layer services: Mediator + pipeline behaviours,
/// FluentValidation validators, and Mapster mappings.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddIdentityApplication(this IServiceCollection services)
    {
        services.AddMediator(opts =>
        {
            opts.ServiceLifetime = ServiceLifetime.Scoped;
        });

        services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(typeof(DependencyInjection).Assembly);
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();

        return services;
    }
}
