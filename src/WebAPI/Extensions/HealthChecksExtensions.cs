using CleanArchitecture.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace CleanArchitecture.WebAPI.Extensions;

internal static class HealthChecksExtensions
{
    internal static IServiceCollection AddAppHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        var builder = services.AddHealthChecks();

        if (!string.IsNullOrWhiteSpace(connectionString))
            builder.AddDbContextCheck<ApplicationDbContext>("database");

        return services;
    }

    internal static WebApplication MapAppHealthChecks(this WebApplication app)
    {
        // Liveness: just checks the app is running
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false
        });

        // Readiness: checks all registered health checks (DB, etc.)
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            ResponseWriter = WriteJsonResponse
        });

        return app;
    }

    private static async Task WriteJsonResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            duration = report.TotalDuration,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration,
                description = e.Value.Description,
                error = e.Value.Exception?.Message
            })
        };

        await context.Response.WriteAsJsonAsync(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }
}
