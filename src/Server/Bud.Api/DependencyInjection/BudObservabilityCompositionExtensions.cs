using Bud.Api.Observability;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Bud.Api.DependencyInjection;

public static class BudObservabilityCompositionExtensions
{
    public static IServiceCollection AddBudObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        services.AddBudStructuredLogging(configuration, environment);
        services.AddBudOpenTelemetry();
        return services;
    }

    private static IServiceCollection AddBudOpenTelemetry(this IServiceCollection services)
    {
        // All export/resource configuration is external, read from standard OTel env vars:
        //   OTEL_SERVICE_NAME, OTEL_RESOURCE_ATTRIBUTES, OTEL_EXPORTER_OTLP_ENDPOINT, etc.
        // The code only registers what to instrument.
        services.AddOpenTelemetry()
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(options =>
                {
                    // Exclude health check endpoints from traces to reduce noise
                    options.Filter = ctx =>
                        !ctx.Request.Path.StartsWithSegments("/health");
                })
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation())
            .UseOtlpExporter();

        return services;
    }

    private static IServiceCollection AddBudStructuredLogging(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // In Development and Testing environments, use the default console formatter.
        // In all other environments (Production, Staging, etc.), use the Cloud Logging JSON formatter.
        if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
        {
            return services;
        }

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddConsoleFormatter<CloudLoggingJsonFormatter, CloudLoggingJsonFormatterOptions>(options =>
            {
                options.GcpProjectId = configuration["GCP_PROJECT_ID"];
                options.IncludeScopes = true;
            });
            builder.AddConsole(options =>
            {
                options.FormatterName = CloudLoggingJsonFormatter.FormatterName;
            });
        });

        return services;
    }
}
