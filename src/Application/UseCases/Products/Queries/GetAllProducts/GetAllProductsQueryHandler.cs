using CleanArchitecture.Application.Common;
using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Interfaces;
using MediatR;

namespace CleanArchitecture.Application.UseCases.Products.Queries.GetAllProducts;

internal sealed class GetAllProductsQueryHandler(IProductRepository productRepository)
    : IRequestHandler<GetAllProductsQuery, PagedResult<ProductDto>>
{
    public async Task<PagedResult<ProductDto>> Handle(
        GetAllProductsQuery request,
        CancellationToken cancellationToken)
    {
        var all = request.OnlyActive
            ? await productRepository.GetActiveProductsAsync(cancellationToken)
            : await productRepository.GetAllAsync(cancellationToken);

        IEnumerable<Product> filtered = all;

        if (!string.IsNullOrWhiteSpace(request.NameContains))
            filtered = filtered.Where(p => p.Name.Contains(request.NameContains, StringComparison.OrdinalIgnoreCase));

        if (request.MinPrice.HasValue)
            filtered = filtered.Where(p => p.Price >= request.MinPrice.Value);

        if (request.MaxPrice.HasValue)
            filtered = filtered.Where(p => p.Price <= request.MaxPrice.Value);

        var filteredList = filtered.ToList();
        var totalCount = filteredList.Count;

        var sorted = Sort(filteredList, request.OrderBy, request.Ascending);

        var items = sorted
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToDtoList();

        return new PagedResult<ProductDto>(items, request.Page, request.PageSize, totalCount);
    }

    private static IEnumerable<Product> Sort(
        IEnumerable<Product> products,
        string orderBy,
        bool ascending) =>
        orderBy.ToLowerInvariant() switch
        {
            "name"  => ascending ? products.OrderBy(p => p.Name)       : products.OrderByDescending(p => p.Name),
            "price" => ascending ? products.OrderBy(p => p.Price)      : products.OrderByDescending(p => p.Price),
            _       => ascending ? products.OrderBy(p => p.CreatedAt)  : products.OrderByDescending(p => p.CreatedAt)
        };
}
