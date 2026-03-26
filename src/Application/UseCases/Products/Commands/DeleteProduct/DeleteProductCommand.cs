using CleanArchitecture.Application.Common;
using CleanArchitecture.Application.Common.Mediator;
using CleanArchitecture.Application.UseCases.Products.Common;

namespace CleanArchitecture.Application.UseCases.Products.Commands.DeleteProduct;

public sealed record DeleteProductCommand(Guid Id) : ICommand, ICacheInvalidator
{
    public IEnumerable<string> CacheKeysToInvalidate => ProductCacheInvalidation.GetIndividualProductKeys(Id);
    public IEnumerable<string> CacheKeyPrefixesToInvalidate => ProductCacheInvalidation.GetProductListPrefixes();
}
