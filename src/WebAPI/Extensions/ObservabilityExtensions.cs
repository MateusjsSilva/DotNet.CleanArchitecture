using CleanArchitecture.Application.Telemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CleanArchitecture.WebAPI.Extensions;

internal static class ObservabilityExtensions
{
    internal static IServiceCollection AddObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var serviceName = configuration["Observability:ServiceName"] ?? "CleanArchitecture.API";
        var otlpEndpoint = configuration["Observability:Otlp:Endpoint"];

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName)
                .AddTelemetrySdk()
                .AddEnvironmentVariableDetector())
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(opts => opts.RecordException = true)
                    .AddHttpClientInstrumentation()
                    .AddSource(ApplicationActivitySource.Name);

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                    tracing.AddOtlpExporter(opts => opts.Endpoint = new Uri(otlpEndpoint));
                else
                    tracing.AddConsoleExporter();
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                    metrics.AddOtlpExporter(opts => opts.Endpoint = new Uri(otlpEndpoint));

                // Always expose /metrics for Prometheus scraping
                metrics.AddPrometheusExporter();
            });

        return services;
    }

    internal static IApplicationBuilder UsePrometheusMetrics(this IApplicationBuilder app)
    {
        app.UseOpenTelemetryPrometheusScrapingEndpoint();
        return app;
    }
}
