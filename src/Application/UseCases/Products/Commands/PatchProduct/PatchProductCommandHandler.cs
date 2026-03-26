using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Domain.Exceptions;
using CleanArchitecture.Domain.Interfaces;
using MediatR;

namespace CleanArchitecture.Application.UseCases.Products.Commands.PatchProduct;

internal sealed class PatchProductCommandHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<PatchProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(
        PatchProductCommand request,
        CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Product", request.Id);

        product.Patch(request.Name, request.Description, request.Price);

        productRepository.Update(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return product.ToDto();
    }
}
