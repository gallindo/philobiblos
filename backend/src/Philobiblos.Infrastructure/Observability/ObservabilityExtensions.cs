using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Exporter;
using OpenTelemetry.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Philobiblos.Infrastructure.Data;

namespace Philobiblos.Infrastructure;

public static class ObservabilityExtensions
{
    public const string ServiceName = "Philobiblos.Api";
    private const string CorrelationIdTag = "correlation.id";

    public static IServiceCollection AddObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var serviceVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown";
        var deploymentEnvironment = environment.EnvironmentName.ToLowerInvariant();

        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(ServiceName, serviceVersion: serviceVersion)
            .AddAttributes(new[]
            {
                new KeyValuePair<string, object>("deployment.environment", deploymentEnvironment)
            });

        services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddSource(ServiceName)
                    .AddOtlpExporter(options =>
                    {
                        var endpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
                        if (!string.IsNullOrWhiteSpace(endpoint))
                        {
                            options.Endpoint = new Uri(endpoint);
                        }
                    });
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter(ServiceName)
                    .AddOtlpExporter(options =>
                    {
                        var endpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
                        if (!string.IsNullOrWhiteSpace(endpoint))
                        {
                            options.Endpoint = new Uri(endpoint);
                        }
                    })
                    .AddPrometheusExporter();
            });

        services.AddHealthChecks()
            .AddDbContextCheck<LibraryDbContext>("database");

        return services;
    }

    public static IApplicationBuilder UseObservability(this IApplicationBuilder app)
    {
        app.UseOpenTelemetryPrometheusScrapingEndpoint();

        app.Use(async (context, next) =>
        {
            Activity.Current?.SetTag(CorrelationIdTag, context.TraceIdentifier);
            await next(context);
        });

        return app;
    }
}
