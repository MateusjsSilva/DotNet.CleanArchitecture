# ADR-019: Soft Delete and Optimistic Concurrency Interaction

## Status
Accepted

## Context

Two concurrent clients can interact with the same product at the same time:

- **Client A** calls `DELETE /products/{id}` — soft-deletes the product.
- **Client B** (who fetched the product before the deletion) calls `PUT /products/{id}` or `PATCH /products/{id}` with a stale `RowVersion`.

The question is: what happens, and is it intentional?

## Decision

### Scenario: Client A deletes, Client B updates concurrently

1. Client A's `DeleteProductCommandHandler` calls `product.SoftDelete()` and saves.
   EF Core sets `IsDeleted = true` on the row.

2. Client B's `UpdateProductCommandHandler` / `PatchProductCommandHandler` calls
   `productRepository.GetByIdAsync(id)`.
   Because `ApplicationDbContext` applies the global query filter `WHERE IsDeleted = 0`,
   the product is **not found** and a `NotFoundException` is thrown → HTTP **404**.

**The RowVersion is never checked** in this path because the entity lookup fails first.

### Why this is correct

- The product no longer exists from the caller's perspective (it is soft-deleted).
  Returning 404 is the correct semantic — the resource is gone.
- If we wanted to return 409 Conflict instead, we would need to bypass the query filter
  (`IgnoreQueryFilters()`) to load the deleted entity and inspect its state. This adds
  complexity and leaks infrastructure details. The 404 behaviour is simpler and sufficient.

### Audit trail

`DeletedAt` and `DeletedBy` are **not** set by `AuditableEntity.SoftDelete()`.
They are stamped by `ApplicationDbContext.SetAuditFields()` during `SaveChangesAsync`,
following the same pattern as `CreatedAt`/`UpdatedAt`.

This centralises all audit logic in one place. Callers that invoke `SoftDelete()` outside
of a persisted `SaveChanges` (e.g. unit tests) will see `DeletedAt == null` — this is
expected and documented.

### RowVersion requirement on PATCH

`PatchProductCommand` requires a non-null `RowVersion` (same as `UpdateProductCommand`).
The validator enforces this at the boundary. Allowing a PATCH without `RowVersion` would
silently bypass optimistic concurrency, which is more dangerous for partial updates than
for full PUT replacements because the caller may not realise which fields they are changing.

## Consequences

- **DELETE wins over concurrent UPDATE/PATCH**: the delete takes precedence; the updating
  client gets a 404 rather than a conflict response. This is accepted.
- **PATCH always requires RowVersion**: removes the prior inconsistency where RowVersion
  was optional on PATCH but mandatory on PUT.
- **Audit fields are single-sourced**: `DeletedAt`/`DeletedBy` are set only in
  `ApplicationDbContext`, preventing duplicated timestamp logic in the domain.
- **Admin queries must use `IgnoreQueryFilters()`** to access soft-deleted records —
  this is documented in ADR-006 and remains unchanged.
