using CleanArchitecture.Application.Common;
using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Application.UseCases.Products.Common;
using CleanArchitecture.Application.Validators.Common;
using MediatR;

namespace CleanArchitecture.Application.UseCases.Products.Commands.UpdateProduct;

public sealed record UpdateProductCommand(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    byte[] RowVersion
) : IRequest<ProductDto>, ICacheInvalidator, IHasId
{
    public IEnumerable<string> CacheKeysToInvalidate => ProductCacheInvalidation.GetIndividualProductKeys(Id);
    public IEnumerable<string> CacheKeyPrefixesToInvalidate => ProductCacheInvalidation.GetProductListPrefixes();
}
