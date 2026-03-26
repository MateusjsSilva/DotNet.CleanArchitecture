using CleanArchitecture.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Respawn;
using Testcontainers.MsSql;

namespace CleanArchitecture.IntegrationTests;

/// <summary>
/// Shared fixture for all integration test collections.
///
/// Strategy:
///   • Docker available  → Testcontainers SQL Server + Respawn (real SQL, full migration coverage)
///   • Docker unavailable → EF Core InMemory (fast, zero-dependency, no Docker required)
///
/// Both modes are valid for smoke/integration tests.
/// Testcontainers is preferred in CI (GitHub Actions ubuntu runners include Docker).
/// </summary>
public sealed class WebApplicationFactoryFixture
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    private MsSqlContainer? _container;
    private Respawner? _respawner;
    private bool _useContainer;

    // ── IAsyncLifetime ────────────────────────────────────────────────────────

    public async Task InitializeAsync()
    {
        try
        {
            _container = new MsSqlBuilder()
                .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                .Build();

            await _container.StartAsync();
            _useContainer = true;

            // Apply EF Core migrations once against the fresh container
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.MigrateAsync();

            // Configure Respawn to delete all rows between test classes
            await using var connection = new SqlConnection(_container.GetConnectionString());
            await connection.OpenAsync();
            _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
            {
                DbAdapter = DbAdapter.SqlServer,
                TablesToIgnore = ["__EFMigrationsHistory"]
            });
        }
        catch
        {
            // Docker unavailable — fall back to EF Core InMemory
            _useContainer = false;
            if (_container is not null)
            {
                await _container.DisposeAsync();
                _container = null;
            }

            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.EnsureCreatedAsync();
        }
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Creates an HTTP client with authenticated test user.</summary>
    public HttpClient CreateAuthenticatedClient() => CreateClient();

    /// <summary>Creates an anonymous HTTP client (no authentication).</summary>
    public HttpClient CreateAnonymousClient()
    {
        return WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove authentication entirely for anonymous client
                services.RemoveAll<IAuthenticationService>();
                services.RemoveAll<IAuthenticationSchemeProvider>();
            });
        }).CreateClient();
    }

    /// <summary>Resets all test data so each test class starts with a clean database.</summary>
    public async Task ResetDatabaseAsync()
    {
        if (_useContainer && _container is not null && _respawner is not null)
        {
            await using var connection = new SqlConnection(_container.GetConnectionString());
            await connection.OpenAsync();
            await _respawner.ResetAsync(connection);
        }
        else
        {
            // InMemory: recreate the schema to clear all data
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();
        }
    }

    // ── WebApplicationFactory ─────────────────────────────────────────────────

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        // Configure test authentication globally
        builder.ConfigureServices(services =>
        {
            // Replace JWT authentication with test authentication
            services.RemoveAll<IAuthenticationService>();
            services.RemoveAll<IAuthenticationSchemeProvider>();

            services.AddAuthentication(TestAuthHandler.TestScheme)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.TestScheme, _ => { });

            services.AddAuthorization();
        });

        if (_useContainer && _container is not null)
        {
            // Point the app at the Testcontainers SQL Server instance
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = _container.GetConnectionString()
                }));
        }
        // else: no connection string → AddInfrastructure falls back to InMemory automatically
    }
}
