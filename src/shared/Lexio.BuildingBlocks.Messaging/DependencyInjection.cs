using Confluent.Kafka;
using Lexio.BuildingBlocks.Abstractions.Messaging;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lexio.BuildingBlocks.Messaging;

/// <summary>Service registration extension for Messaging building block.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers MassTransit (RabbitMQ transport) + Kafka producer.
    /// Reads RabbitMQ:Host/Username/Password and Kafka:BootstrapServers from configuration.
    /// </summary>
    public static IServiceCollection AddLexioMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((_, cfg) =>
            {
                cfg.Host(
                    configuration["RabbitMQ:Host"] ?? "localhost",
                    h =>
                    {
                        h.Username(configuration["RabbitMQ:Username"] ?? "guest");
                        h.Password(configuration["RabbitMQ:Password"] ?? "guest");
                    });
            });
        });

        services.AddSingleton<IEventBus, MassTransitEventBus>();

        // Kafka producer factory — registered as singleton for connection reuse
        services.AddSingleton<IProducer<string, string>>(_ =>
        {
            var config = new ProducerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            };
            return new ProducerBuilder<string, string>(config).Build();
        });

        services.AddSingleton<KafkaEventPublisher>();

        return services;
    }
}
