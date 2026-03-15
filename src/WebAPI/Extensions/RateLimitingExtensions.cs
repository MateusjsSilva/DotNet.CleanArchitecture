using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json;
using System.Threading.RateLimiting;

namespace CleanArchitecture.WebAPI.Extensions;

internal static class RateLimitingExtensions
{
    internal const string FixedPolicy = "fixed";

    internal static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter(FixedPolicy, cfg =>
            {
                cfg.PermitLimit = 100;
                cfg.Window = TimeSpan.FromMinutes(1);
                cfg.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                cfg.QueueLimit = 10;
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
