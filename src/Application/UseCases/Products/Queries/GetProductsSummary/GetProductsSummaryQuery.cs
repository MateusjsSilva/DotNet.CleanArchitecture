using CleanArchitecture.Application.Common;
using CleanArchitecture.Application.Common.Mediator;
using CleanArchitecture.Application.DTOs;

namespace CleanArchitecture.Application.UseCases.Products.Queries.GetProductsSummary;

/// <summary>
/// Example of a Dapper read-side query for complex reporting / projections.
/// Uses sliding expiration since the summary changes whenever products are added/updated.
/// </summary>
public sealed record GetProductsSummaryQuery : IQuery<ProductsSummaryDto>, ICacheableQuery
{
    public string CacheKey => $"{CacheKeys.ProductCollections}:summary";

    public string? CacheKeyPrefix => CacheKeys.ProductCollections;

    /// <summary>
    /// Uses sliding expiration (3 minutes).
    /// If the summary is accessed frequently, cache stays alive.
    /// Invalidated when products are mutated.
    /// </summary>
    public TimeSpan? AbsoluteExpiration => null;

    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(3);
}
