# ADR-003: GUID v7 for Entity Identifiers

## Status
Accepted

## Context
Choosing an ID strategy involves trade-offs between uniqueness, database performance, and developer ergonomics. Common options are auto-increment integers, UUID v4 (random), GUID v7 (time-ordered), and ULID.

## Decision
Use `Guid.CreateVersion7()` (.NET 9+) for all entity primary keys.

```csharp
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.CreateVersion7();
}
```

## Rationale

| Property | Auto-increment | UUID v4 | **GUID v7** |
|---|---|---|---|
| Globally unique | ❌ (per table) | ✅ | ✅ |
| Sortable by creation time | ✅ | ❌ | ✅ |
| Index fragmentation | None | High | Low |
| Requires DB round-trip to get ID | ✅ | ❌ | ❌ |
| Human-readable ordering | ✅ | ❌ | ✅ |

GUID v7 embeds a millisecond-precision timestamp in the most significant bits, making it time-ordered and reducing B-tree index fragmentation to near-zero — combining the database performance of sequential IDs with the uniqueness and independence of UUIDs.

## Consequences
- **Positive**: IDs can be generated in the application before hitting the database, enabling optimistic writes.
- **Positive**: Sequential ordering aids query performance for range scans and reduces page splits.
- **Positive**: IDs are globally unique, safe for distributed systems and future microservice extraction.
- **Negative**: 16 bytes vs 4 bytes for integers — negligible for most workloads.
