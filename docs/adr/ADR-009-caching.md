# ADR-009: Opt-in Caching and Invalidation via ICacheableQuery / ICacheInvalidator

## Status
Accepted

## Context
Some queries (e.g., fetching a product by ID) are called frequently with the same parameters. We want caching but without polluting every query handler with cache logic or coupling them to a specific cache provider. We also need a way to evict stale entries automatically when a command mutates data.

## Decision

Two **marker interfaces** and a single **MediatR pipeline behavior** `CachingBehavior` handle both concerns.

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
    TimeSpan? AbsoluteExpiration => null; // default: 5 minutes
}
```

A query opts in to caching by implementing `ICacheableQuery`:

```csharp
public sealed record GetProductByIdQuery(Guid Id)
    : IRequest<ProductDto?>, ICacheableQuery
{
    public string CacheKey => $"product:{Id}";
    public TimeSpan? AbsoluteExpiration => TimeSpan.FromMinutes(10);
}
```

On a cache hit, the handler is never called. On a miss, the response is serialized to JSON and stored via `IDistributedCache`.

### Write invalidation — ICacheInvalidator

```csharp
public interface ICacheInvalidator
{
    IEnumerable<string> CacheKeysToInvalidate { get; }
}
```

A command opts in to cache eviction by implementing `ICacheInvalidator`:

```csharp
public sealed record UpdateProductCommand(Guid Id, string Name, ...)
    : IRequest<ProductDto>, ICacheInvalidator
{
    public IEnumerable<string> CacheKeysToInvalidate => [$"product:{Id}"];
}
```

`CachingBehavior` calls `IDistributedCache.RemoveAsync` for each key **after** the handler completes successfully.

### Pipeline flow

```
Request
  ↓
CachingBehavior
  ├── ICacheableQuery? → try cache → hit: return / miss: call handler → store result
  └── ICacheInvalidator? → call handler → evict keys
  ↓
Handler
```

## Consequences
- **Positive**: Cache logic is centralized in one behavior — handlers have zero awareness of caching or invalidation.
- **Positive**: Opt-in per query/command; non-cacheable requests have zero overhead.
- **Positive**: Cache provider can be swapped to Redis by changing one line in DI — no code changes.
- **Positive**: Invalidation is explicit and co-located with the command that causes the mutation.
- **Negative**: In-memory distributed cache is not shared across instances (expected — swap to Redis for multi-instance).
- **Negative**: Keys must be kept in sync between `ICacheableQuery.CacheKey` and `ICacheInvalidator.CacheKeysToInvalidate` manually.
