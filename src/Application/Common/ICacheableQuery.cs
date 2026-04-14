using CleanArchitecture.Application.Common.Mediator;

namespace CleanArchitecture.Application.Common;

/// <summary>
/// Marker interface for queries whose responses should be cached.
/// Implement this on any <see cref="IQuery{TResponse}"/> to opt-in to the CachingBehavior pipeline.
///
/// Example with absolute expiration (10 minutes fixed):
/// <code>
///     public sealed record GetProductByIdQuery(Guid Id)
///         : IQuery&lt;ProductDto?&gt;, ICacheableQuery
///     {
///         public string CacheKey => CacheKeys.Product(Id);
///         public TimeSpan? AbsoluteExpiration => TimeSpan.FromMinutes(10);
///         public TimeSpan? SlidingExpiration => null;
///     }
/// </code>
///
/// Example with sliding expiration (refreshes on each access, 5 minute timeout):
/// <code>
///     public sealed record GetAllProductsQuery(...)
///         : IQuery&lt;PagedResult&lt;ProductDto&gt;&gt;, ICacheableQuery
///     {
///         public string CacheKey => $"{CacheKeys.ProductCollections}:page={page}:pageSize={pageSize}";
///         public TimeSpan? AbsoluteExpiration => null;
///         public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(5);
///     }
/// </code>
/// </summary>
public interface ICacheableQuery
{
    /// <summary>Cache key unique to this query and its inputs.</summary>
    string CacheKey { get; }

    /// <summary>
    /// The prefix group this cache key belongs to.
    /// Used by the CachingBehavior registry so prefix-based invalidation
    /// (e.g. from <see cref="ICacheInvalidator.CacheKeyPrefixesToInvalidate"/>) can
    /// locate and remove all related keys at once.
    ///
    /// Must match the prefix string used in the invalidating command's
    /// <see cref="ICacheInvalidator.CacheKeyPrefixesToInvalidate"/>.
    ///
    /// Examples:
    ///   <c>CacheKeys.ProductCollections</c>  for list/summary queries
    ///   <c>CacheKeys.Product(Id)</c>          for single-entity queries (use exact key, no prefix)
    /// </summary>
    string? CacheKeyPrefix => null;

    /// <summary>
    /// Absolute expiration time (fixed duration from creation).
    /// Good for data that doesn't change frequently.
    /// If both absolute and sliding are set, absolute takes precedence.
    /// </summary>
    TimeSpan? AbsoluteExpiration => null;

    /// <summary>
    /// Sliding expiration time (resets on each access).
    /// Good for data that is accessed frequently but might be stale.
    /// Cache expires only if not accessed for the duration.
    /// </summary>
    TimeSpan? SlidingExpiration => null;
}
