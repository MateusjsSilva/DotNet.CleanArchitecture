using CleanArchitecture.Domain.Exceptions;
using CleanArchitecture.Domain.Interfaces;
using MediatR;

namespace CleanArchitecture.Application.UseCases.Products.Commands.DeleteProduct;

internal sealed class DeleteProductCommandHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteProductCommand>
{
    public async Task Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Product", request.Id);

        product.SoftDelete();
        productRepository.Update(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
