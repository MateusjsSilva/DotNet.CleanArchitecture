# ADR-005: Domain Events with Outbox Pattern

## Status
Accepted

## Context
Domain operations need to trigger side effects (e.g., `ProductCreatedEvent` → send notification, update read model). Two approaches exist:

1. **Synchronous dispatch** — publish events inside `SaveChangesAsync` via MediatR. Simple, but if the process crashes after `SaveChanges` succeeds and before the handler runs, the event is lost.
2. **Outbox Pattern** — persist events to a DB table in the same transaction as the entity change, then dispatch asynchronously from a background service.

## Decision
Use the **Outbox Pattern**.

### How it works

1. When `Product.Create()` is called, it raises `ProductCreatedEvent` via `RaiseDomainEvent()` (in-memory, no I/O).
2. In `ApplicationDbContext.SaveChangesAsync()`, before calling `base.SaveChangesAsync()`, domain events are serialized to `OutboxMessage` rows and added to the EF change tracker.
3. `base.SaveChangesAsync()` persists both the entity change **and** the outbox messages **in one transaction**.
4. `OutboxProcessorService` (a `BackgroundService`) polls the `OutboxMessages` table every 10 seconds, deserializes each event, and publishes it via `IPublisher` (MediatR).
5. `ProductCreatedEventHandler` (an `INotificationHandler<ProductCreatedEvent>`) receives the event and performs side effects.

```
Entity change + OutboxMessage → same DB transaction → guaranteed delivery
```

### OutboxMessage schema

| Column | Type | Description |
|---|---|---|
| `Id` | `Guid` (v7) | Primary key |
| `Type` | `nvarchar(500)` | Assembly-qualified CLR type name |
| `Content` | `nvarchar(max)` | JSON-serialized event payload |
| `OccurredAt` | `datetime2` | When the event was raised |
| `ProcessedAt` | `datetime2?` | Null = pending; set when dispatched |
| `Error` | `nvarchar(max)?` | Set if dispatch fails |

## Consequences
- **Positive**: Event delivery is guaranteed even if the process restarts mid-dispatch.
- **Positive**: Entity change and event publication are atomic (same transaction).
- **Positive**: Domain layer stays free of infrastructure concerns — entities just call `RaiseDomainEvent()`.
- **Negative**: Events are delivered with eventual consistency (up to 10 s delay). For same-request consistency, synchronous handlers can still be used via `INotificationHandler`.
- **Negative**: Adds operational complexity — the `OutboxMessages` table must be included in database maintenance and monitoring.
