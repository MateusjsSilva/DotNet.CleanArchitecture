using CleanArchitecture.Application.Common;
using CleanArchitecture.Application.UseCases.Products.Common;
using MediatR;

namespace CleanArchitecture.Application.UseCases.Products.Commands.DeleteProduct;

public sealed record DeleteProductCommand(Guid Id) : IRequest, ICacheInvalidator
{
    public IEnumerable<string> CacheKeysToInvalidate => ProductCacheInvalidation.GetIndividualProductKeys(Id);
    public IEnumerable<string> CacheKeyPrefixesToInvalidate => ProductCacheInvalidation.GetProductListPrefixes();
}
