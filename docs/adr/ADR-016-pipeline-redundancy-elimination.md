# ADR-016: Pipeline Redundancy Elimination

**Date**: 2026-03-26
**Status**: Accepted

## Context

During pipeline analysis, several redundancies were identified where the same operations were being performed multiple times across different layers of the application:

1. **Cache Invalidation Duplication**: Cache keys were being invalidated both by `CachingBehavior` via the `ICacheInvalidator` interface and again by domain event handlers.
2. **Audit Logging Duplication**: Audit trails were being created automatically by `ApplicationDbContext.SaveChangesAsync()` and also manually in domain event handlers.
3. **Excessive Documentation**: Event handlers contained extensive documentation comments for functionality that was already implemented elsewhere.

This redundancy violated DRY principles, increased maintenance burden, and created potential inconsistencies in the system.

## Decision

### 1. Simplify Domain Event Handlers

Domain event handlers have been simplified to focus solely on their unique responsibilities:

- **Removed**: Duplicate cache invalidation logic
- **Removed**: Manual audit logging
- **Kept**: Metrics collection for observability
- **Kept**: Essential logging for operational visibility

**Before** (redundant):
```csharp
public async Task Handle(ProductUpdatedEvent notification, CancellationToken cancellationToken)
{
    // Cache invalidation (DUPLICATE - already handled by CachingBehavior)
    await cacheInvalidator.InvalidateAsync($"product:{notification.ProductId}");

    // Manual audit logging (DUPLICATE - already handled by ApplicationDbContext)
    logger.LogInformation("Audit: Product {Id} updated by {User}", notification.ProductId, currentUser.UserName);

    // Metrics (UNIQUE - kept)
    RecordMetrics(notification);
}
```

**After** (focused):
```csharp
public Task Handle(ProductUpdatedEvent notification, CancellationToken cancellationToken)
{
    logger.LogInformation("Product updated: {ProductId} - {ProductName}",
        notification.ProductId, notification.ProductName);

    RecordMetrics(notification);
    return Task.CompletedTask;
}
```

### 2. Cache Invalidation Pipeline

Cache invalidation follows a single, consistent path:

```
Command → CachingBehavior → ICacheInvalidator.CacheKeysToInvalidate
```

Commands that mutate cached data implement `ICacheInvalidator`:
```csharp
public sealed record UpdateProductCommand : ICacheInvalidator
{
    public IEnumerable<string> CacheKeysToInvalidate => [$"product:{Id}"];
}
```

### 3. Audit Trail Pipeline

Audit logging follows a single, automatic path:

```
Entity Change → ApplicationDbContext.SaveChangesAsync() → AuditableEntity fields populated
```

All entities inheriting from `AuditableEntity` get audit fields automatically populated via the `ICurrentUserService` as documented in ADR-011.

### 4. Event Handler Responsibilities

Domain event handlers are now focused on their core unique purposes:

| Handler | Responsibility |
|---------|---------------|
| `ProductCreatedEventHandler` | Metrics collection, operational logging |
| `ProductUpdatedEventHandler` | Metrics collection, operational logging |
| `ProductDeletedEventHandler` | Metrics collection, operational logging |
| `ProductPatchedEventHandler` | Field-level metrics, operational logging |

## Consequences

### Positive
- **Single Responsibility**: Each component has one clear purpose
- **No Duplication**: Cache invalidation and audit logging happen exactly once
- **Maintainable**: Changes to caching or auditing logic require updates in only one place
- **Consistent**: All commands follow the same pattern for cache invalidation
- **Performance**: Eliminated unnecessary duplicate operations
- **Clarity**: Event handlers focus on their unique side effects only

### Negative
- **Less Explicit**: Cache invalidation is now implicit via the `ICacheInvalidator` interface rather than explicit in event handlers
- **Learning Curve**: Developers need to understand that cache invalidation happens automatically via commands, not events

## Related ADRs

- [ADR-009: Caching Strategy](ADR-009-caching.md) - Defines the `ICacheInvalidator` interface
- [ADR-011: Audit Trail](ADR-011-audit-trail.md) - Defines automatic audit via `ApplicationDbContext`
- [ADR-005: Domain Events & Outbox](ADR-005-domain-events-outbox.md) - Defines event handler responsibilities
- [ADR-002: CQRS with MediatR](ADR-002-cqrs-mediatr.md) - Defines pipeline behavior execution order