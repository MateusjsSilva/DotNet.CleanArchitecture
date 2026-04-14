# ADR-018: IApplicationDbContext as a Read-Only Contract

**Date**: 2026-04-13
**Status**: Accepted

## Context

`IApplicationDbContext` is the Application layer's abstraction over the EF Core `DbContext`. It was originally defined as:

```csharp
public interface IApplicationDbContext
{
    DbSet<Product> Products { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

The presence of `SaveChangesAsync` on this interface created a second persistence path running in parallel with `IUnitOfWork`. Any code injecting `IApplicationDbContext` could call `SaveChangesAsync` and bypass the Unit of Work, breaking the transactional guarantee that all changes in a command are committed atomically.

### Observed risk

Command handlers are supposed to commit changes exclusively through `IUnitOfWork`:

```csharp
// Correct — single save path
await productRepository.AddAsync(product, cancellationToken);
await unitOfWork.SaveChangesAsync(cancellationToken);
```

With `SaveChangesAsync` present on `IApplicationDbContext`, a developer writing a new handler could accidentally write:

```csharp
// Accidental double-save path — bypasses UoW
await context.Products.AddAsync(product, cancellationToken);
await context.SaveChangesAsync(cancellationToken); // ← wrong
```

This bypasses any future UoW-level hooks (e.g., cross-aggregate saga, transaction scope) and makes the codebase inconsistent.

## Decision

Remove `SaveChangesAsync` from `IApplicationDbContext`, restricting it to a **read-only DbSet provider**:

```csharp
/// <summary>
/// Read-side abstraction over the database context.
/// Exposes only DbSet properties — never SaveChanges.
/// Commands must persist via <see cref="IUnitOfWork"/>.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Product> Products { get; }
}
```

### Persistence responsibility map

| Scenario | Interface to use |
|---|---|
| Query handler reads data | `IApplicationDbContext` (or `IProductQueries` for Dapper) |
| Command handler persists data | `IUnitOfWork.SaveChangesAsync()` |
| Repository saves aggregate | via `IUnitOfWork` injected into the handler |
| Background service commits outbox | `IUnitOfWork` or direct `ApplicationDbContext` (Infrastructure layer) |

## Consequences

### Positive
- **Single persistence path**: `IUnitOfWork` is the only way Application code can commit changes — no accidental bypasses.
- **Interface Segregation**: `IApplicationDbContext` now has a single responsibility — expose DbSets for reading.
- **Simpler mocking**: Test doubles for `IApplicationDbContext` no longer need to stub `SaveChangesAsync`.
- **Compile-time enforcement**: Code that tries to call `context.SaveChangesAsync()` via `IApplicationDbContext` will not compile.

### Negative
- **Breaking change for consumers**: Any existing code calling `IApplicationDbContext.SaveChangesAsync()` must be migrated to `IUnitOfWork`. (No such usage exists in this codebase.)

## Related ADRs

- [ADR-001: Clean Architecture Layers](ADR-001-clean-architecture.md) — Interface Segregation Principle
- [ADR-005: Domain Events & Outbox](ADR-005-domain-events-outbox.md) — Transactional outbox relies on UoW commit
- [ADR-011: Audit Trail](ADR-011-audit-trail.md) — Audit fields populated inside `ApplicationDbContext.SaveChangesAsync`
