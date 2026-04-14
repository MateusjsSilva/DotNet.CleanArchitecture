using CleanArchitecture.Application.Common;
using CleanArchitecture.Application.Common.Mediator;
using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Domain.Interfaces;

namespace CleanArchitecture.Application.UseCases.Products.Queries.GetAllProducts;

internal sealed class GetAllProductsQueryHandler(IProductRepository productRepository)
    : IQueryHandler<GetAllProductsQuery, PagedResult<ProductDto>>
{
    public async Task<PagedResult<ProductDto>> Handle(
        GetAllProductsQuery request,
        CancellationToken cancellationToken)
    {
        var (items, totalCount) = await productRepository.GetPagedAsync(
            request.OnlyActive,
            request.NameContains,
            request.MinPrice,
            request.MaxPrice,
            request.OrderBy,
            request.Ascending,
            request.Page,
            request.PageSize,
            cancellationToken);

        return new PagedResult<ProductDto>(items.ToDtoList(), request.Page, request.PageSize, totalCount);
    }
}
