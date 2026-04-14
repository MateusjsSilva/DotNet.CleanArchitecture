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
4. `OutboxProcessorService` (a `BackgroundService`) polls the `OutboxMessages` table every 10 seconds, deserializes each event, and publishes it via `IMediator.PublishAsync` (custom mediator — see ADR-017).
5. `ProductCreatedEventHandler` (an `IDomainEventHandler<ProductCreatedEvent>`) receives the event and performs side effects (metrics, logging).

```
Entity change + OutboxMessage → same DB transaction → guaranteed delivery
```

### OutboxMessage schema

| Column | Type | Description |
|---|---|---|
| `Id` | `Guid` (v7) | Primary key |
| `Type` | `nvarchar(500)` | Assembly-qualified CLR type name |
| `Content` | `nvarchar(max)` | JSON-serialized event payload |
| `EventVersion` | `int` (default 1) | Schema version for migration support |
| `OccurredAt` | `datetime2` | When the event was raised |
| `ProcessedAt` | `datetime2?` | Null = pending; set when dispatched |
| `RetryCount` | `int` | Number of failed dispatch attempts |
| `Error` | `nvarchar(max)?` | Last error message if dispatch failed |
| `IdempotencyKey` | `nvarchar(max)?` | SHA-256(type+content) — prevents duplicate dispatch on retry |

## Polling interval

The processor polls every `OutboxProcessor:IntervalSeconds` seconds (default 10). Tune via `appsettings.json`:

```json
"OutboxProcessor": {
  "IntervalSeconds": 10,
  "MaxRetries": 3,
  "BatchSize": 20
}
```

Lower values reduce latency but increase DB load. The tradeoff is explicit by design — do not reduce below 2 s without load-testing.

## Dead Letter Queue monitoring

Messages that exceed `MaxRetries` are moved to the `DeadLetterMessages` table and **stop being retried**. Without active monitoring, failed events can accumulate silently.

Recommended monitoring approaches (choose at least one):

1. **Health check** — query `COUNT(*) FROM DeadLetterMessages WHERE ReprocessedAt IS NULL` and expose it via `/health/ready`. A non-zero count can degrade the health status.
2. **Alert on log pattern** — `OutboxProcessorService` logs `LogError` when moving a message to DLQ. Configure your log aggregator (Seq, Grafana Loki, Application Insights) to alert on that pattern.
3. **Prometheus counter** — increment a `dlq_messages_total` counter from the processor and add a Grafana alert when the rate > 0.

To reprocess a DLQ message: set `DeadLetterMessage.ReprocessedAt = null` is not sufficient on its own — the original `OutboxMessage` still has `ProcessedAt` set (or is already in DLQ). The cleanest path is to re-insert the original event payload as a new `OutboxMessage` row.

## Consequences
- **Positive**: Event delivery is guaranteed even if the process restarts mid-dispatch.
- **Positive**: Entity change and event publication are atomic (same transaction).
- **Positive**: Domain layer stays free of infrastructure concerns — entities just call `RaiseDomainEvent()`.
- **Negative**: Events are delivered with eventual consistency (up to `IntervalSeconds` delay). For same-request consistency, synchronous handlers can be invoked directly via `IMediator.PublishAsync`.
- **Negative**: Adds operational complexity — the `OutboxMessages` table must be included in database maintenance and monitoring.
- **Negative**: DLQ messages fail silently unless actively monitored (see section above).
