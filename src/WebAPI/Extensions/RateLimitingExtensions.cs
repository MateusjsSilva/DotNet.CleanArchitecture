using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json;
using System.Threading.RateLimiting;

namespace CleanArchitecture.WebAPI.Extensions;

/// <summary>
/// Granular rate limiting policies for different API endpoints.
/// Protects against abuse and ensures fair resource distribution based on endpoint sensitivity.
///
/// Usage: Apply policies via [RequireRateLimiting] attribute on controller/action:
///     [RequireRateLimiting(RateLimitingExtensions.AuthPolicy)]
///     public IActionResult Login() { ... }
/// </summary>
internal static class RateLimitingExtensions
{
    // Policy names - use in [RequireRateLimiting] attributes on controllers/actions
    internal const string AuthPolicy = "auth";        // Strict: 5 req/min (prevents brute force on login/register)
    internal const string ProductsPolicy = "products"; // Standard: 100 req/min (CRUD operations)
    internal const string AIPolicy = "ai";            // Relaxed: 30 req/min (resource-intensive operations)
    internal const string DefaultPolicy = "default";  // Fallback: 50 req/min (other endpoints)


    internal static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // Auth endpoints: strict rate limiting to prevent brute force attacks
            options.AddFixedWindowLimiter(AuthPolicy, cfg =>
            {
                cfg.PermitLimit = 5;
                cfg.Window = TimeSpan.FromMinutes(1);
                cfg.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                cfg.QueueLimit = 0;
            });

            // Products endpoints: standard rate limiting for normal CRUD operations
            options.AddFixedWindowLimiter(ProductsPolicy, cfg =>
            {
                cfg.PermitLimit = 100;
                cfg.Window = TimeSpan.FromMinutes(1);
                cfg.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                cfg.QueueLimit = 0;
            });

            // AI endpoints: relaxed rate limiting (resource-intensive operations)
            options.AddFixedWindowLimiter(AIPolicy, cfg =>
            {
                cfg.PermitLimit = 30;
                cfg.Window = TimeSpan.FromMinutes(1);
                cfg.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                cfg.QueueLimit = 0;
            });

            // Default fallback policy for other endpoints
            options.AddFixedWindowLimiter(DefaultPolicy, cfg =>
            {
                cfg.PermitLimit = 50;
                cfg.Window = TimeSpan.FromMinutes(1);
                cfg.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                cfg.QueueLimit = 0;
            });

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.ContentType = "application/problem+json";

                await context.HttpContext.Response.WriteAsJsonAsync(
                    new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Too Many Requests",
                        Detail = "Rate limit exceeded. Please try again later.",
                        Instance = context.HttpContext.Request.Path
                    },
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase },
                    token);
            };
        });

        return services;
    }
}
