using CleanArchitecture.Application.Common;
using CleanArchitecture.Application.Common.Mediator;
using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Application.UseCases.Products.Common;
using CleanArchitecture.Application.Validators.Common;

namespace CleanArchitecture.Application.UseCases.Products.Commands.UpdateProduct;

public sealed record UpdateProductCommand(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    byte[] RowVersion
) : ICommand<ProductDto>, ICacheInvalidator, IHasId
{
    public IEnumerable<string> CacheKeysToInvalidate => ProductCacheInvalidation.GetIndividualProductKeys(Id);
    public IEnumerable<string> CacheKeyPrefixesToInvalidate => ProductCacheInvalidation.GetProductListPrefixes();
}
