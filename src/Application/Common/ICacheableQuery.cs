namespace CleanArchitecture.Application.Common;

/// <summary>
/// Marker interface for queries whose responses should be cached.
/// Implement this on any IRequest&lt;TResponse&gt; to opt-in to the CachingBehavior pipeline.
/// </summary>
public interface ICacheableQuery
{
    /// <summary>Cache key unique to this query and its inputs.</summary>
    string CacheKey { get; }

    /// <summary>How long the cached value should live. Null = sliding expiration default.</summary>
    TimeSpan? AbsoluteExpiration => null;
}
