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

            await cache.SetStringAsync(
                cacheableRequest.CacheKey,
                JsonSerializer.Serialize(response),
                BuildCacheOptions(cacheableRequest),
                cancellationToken);

            // Register this key in the prefix registry so prefix-based invalidation
            // can find and remove it later (e.g. when a product mutation occurs).
            await RegisterKeyInPrefixRegistryAsync(cacheableRequest.CacheKey, cancellationToken);

            logger.LogDebug(
                "Cached response for {CacheKey} with expiration {ExpirationMs}ms",
                cacheableRequest.CacheKey,
                GetExpirationMs(cacheableRequest));

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

            foreach (var prefix in invalidator.CacheKeyPrefixesToInvalidate)
            {
                await InvalidatePrefixAsync(prefix, cancellationToken);
            }
        }

        return result;
    }

    // ── Prefix registry helpers ───────────────────────────────────────────────

    /// <summary>
    /// Adds <paramref name="key"/> to a registry set stored under the distributed cache
    /// key <c>cache:registry:{prefix}</c>, where prefix is the portion of the key before
    /// the first colon (e.g. "products" for "products:page=1:...").
    /// </summary>
    private async Task RegisterKeyInPrefixRegistryAsync(string key, CancellationToken ct)
    {
        var prefix = ExtractPrefix(key);
        var registryKey = CacheKeys.Registry(prefix);

        var existing = await cache.GetStringAsync(registryKey, ct);
        var keys = existing is null
            ? new HashSet<string>()
            : JsonSerializer.Deserialize<HashSet<string>>(existing)!;

        if (!keys.Add(key))
            return; // already registered — nothing to update

        // Keep the registry alive longer than the entries themselves
        await cache.SetStringAsync(
            registryKey,
            JsonSerializer.Serialize(keys),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) },
            ct);
    }

    /// <summary>
    /// Removes every cache key registered under the given <paramref name="prefix"/>
    /// and then removes the registry entry itself.
    /// </summary>
    private async Task InvalidatePrefixAsync(string prefix, CancellationToken ct)
    {
        var registryKey = CacheKeys.Registry(prefix);

        var existing = await cache.GetStringAsync(registryKey, ct);
        if (existing is null)
        {
            logger.LogDebug("No cache registry found for prefix '{Prefix}' — nothing to invalidate.", prefix);
            return;
        }

        var keys = JsonSerializer.Deserialize<HashSet<string>>(existing)!;

        foreach (var key in keys)
        {
            await cache.RemoveAsync(key, ct);
            logger.LogDebug("Cache evicted '{CacheKey}' via prefix '{Prefix}'", key, prefix);
        }

        await cache.RemoveAsync(registryKey, ct);
        logger.LogDebug("Cache prefix registry '{RegistryKey}' cleared ({Count} entries).", registryKey, keys.Count);
    }

    private static string ExtractPrefix(string key) =>
        key.Contains(':') ? key[..key.IndexOf(':')] : key;

    // ── Cache option helpers ──────────────────────────────────────────────────

    private static DistributedCacheEntryOptions BuildCacheOptions(ICacheableQuery cacheableRequest)
    {
        var options = new DistributedCacheEntryOptions();

        if (cacheableRequest.AbsoluteExpiration.HasValue)
            options.AbsoluteExpirationRelativeToNow = cacheableRequest.AbsoluteExpiration.Value;
        else if (cacheableRequest.SlidingExpiration.HasValue)
            options.SlidingExpiration = cacheableRequest.SlidingExpiration.Value;
        else
            options.AbsoluteExpirationRelativeToNow = DefaultExpiration;

        return options;
    }

    private static double GetExpirationMs(ICacheableQuery cacheableRequest)
    {
        if (cacheableRequest.AbsoluteExpiration.HasValue)
            return cacheableRequest.AbsoluteExpiration.Value.TotalMilliseconds;

        if (cacheableRequest.SlidingExpiration.HasValue)
            return cacheableRequest.SlidingExpiration.Value.TotalMilliseconds;

        return DefaultExpiration.TotalMilliseconds;
    }
}
