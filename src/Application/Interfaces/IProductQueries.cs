using CleanArchitecture.Application.DTOs;

namespace CleanArchitecture.Application.Interfaces;

/// <summary>
/// Read-side query contract for complex product projections (implemented with Dapper in Infrastructure).
/// </summary>
public interface IProductQueries
{
    Task<ProductsSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
}
