namespace CleanArchitecture.Application.Common;

/// <summary>
/// Centralized cache key constants to avoid stringly-typed code and ensure consistency.
/// Uses namespaced prefixes to prevent conflicts between different entity types.
/// </summary>
public static class CacheKeys
{
    // Entity-specific prefixes (namespaced to prevent conflicts)
    private const string ProductEntityPrefix = "entity:product";
    private const string ProductCollectionPrefix = "collection:products";

    /// <summary>
    /// Cache registry prefix for tracking invalidation keys. Format: "registry:{prefix}"
    /// </summary>
    private const string RegistryPrefix = "registry";

    // --- Product Cache Keys ---

    /// <summary>
    /// Gets a specific product cache key for the given ID.
    /// </summary>
    /// <param name="productId">The product ID</param>
    /// <returns>Cache key in format "entity:product:{id}"</returns>
    public static string Product(Guid productId) => $"{ProductEntityPrefix}:{productId}";

    /// <summary>
    /// Prefix for product collection cache keys (lists, queries, etc.).
    /// </summary>
    public static string ProductCollections => ProductCollectionPrefix;

    /// <summary>
    /// Gets the cache registry key for a given prefix.
    /// </summary>
    /// <param name="prefix">The cache prefix to register</param>
    /// <returns>Registry key in format "registry:{prefix}"</returns>
    public static string Registry(string prefix) => $"{RegistryPrefix}:{prefix}";

    // --- Future: Ready for other entities ---
    // When adding Customer, Order, etc., use similar pattern:
    // private const string CustomerEntityPrefix = "entity:customer";
    // private const string CustomerCollectionPrefix = "collection:customers";
    // public static string Customer(Guid customerId) => $"{CustomerEntityPrefix}:{customerId}";
}