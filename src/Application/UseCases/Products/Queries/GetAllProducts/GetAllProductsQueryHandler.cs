using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Domain.Interfaces;
using Mapster;
using MediatR;

namespace CleanArchitecture.Application.UseCases.Products.Queries.GetAllProducts;

internal sealed class GetAllProductsQueryHandler(IProductRepository productRepository)
    : IRequestHandler<GetAllProductsQuery, IReadOnlyList<ProductDto>>
{
    public async Task<IReadOnlyList<ProductDto>> Handle(
        GetAllProductsQuery request,
        CancellationToken cancellationToken)
    {
        var products = request.OnlyActive
            ? await productRepository.GetActiveProductsAsync(cancellationToken)
            : await productRepository.GetAllAsync(cancellationToken);

        return products.Adapt<IReadOnlyList<ProductDto>>();
    }
}
