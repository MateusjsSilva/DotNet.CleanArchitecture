using MediatR;

namespace CleanArchitecture.Application.UseCases.Products.Commands.DeleteProduct;

public sealed record DeleteProductCommand(Guid Id) : IRequest;
