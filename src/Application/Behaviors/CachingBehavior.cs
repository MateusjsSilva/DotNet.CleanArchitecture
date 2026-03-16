using CleanArchitecture.Application.Common;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CleanArchitecture.Application.Behaviors;

internal sealed class CachingBehavior<TRequest, TResponse>(
    IDistributedCache cache,
    ILogger<CachingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(5);

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // --- Read from cache (queries only) ---
        if (request is ICacheableQuery cacheableRequest)
        {
            var cached = await cache.GetStringAsync(cacheableRequest.CacheKey, cancellationToken);

            if (cached is not null)
            {
                logger.LogDebug("Cache hit for {CacheKey}", cacheableRequest.CacheKey);
                return JsonSerializer.Deserialize<TResponse>(cached)!;
            }

            logger.LogDebug("Cache miss for {CacheKey}", cacheableRequest.CacheKey);

            var response = await next();

            var expiration = cacheableRequest.AbsoluteExpiration ?? DefaultExpiration;
            await cache.SetStringAsync(
                cacheableRequest.CacheKey,
                JsonSerializer.Serialize(response),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiration },
                cancellationToken);

            return response;
        }

        // --- Execute handler ---
        var result = await next();

        // --- Invalidate cache (commands that mutate data) ---
        if (request is ICacheInvalidator invalidator)
        {
            foreach (var key in invalidator.CacheKeysToInvalidate)
            {
                await cache.RemoveAsync(key, cancellationToken);
                logger.LogDebug("Cache evicted for {CacheKey}", key);
            }
        }

        return result;
    }
}
