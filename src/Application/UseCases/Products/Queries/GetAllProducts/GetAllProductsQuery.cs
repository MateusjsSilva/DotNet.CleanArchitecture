using CleanArchitecture.Application.Common;
using CleanArchitecture.Application.DTOs;
using MediatR;

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
    decimal? MaxPrice = null) : IRequest<PagedResult<ProductDto>>;
