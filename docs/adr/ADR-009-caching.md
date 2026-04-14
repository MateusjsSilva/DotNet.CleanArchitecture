# ADR-009: Opt-in Caching and Invalidation via ICacheableQuery / ICacheInvalidator

## Status
Accepted

## Context
Some queries (e.g., fetching a product by ID) are called frequently with the same parameters. We want caching but without polluting every query handler with cache logic or coupling them to a specific cache provider. We also need a way to evict stale entries automatically when a command mutates data.

## Decision

Two **marker interfaces** and a single **custom pipeline behavior** `CachingBehavior` handle both concerns.

### Cache provider strategy

| Provider | When to use | DI registration |
|---|---|---|
| `DistributedMemoryCache` | Single-instance / dev / tests | `services.AddDistributedMemoryCache()` |
| Redis (`StackExchange.Redis`) | Multi-instance / production | `services.AddStackExchangeRedisCache(...)` |

The behavior depends on `IDistributedCache` — swapping the backend requires changing **only** the DI registration, not any application code.

### Read caching — ICacheableQuery

```csharp
public interface ICacheableQuery
{
    string CacheKey { get; }
    string? CacheKeyPrefix => null;         // prefix group for bulk invalidation
    TimeSpan? AbsoluteExpiration => null;   // default: 5 minutes absolute
    TimeSpan? SlidingExpiration => null;    // alternative: reset on each access
}
```

A query opts in to caching by implementing `ICacheableQuery`:

```csharp
public sealed record GetProductByIdQuery(Guid Id)
    : IQuery<ProductDto>, ICacheableQuery
{
    public string CacheKey => CacheKeys.Product(Id);
    public TimeSpan? AbsoluteExpiration => TimeSpan.FromMinutes(10);
}

public sealed record GetAllProductsQuery(...)
    : IQuery<PagedResult<ProductDto>>, ICacheableQuery
{
    public string CacheKey =>
        $"{CacheKeys.ProductCollections}:page={Page}:pageSize={PageSize}:...";
    public string? CacheKeyPrefix => CacheKeys.ProductCollections;
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(5);
}
```

On a cache hit, the handler is never called. On a miss, the response is serialized to JSON and stored via `IDistributedCache`. If `CacheKeyPrefix` is set, the key is registered in a **prefix registry** entry (`registry:{prefix}`) so prefix-based invalidation can locate it.

### Write invalidation — ICacheInvalidator

```csharp
public interface ICacheInvalidator
{
    IEnumerable<string> CacheKeysToInvalidate { get; }
    IEnumerable<string> CacheKeyPrefixesToInvalidate { get; }
}
```

A command opts in to cache eviction by implementing `ICacheInvalidator`:

```csharp
public sealed record UpdateProductCommand(Guid Id, string Name, ...)
    : ICommand<ProductDto>, ICacheInvalidator
{
    // Evicts the individual product cache entry
    public IEnumerable<string> CacheKeysToInvalidate =>
        ProductCacheInvalidation.GetIndividualProductKeys(Id);

    // Evicts all collection/summary cache entries registered under this prefix
    public IEnumerable<string> CacheKeyPrefixesToInvalidate =>
        ProductCacheInvalidation.GetProductListPrefixes();
}
```

`CachingBehavior` calls `IDistributedCache.RemoveAsync` for each individual key and for every key registered under each prefix — **after** the handler completes successfully.

### Pipeline flow

```
Request
  ↓
CachingBehavior
  ├── ICacheableQuery?
  │     ├── Cache hit  → return cached response (handler never called)
  │     └── Cache miss → call handler → store result → register key in prefix registry
  └── ICacheInvalidator? → call handler → evict individual keys → evict prefix registry keys
  ↓
Handler
```

### Cache key registry

Prefix-based invalidation works through a secondary registry entry stored in the cache:

```
Key: registry:collection:products
Value: JSON set of all active cache keys under this prefix
```

When a query with `CacheKeyPrefix = "collection:products"` stores a result, its full cache key is added to the set. When a mutation command lists `"collection:products"` in `CacheKeyPrefixesToInvalidate`, `CachingBehavior` reads the registry, removes every listed key, then removes the registry itself.

## Consequences
- **Positive**: Cache logic is centralized in one behavior — handlers have zero awareness of caching or invalidation.
- **Positive**: Opt-in per query/command; non-cacheable requests have zero overhead.
- **Positive**: Cache provider can be swapped to Redis by changing one line in DI — no code changes.
- **Positive**: Prefix-based invalidation correctly evicts all list/summary variants when any product mutation occurs.
- **Positive**: Corrupted cache entries (stale schema) are automatically evicted and re-fetched rather than causing 500 errors.
- **Negative**: In-memory distributed cache is not shared across instances (expected — swap to Redis for multi-instance).
- **Negative**: Prefix registry adds a small extra round-trip per cache write/invalidation.
