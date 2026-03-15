using CleanArchitecture.Application.Common;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Application.Behaviors;

internal sealed class CachingBehavior<TRequest, TResponse>(
    IMemoryCache cache,
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
        if (request is not ICacheableQuery cacheableRequest)
            return await next();

        if (cache.TryGetValue(cacheableRequest.CacheKey, out TResponse? cached))
        {
            logger.LogDebug("Cache hit for {CacheKey}", cacheableRequest.CacheKey);
            return cached!;
        }

        logger.LogDebug("Cache miss for {CacheKey}", cacheableRequest.CacheKey);

        var response = await next();

        var expiration = cacheableRequest.AbsoluteExpiration ?? DefaultExpiration;
        cache.Set(cacheableRequest.CacheKey, response, expiration);

        return response;
    }
}
