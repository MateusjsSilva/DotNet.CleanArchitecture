using CleanArchitecture.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
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
    // Set after InitializeAsync completes; read lazily by the ConfigureWebHost lambda.
    private string? _connectionString;

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
            _connectionString = _container.GetConnectionString();

            // Apply EF Core migrations once against the fresh container
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.MigrateAsync();

            // Configure Respawn to delete all rows between test classes
            await using var connection = new SqlConnection(_connectionString);
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

    /// <summary>
    /// Creates an HTTP client. The test authentication scheme is always active,
    /// so the client is authenticated by default.
    ///
    /// For endpoints marked [AllowAnonymous], this client works without any special setup.
    /// For endpoints marked [Authorize], the TestAuthHandler auto-authenticates.
    /// </summary>
    public HttpClient CreateAuthenticatedClient() => CreateClient();

    /// <summary>Resets all test data so each test class starts with a clean database.</summary>
    public async Task ResetDatabaseAsync()
    {
        if (_useContainer && _connectionString is not null && _respawner is not null)
        {
            await using var connection = new SqlConnection(_connectionString);
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

        builder.ConfigureServices(services =>
        {
            if (_useContainer && _connectionString is not null)
            {
                // Point EF Core at the Testcontainers SQL Server instance.
                // Remove existing DbContext registration and re-register with the container connection string.
                // _connectionString is set during InitializeAsync before any client is created.
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlServer(_connectionString));
            }

            // Replace JWT Bearer with a test scheme that auto-authenticates every request.
            // This call overrides the default scheme set by AddInfrastructure (JWT Bearer).
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.TestScheme;
                options.DefaultChallengeScheme = TestAuthHandler.TestScheme;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.TestScheme, _ => { });
        });
    }
}
