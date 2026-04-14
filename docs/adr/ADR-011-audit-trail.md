# ADR-011: Automatic Audit Trail via ICurrentUserService

## Status
Accepted

## Context
All mutating operations on auditable entities should record who performed them and when. This is a cross-cutting concern that should not require explicit code in every handler.

## Decision
`AuditableEntity` carries six audit fields automatically populated by `ApplicationDbContext.SaveChangesAsync()`:

| Field | Set on | Source |
|---|---|---|
| `CreatedAt` | Insert | `DateTime.UtcNow` |
| `CreatedBy` | Insert | `ICurrentUserService.UserName` |
| `UpdatedAt` | Update (including soft delete) | `DateTime.UtcNow` |
| `UpdatedBy` | Update (including soft delete) | `ICurrentUserService.UserName` |
| `DeletedAt` | Soft delete (first `SaveChanges` after `SoftDelete()`) | `DateTime.UtcNow` in `ApplicationDbContext.SetAuditFields()` |
| `DeletedBy` | Soft delete (first `SaveChanges` after `SoftDelete()`) | `ICurrentUserService.UserName` in `ApplicationDbContext.SetAuditFields()` |

`ICurrentUserService` is an Application-layer interface implemented in Infrastructure by `CurrentUserService`, which reads the authenticated user from `IHttpContextAccessor`:

```csharp
public string? UserName =>
    httpContextAccessor.HttpContext?.User
        ?.FindFirstValue(ClaimTypes.Email);
```

For unauthenticated requests (e.g., public endpoints, background services, EF CLI tools), `UserName` returns `null` — the audit fields are simply left empty without throwing.

> **Note on DeletedAt/By**: `AuditableEntity.SoftDelete()` sets only `IsDeleted = true`. The `DeletedAt` and `DeletedBy` fields are stamped inside `ApplicationDbContext.SetAuditFields()` during `SaveChangesAsync`, matching the same centralized pattern as `CreatedAt`/`UpdatedAt`. This means `DeletedAt` is `null` between calling `SoftDelete()` and the subsequent `SaveChangesAsync` — e.g., in unit tests that do not hit the database. This is expected and documented.

## Consequences
- **Positive**: All handlers get audit fields for free — no `CreatedBy = currentUser` in every command handler.
- **Positive**: `ICurrentUserService` is an Application-layer abstraction; the Infrastructure implementation can be swapped (e.g., for a gRPC context in non-HTTP workloads).
- **Negative**: Relies on `IHttpContextAccessor`, which is only populated within an HTTP request context. Background jobs and CLI tools see `null` user.
