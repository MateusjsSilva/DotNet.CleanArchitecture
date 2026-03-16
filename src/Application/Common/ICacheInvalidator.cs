namespace CleanArchitecture.Application.Common;

/// <summary>
/// Marker interface for commands/queries that should evict one or more cache entries
/// after the handler executes. Implement alongside IRequest to opt-in to automatic
/// cache invalidation via the CachingBehavior pipeline.
/// </summary>
public interface ICacheInvalidator
{
    /// <summary>Cache keys to remove after the handler completes successfully.</summary>
    IEnumerable<string> CacheKeysToInvalidate { get; }
}
