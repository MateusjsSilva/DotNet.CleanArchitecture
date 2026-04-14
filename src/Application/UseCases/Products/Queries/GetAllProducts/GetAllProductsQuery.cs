using CleanArchitecture.Application.Common;
using CleanArchitecture.Application.Common.Mediator;
using CleanArchitecture.Application.DTOs;

namespace CleanArchitecture.Application.UseCases.Products.Queries.GetAllProducts;

/// <param name="OrderBy">Field to sort by: createdAt (default), name, price</param>
/// <param name="Ascending">Sort direction. Default: false (newest first)</param>
/// <param name="NameContains">Case-insensitive substring filter on product name</param>
/// <param name="MinPrice">Minimum price (inclusive)</param>
/// <param name="MaxPrice">Maximum price (inclusive)</param>
public sealed record GetAllProductsQuery(
    bool OnlyActive = true,
    int Page = 1,
    int PageSize = 20,
    string OrderBy = "createdAt",
    bool Ascending = false,
    string? NameContains = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null)
    : IQuery<PagedResult<ProductDto>>, ICacheableQuery
{
    /// <summary>
    /// Generates a unique cache key based on query parameters.
    /// Changes to any filter automatically invalidate the cache.
    /// </summary>
    public string CacheKey =>
        $"{CacheKeys.ProductCollections}:page={Page}:pageSize={PageSize}:orderBy={OrderBy}:ascending={Ascending}" +
        $":onlyActive={OnlyActive}:name={NameContains}:minPrice={MinPrice}:maxPrice={MaxPrice}";

    public string? CacheKeyPrefix => CacheKeys.ProductCollections;

    /// <summary>
    /// Uses sliding expiration (5 minutes).
    /// Cache expires only if not accessed for 5 minutes.
    /// </summary>
    public TimeSpan? AbsoluteExpiration => null;

    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(5);
}
