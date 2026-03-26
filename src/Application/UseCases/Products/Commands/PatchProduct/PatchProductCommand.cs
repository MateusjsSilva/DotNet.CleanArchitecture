using CleanArchitecture.Application.Common;
using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Application.UseCases.Products.Common;
using MediatR;

namespace CleanArchitecture.Application.UseCases.Products.Commands.PatchProduct;

/// <summary>
/// Partially updates a product. Only non-null fields are applied (HTTP PATCH semantics).
/// Supply only the properties you want to change; omit the rest.
/// </summary>
public sealed record PatchProductCommand(
    Guid Id,
    string? Name = null,
    string? Description = null,
    decimal? Price = null
) : IRequest<ProductDto>, ICacheInvalidator
{
    public IEnumerable<string> CacheKeysToInvalidate => ProductCacheInvalidation.GetIndividualProductKeys(Id);
    public IEnumerable<string> CacheKeyPrefixesToInvalidate => ProductCacheInvalidation.GetProductListPrefixes();
}
