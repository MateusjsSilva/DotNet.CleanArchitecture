# ADR-018: IApplicationDbContext as a Read-Only Contract

**Date**: 2026-04-13
**Status**: Accepted

## Context

`IApplicationDbContext` is the Application layer's abstraction over the EF Core `DbContext`. It was originally allowed to expose both `DbSet` properties and `SaveChangesAsync`:

```csharp
public interface IApplicationDbContext
{
    DbSet<MyEntity> MyEntities { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

The presence of `SaveChangesAsync` created a second persistence path running in parallel with `IUnitOfWork`. Any code injecting `IApplicationDbContext` could call `SaveChangesAsync` and bypass the unit of work, weakening the transactional guarantee that all changes in a command are committed atomically.

## Observed Risk

Command handlers are supposed to commit changes exclusively through `IUnitOfWork`:

```csharp
await repository.AddAsync(entity, cancellationToken);
await unitOfWork.SaveChangesAsync(cancellationToken);
```

With `SaveChangesAsync` present on `IApplicationDbContext`, a developer writing a new handler could accidentally write:

```csharp
await context.MyEntities.AddAsync(entity, cancellationToken);
await context.SaveChangesAsync(cancellationToken);
```

That bypasses unit-of-work-level hooks, transaction handling, and the consistency expected by the rest of the codebase.

## Decision

Remove `SaveChangesAsync` from `IApplicationDbContext`, restricting it to a **read-only DbSet provider**:

```csharp
public interface IApplicationDbContext
{
    DbSet<MyEntity> MyEntities { get; }
}
```

## Persistence Responsibility Map

| Scenario | Interface to use |
|---|---|
| Query handler reads data | `IApplicationDbContext` or a feature-specific Dapper query interface |
| Command handler persists data | `IUnitOfWork.SaveChangesAsync()` |
| Repository stages aggregate changes | Repository methods, committed by the handler through `IUnitOfWork` |
| Background service commits infrastructure work | `IUnitOfWork` or direct `ApplicationDbContext` inside Infrastructure |

## Consequences

- **Positive**: `IUnitOfWork` is the only way Application code can commit changes.
- **Positive**: `IApplicationDbContext` has a single responsibility: expose read-side `DbSet` access.
- **Positive**: Test doubles for `IApplicationDbContext` no longer need to stub `SaveChangesAsync`.
- **Positive**: Code that tries to call `context.SaveChangesAsync()` through `IApplicationDbContext` will not compile.
- **Negative**: Existing consumers that called `IApplicationDbContext.SaveChangesAsync()` must migrate to `IUnitOfWork`.

## Related ADRs

- [ADR-001: Clean Architecture Layers](ADR-001-clean-architecture.md)
- [ADR-005: Domain Events & Outbox](ADR-005-domain-events-outbox.md)
- [ADR-011: Audit Trail](ADR-011-audit-trail.md)
