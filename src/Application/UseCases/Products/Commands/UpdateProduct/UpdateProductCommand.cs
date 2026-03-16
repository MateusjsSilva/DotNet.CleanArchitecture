using CleanArchitecture.Application.Common;
using CleanArchitecture.Application.DTOs;
using MediatR;

namespace CleanArchitecture.Application.UseCases.Products.Commands.UpdateProduct;

public sealed record UpdateProductCommand(
    Guid Id,
    string Name,
    string? Description,
    decimal Price
) : IRequest<ProductDto>, ICacheInvalidator
{
    public IEnumerable<string> CacheKeysToInvalidate =>
    [
        $"product:{Id}"
    ];
}
