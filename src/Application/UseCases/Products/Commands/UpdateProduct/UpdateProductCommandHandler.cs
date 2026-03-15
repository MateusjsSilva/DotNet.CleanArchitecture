using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Domain.Exceptions;
using CleanArchitecture.Domain.Interfaces;
using MediatR;

namespace CleanArchitecture.Application.UseCases.Products.Commands.UpdateProduct;

internal sealed class UpdateProductCommandHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(
        UpdateProductCommand request,
        CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Product", request.Id);

        product.Update(request.Name, request.Description, request.Price);

        productRepository.Update(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return product.ToDto();
    }
}
