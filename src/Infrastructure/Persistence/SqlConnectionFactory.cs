using CleanArchitecture.Application.Interfaces;
using Npgsql;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace CleanArchitecture.Infrastructure.Persistence;

internal sealed class SqlConnectionFactory(IConfiguration configuration) : ISqlConnectionFactory
{
    public IDbConnection CreateConnection()
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        return new NpgsqlConnection(connectionString);
    }
}
