namespace CleanArchitecture.Application.Common;

/// <summary>
/// Marker interface for commands that should evict cache entries after the handler
/// executes. Implement alongside IRequest to opt-in to automatic cache invalidation
/// via the CachingBehavior pipeline.
/// </summary>
public interface ICacheInvalidator
{
    /// <summary>Exact cache keys to remove after the handler completes successfully.</summary>
    IEnumerable<string> CacheKeysToInvalidate { get; }

    /// <summary>
    /// Top-level key prefixes whose entire cached sets should be invalidated.
    /// The CachingBehavior maintains a registry of all keys per prefix and removes
    /// all of them when this list is non-empty.
    /// Example: "products" removes every cached <see cref="T:GetAllProductsQuery"/> page.
    /// </summary>
    IEnumerable<string> CacheKeyPrefixesToInvalidate => [];
}
