using CleanArchitecture.Application.Common;
using CleanArchitecture.Application.Common.Mediator;
using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Application.UseCases.Products.Common;

namespace CleanArchitecture.Application.UseCases.Products.Commands.PatchProduct;

/// <summary>
/// Partially updates a product. Only non-null fields are applied (HTTP PATCH semantics).
/// Supply only the properties you want to change; omit the rest.
/// RowVersion is required for optimistic concurrency — always send the current value.
/// </summary>
public sealed record PatchProductCommand(
    Guid Id,
    byte[] RowVersion,
    string? Name = null,
    string? Description = null,
    decimal? Price = null
) : ICommand<ProductDto>, ICacheInvalidator, IHasId
{
    public IEnumerable<string> CacheKeysToInvalidate => ProductCacheInvalidation.GetIndividualProductKeys(Id);
    public IEnumerable<string> CacheKeyPrefixesToInvalidate => ProductCacheInvalidation.GetProductListPrefixes();
}
