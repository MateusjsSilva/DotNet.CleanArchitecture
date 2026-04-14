namespace CleanArchitecture.Infrastructure.Persistence.Outbox;

/// <summary>
/// Persists domain events to the database so they can be dispatched
/// reliably by the OutboxProcessorService even if the process restarts.
///
/// Includes versioning for safe event schema evolution and idempotent processing.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    /// <summary>Assembly-qualified type name used to deserialize the event.</summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>JSON-serialized domain event payload.</summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>Event schema version for migration and versioning support.</summary>
    public int EventVersion { get; init; } = 1;

    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    /// <summary>Timestamp when the message was successfully processed.</summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>Number of retry attempts (incremented each time processing fails).</summary>
    public int RetryCount { get; set; }

    /// <summary>Last error encountered during processing (null if success).</summary>
    public string? Error { get; set; }

    /// <summary>
    /// Idempotency key to prevent duplicate processing if the same event is published multiple times.
    /// Derived from the aggregate ID + event type + occurred timestamp.
    /// </summary>
    public string? IdempotencyKey { get; init; }
}
