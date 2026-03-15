using CleanArchitecture.Application.Common;
using CleanArchitecture.Application.DTOs;
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

        var totalCount = all.Count;

        var items = all
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToDtoList();

        return new PagedResult<ProductDto>(items, request.Page, request.PageSize, totalCount);
    }
}
