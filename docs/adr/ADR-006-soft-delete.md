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

## Performance: index on `IsDeleted`

The global filter `WHERE IsDeleted = 0` appended to every query can force a full table scan without a supporting index. Two strategies:

**Option A — Filtered index (recommended for large tables)**
```sql
-- Indexes only the live rows; near-zero storage for the deleted minority.
CREATE INDEX IX_Products_IsDeleted_Id
    ON Products (IsDeleted, Id)
    WHERE IsDeleted = 0;
```
Add this in the EF configuration via `HasIndex` with `HasFilter`:
```csharp
builder.HasIndex(p => new { p.IsDeleted, p.Id })
       .HasFilter("[IsDeleted] = 0");
```

**Option B — Composite index (simpler, no filter clause)**
```csharp
builder.HasIndex(p => new { p.IsDeleted, p.Name });
```

The template does **not** add these indexes automatically because the right strategy depends on the entity's query patterns and expected deleted-to-live ratio. Add them when you add your first entity with meaningful data volume.

**UNIQUE constraints** on soft-deletable tables require a filtered index to allow re-insertion of a logically deleted name:
```sql
CREATE UNIQUE INDEX UX_Products_Name
    ON Products (Name)
    WHERE IsDeleted = 0;
```
Without `WHERE IsDeleted = 0`, attempting to re-create a deleted product with the same name will violate the constraint.

## GDPR / Data Erasure Compliance

Soft delete retains all personal data indefinitely. If your application is subject to GDPR (or similar regulations) and receives a **right-to-erasure** request, soft delete alone is insufficient.

Options:
1. **Data redaction on soft delete** — overwrite personal fields (`Name`, `Email`, etc.) with anonymised values before setting `IsDeleted = true`.
2. **Separate archive table** — move soft-deleted rows to a cold-storage table with a shorter retention policy.
3. **Scheduled hard delete** — a background job permanently deletes rows where `IsDeleted = true AND DeletedAt < (UtcNow - retentionWindow)`.

The right strategy depends on your retention policy. This template does not implement any of these options to avoid prescribing a compliance approach; the implementer must add one before handling personal data in a regulated context.
