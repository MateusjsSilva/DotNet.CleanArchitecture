using CleanArchitecture.Application.Common;
using CleanArchitecture.Application.Common.Mediator;
using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Application.UseCases.Products.Common;

namespace CleanArchitecture.Application.UseCases.Products.Commands.CreateProduct;

public sealed record CreateProductCommand(
    string Name,
    string? Description,
    decimal Price
) : ICommand<ProductDto>, ICacheInvalidator
{
    // A new product affects all paginated list results - no specific product key since it's a new entity.
    public IEnumerable<string> CacheKeysToInvalidate => [];
    public IEnumerable<string> CacheKeyPrefixesToInvalidate => ProductCacheInvalidation.GetProductListPrefixes();
}
