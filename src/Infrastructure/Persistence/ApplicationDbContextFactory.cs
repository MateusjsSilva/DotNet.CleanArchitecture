using CleanArchitecture.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace CleanArchitecture.Infrastructure.Persistence;

/// <summary>
/// Used by EF Core tools (dotnet ef migrations add / dotnet ef database update).
/// Run from the solution root: dotnet ef migrations add MyMigration --project src/Infrastructure --startup-project src/WebAPI
/// </summary>
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=CleanArchitectureDb_Dev;Username=postgres;Password=postgres;GSS Encryption Mode=Disable";

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options, new NoOpCurrentUserService());
    }

    private sealed class NoOpCurrentUserService : ICurrentUserService
    {
        public string? UserId => null;
        public string? UserName => null;
        public bool IsAuthenticated => false;
    }
}
