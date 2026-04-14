using CleanArchitecture.Application.Interfaces;
using CleanArchitecture.Domain.Interfaces;
using CleanArchitecture.Infrastructure.Identity;
using CleanArchitecture.Infrastructure.Persistence;
using CleanArchitecture.Infrastructure.Persistence.Outbox;
using CleanArchitecture.Infrastructure.Persistence.Repositories;
using CleanArchitecture.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace CleanArchitecture.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase("CleanArchitectureDb"));
        }
        else
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString,
                    b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));
        }

        services.AddScoped<IApplicationDbContext>(sp =>
            sp.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductQueries, ProductQueries>();
        services.AddScoped<ISqlConnectionFactory, SqlConnectionFactory>();

        // Identity
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = false;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // JWT
        var jwtSection = configuration.GetSection(JwtSettings.SectionName);
        services.Configure<JwtSettings>(jwtSection);
        var jwtSettings = jwtSection.Get<JwtSettings>()!;

        ValidateJwtSecret(jwtSettings, configuration);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddScoped<IAuthService, AuthService>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.Configure<OutboxProcessorSettings>(
            configuration.GetSection(OutboxProcessorSettings.SectionName));
        services.AddHostedService<OutboxProcessorService>();

        // Distributed cache: Redis when configured, in-memory otherwise.
        // To use Redis: set ConnectionStrings__Redis in appsettings / environment variables.
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddStackExchangeRedisCache(options =>
                options.Configuration = redisConnectionString);
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        return services;
    }

    /// <summary>
    /// Prevents accidental production deploys with the default placeholder JWT secret.
    /// Throws in non-Development environments; logs a warning in Development so the
    /// developer is reminded to change it before going live.
    /// </summary>
    private static void ValidateJwtSecret(JwtSettings jwtSettings, IConfiguration configuration)
    {
        const string placeholder = "CHANGE_THIS_TO_A_STRONG_SECRET_KEY_IN_PRODUCTION_MIN_32_CHARS";
        const int minimumLength = 32;

        // UseEnvironment("Test") in WebApplicationFactory sets configuration["environment"] (not ASPNETCORE_ENVIRONMENT),
        // so we check multiple sources to correctly identify non-production environments.
        var environment = configuration["ASPNETCORE_ENVIRONMENT"]
            ?? configuration["environment"]
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? "Production";

        var isDevelopment = environment.Equals("Development", StringComparison.OrdinalIgnoreCase)
                         || environment.Equals("Test", StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(jwtSettings.Secret))
        {
            throw new InvalidOperationException(
                "JwtSettings:Secret is not configured. " +
                "Set a strong, unique secret via environment variable or user secrets.");
        }

        if (jwtSettings.Secret.Equals(placeholder, StringComparison.Ordinal))
        {
            if (!isDevelopment)
                throw new InvalidOperationException(
                    "JwtSettings:Secret is still the default placeholder value. " +
                    "Replace it with a cryptographically strong secret before deploying to production. " +
                    "Use: dotnet user-secrets set \"JwtSettings:Secret\" \"<your-secret>\" " +
                    "or set the JWTSETTINGS__SECRET environment variable.");

            // In Development we allow the placeholder but emit a visible warning
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(
                "[SECURITY WARNING] JwtSettings:Secret is still the default placeholder. " +
                "Change it before deploying to any shared or production environment.");
            Console.ResetColor();
        }

        if (jwtSettings.Secret.Length < minimumLength)
        {
            throw new InvalidOperationException(
                $"JwtSettings:Secret must be at least {minimumLength} characters long. " +
                $"Current length: {jwtSettings.Secret.Length}.");
        }
    }
}
