using Lexio.SharedKernel.Time;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Sinks.OpenTelemetry;

namespace Lexio.BuildingBlocks.Observability;

/// <summary>Service registration extension for Observability building block.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Configures Serilog (console + OTLP sink) and OpenTelemetry traces/metrics/logs.
    /// OTEL exporter is conditional on OTEL_EXPORTER_OTLP_ENDPOINT environment variable
    /// to avoid startup failure when Grafana/Jaeger is not running in dev.
    /// </summary>
    public static IServiceCollection AddLexioObservability(
        this IServiceCollection services,
        string serviceName)
    {
        // ── IClock ────────────────────────────────────────────────────────────
        services.AddSingleton<IClock, SystemClock>();

        // ── Serilog ───────────────────────────────────────────────────────────
        var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");

        var logConfig = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("ServiceName", serviceName)
            .WriteTo.Console();

        if (!string.IsNullOrEmpty(otlpEndpoint))
        {
            logConfig.WriteTo.OpenTelemetry(opts =>
            {
                opts.Endpoint = otlpEndpoint;
                opts.Protocol = OtlpProtocol.Grpc;
                opts.ResourceAttributes = new Dictionary<string, object>
                {
                    ["service.name"] = serviceName,
                };
            });
        }

        Log.Logger = logConfig.CreateLogger();
        services.AddSerilog();

        // ── OpenTelemetry ─────────────────────────────────────────────────────
        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSource(serviceName);

                if (!string.IsNullOrEmpty(otlpEndpoint))
                {
                    tracing.AddOtlpExporter();
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddRuntimeInstrumentation();

                if (!string.IsNullOrEmpty(otlpEndpoint))
                {
                    metrics.AddOtlpExporter();
                }
            });

        return services;
    }
}
