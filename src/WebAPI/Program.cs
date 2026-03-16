using CleanArchitecture.Application;
using CleanArchitecture.Infrastructure;
using CleanArchitecture.Infrastructure.Persistence;
using CleanArchitecture.Modules.AI;
using CleanArchitecture.WebAPI.Extensions;
using CleanArchitecture.WebAPI.Middlewares;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting CleanArchitecture API...");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) =>
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .WriteTo.Console()
            .WriteTo.File(
                "logs/log-.txt",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7));

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddAIModule(builder.Configuration);
    builder.Services.AddObservability(builder.Configuration);
    builder.Services.AddAppHealthChecks(builder.Configuration);
    builder.Services.AddCorsPolicy(builder.Configuration);
    builder.Services.AddApiRateLimiting();

    builder.Services.ConfigureHttpClientDefaults(http =>
        http.AddStandardResilienceHandler());

    builder.Services.AddControllers();
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });
    builder.Services.AddOpenApiWithJwtSecurity();
    builder.Services.AddProblemDetails();

    // Authentication is configured inside AddInfrastructure (JWT Bearer + Identity)
    builder.Services.AddAuthorization();

    var app = builder.Build();

    app.UseMiddleware<ExceptionHandlingMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    if (!app.Environment.IsDevelopment())
        app.UseHttpsRedirection();

    app.UseCors(app.Environment.IsDevelopment()
        ? CorsExtensions.AllowAllPolicy
        : CorsExtensions.AllowSpecificPolicy);

    app.UseRateLimiter();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers().RequireRateLimiting(RateLimitingExtensions.FixedPolicy);
    app.MapAppHealthChecks();
    app.UsePrometheusMetrics();

    // Apply pending EF Core migrations on startup (development/staging).
    // For production, prefer running migrations as a separate deployment step.
    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        // Uncomment to seed sample data on first run:
        // await ApplicationDbContextSeeder.SeedAsync(db);
    }

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}

// Needed for integration tests
public partial class Program;
