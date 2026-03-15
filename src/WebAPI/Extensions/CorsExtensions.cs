namespace CleanArchitecture.WebAPI.Extensions;

internal static class CorsExtensions
{
    internal const string AllowAllPolicy = "AllowAll";
    internal const string AllowSpecificPolicy = "AllowSpecific";

    internal static IServiceCollection AddCorsPolicy(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var allowedOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            // Development: allow any origin (no credentials)
            options.AddPolicy(AllowAllPolicy, policy =>
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader());

            // Production: restrict to configured origins with credentials
            options.AddPolicy(AllowSpecificPolicy, policy =>
            {
                if (allowedOrigins.Length > 0)
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                else
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
            });
        });

        return services;
    }
}
