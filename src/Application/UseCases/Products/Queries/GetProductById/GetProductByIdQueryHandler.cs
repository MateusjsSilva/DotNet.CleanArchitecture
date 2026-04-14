using CleanArchitecture.Application.Common.Mediator;
using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Domain.Exceptions;
using CleanArchitecture.Domain.Interfaces;

namespace CleanArchitecture.Application.UseCases.Products.Queries.GetProductById;

internal sealed class GetProductByIdQueryHandler(IProductRepository productRepository)
    : IQueryHandler<GetProductByIdQuery, ProductDto>
{
    public async Task<ProductDto> Handle(
        GetProductByIdQuery request,
        CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Product", request.Id);

        return product.ToDto();
    }
}
