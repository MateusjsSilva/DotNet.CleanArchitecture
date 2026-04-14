# ADR-006: Soft Delete with ISoftDeletable and Global Query Filter

## Status
Accepted

## Context
Deleting records permanently makes it impossible to audit what happened, recover mistakes, or maintain referential integrity in relational data. Most business domains benefit from keeping deleted records accessible to administrators while hiding them from normal operations.

## Decision
Implement soft delete via the `ISoftDeletable` marker interface applied to `AuditableEntity`.

```csharp
public interface ISoftDeletable
{
    bool IsDeleted { get; }
    DateTime? DeletedAt { get; }
    string? DeletedBy { get; }
}

public abstract class AuditableEntity : BaseEntity, ISoftDeletable
{
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; set; }   // stamped by ApplicationDbContext, not here
    public string? DeletedBy { get; set; }

    /// <summary>
    /// Marks the entity as soft-deleted. Audit fields (DeletedAt, DeletedBy) are stamped
    /// by ApplicationDbContext.SetAuditFields() during SaveChangesAsync — not here —
    /// keeping all audit logic in one place (see ADR-011).
    /// </summary>
    public void SoftDelete()
    {
        if (IsDeleted) return; // idempotent
        IsDeleted = true;
    }
}
```

### EF Core Global Query Filter

`ApplicationDbContext.OnModelCreating` applies a `!IsDeleted` filter to **all** entity types that implement `ISoftDeletable`, using reflection:

```csharp
private static void ApplySoftDeleteQueryFilters(ModelBuilder modelBuilder)
{
    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
        if (!typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType)) continue;
        // apply Expression<Func<TEntity, bool>> e => !e.IsDeleted
    }
}
```

### Delete flow

`DeleteProductCommandHandler` calls `product.SoftDelete()` + `repository.Update(product)` instead of `repository.Remove(product)`. The record remains in the database but is invisible to all queries.

## Consequences
- **Positive**: Deleted records are preserved for auditing and recovery.
- **Positive**: The global query filter is transparent — repositories and queries do not need to know about soft delete.
- **Positive**: Adding soft delete to a new entity type only requires implementing `ISoftDeletable` (inherited via `AuditableEntity`).
- **Negative**: The database grows over time. A scheduled purge job can permanently delete records older than a retention window.
- **Negative**: `IgnoreQueryFilters()` must be used in admin queries that need to see deleted records.
