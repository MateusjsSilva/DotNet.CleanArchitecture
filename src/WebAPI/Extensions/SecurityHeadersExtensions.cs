using Microsoft.AspNetCore.HttpOverrides;

namespace CleanArchitecture.WebAPI.Extensions;

public static class SecurityHeadersExtensions
{
    public static IServiceCollection AddSecurityHeaders(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        return services;
    }

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        // HSTS (HTTP Strict Transport Security)
        app.UseHsts();

        // Security Headers Middleware
        app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;

            // Content Security Policy
            headers.Append("Content-Security-Policy",
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net; " +
                "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
                "img-src 'self' data: https:; " +
                "font-src 'self' https://cdn.jsdelivr.net; " +
                "connect-src 'self'; " +
                "frame-ancestors 'none'");

            // X-Frame-Options (prevent clickjacking)
            headers.Append("X-Frame-Options", "DENY");

            // X-Content-Type-Options (prevent MIME sniffing)
            headers.Append("X-Content-Type-Options", "nosniff");

            // Referrer Policy
            headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

            // X-XSS-Protection (legacy browser protection)
            headers.Append("X-XSS-Protection", "1; mode=block");

            // Permissions Policy (feature policy)
            headers.Append("Permissions-Policy",
                "camera=(), " +
                "microphone=(), " +
                "geolocation=(), " +
                "payment=(), " +
                "usb=(), " +
                "magnetometer=(), " +
                "accelerometer=(), " +
                "gyroscope=()");

            // Remove server information
            headers.Remove("Server");
            headers.Remove("X-Powered-By");
            headers.Remove("X-AspNet-Version");
            headers.Remove("X-AspNetMvc-Version");

            await next();
        });

        return app;
    }
}