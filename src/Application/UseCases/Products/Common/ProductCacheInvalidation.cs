using CleanArchitecture.Application.Common;

namespace CleanArchitecture.Application.UseCases.Products.Common;

/// <summary>
/// Helper class providing standard cache invalidation patterns for product-related operations.
/// Centralizes cache invalidation logic to avoid duplication across commands.
/// </summary>
public static class ProductCacheInvalidation
{
    /// <summary>
    /// Gets cache keys to invalidate for a specific product.
    /// </summary>
    /// <param name="productId">The product ID</param>
    /// <returns>Array of specific product cache keys to invalidate</returns>
    public static IEnumerable<string> GetIndividualProductKeys(Guid productId)
    {
        return [CacheKeys.Product(productId)];
    }

    /// <summary>
    /// Gets cache key prefixes to invalidate for product list operations.
    /// </summary>
    /// <returns>Array of cache prefixes to invalidate (affects all product lists)</returns>
    public static IEnumerable<string> GetProductListPrefixes()
    {
        return [CacheKeys.ProductCollections];
    }
}