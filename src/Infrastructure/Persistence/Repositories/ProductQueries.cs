using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Application.Interfaces;
using Dapper;

namespace CleanArchitecture.Infrastructure.Persistence.Repositories;

internal sealed class ProductQueries(ISqlConnectionFactory connectionFactory) : IProductQueries
{
    public async Task<ProductsSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();

        const string sql = """
            SELECT
                COUNT(*)                                           AS TotalProducts,
                SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END)    AS ActiveProducts,
                ISNULL(AVG(Price), 0)                             AS AveragePrice,
                ISNULL(SUM(Price), 0)                             AS TotalValue
            FROM Products
            """;

        return await connection.QuerySingleAsync<ProductsSummaryDto>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));
    }
}
