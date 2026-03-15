using CleanArchitecture.Application;
using CleanArchitecture.Infrastructure;
using CleanArchitecture.Modules.AI;
using CleanArchitecture.WebAPI.Extensions;
using CleanArchitecture.WebAPI.Middlewares;
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
    builder.Services.AddOpenApi();
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

    app.UseHttpsRedirection();

    app.UseCors(app.Environment.IsDevelopment()
        ? CorsExtensions.AllowAllPolicy
        : CorsExtensions.AllowSpecificPolicy);

    app.UseRateLimiter();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers().RequireRateLimiting(RateLimitingExtensions.FixedPolicy);
    app.MapAppHealthChecks();

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
