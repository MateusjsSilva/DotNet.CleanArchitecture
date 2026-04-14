namespace CleanArchitecture.Infrastructure.Persistence.Outbox;

/// <summary>
/// Stores outbox messages that failed after max retry attempts.
/// These can be manually reprocessed or analyzed for debugging.
/// </summary>
public sealed class DeadLetterMessage
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    /// <summary>Reference to the original OutboxMessage that failed.</summary>
    public Guid OutboxMessageId { get; init; }

    /// <summary>Assembly-qualified type name of the domain event.</summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>JSON-serialized domain event payload.</summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>Event version (for versioning/migration).</summary>
    public int EventVersion { get; init; } = 1;

    /// <summary>Last error message that caused the message to be moved to DLQ.</summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>Number of times this message was retried before being moved to DLQ.</summary>
    public int RetryCount { get; set; }

    /// <summary>Timestamp when the message failed and was moved to DLQ.</summary>
    public DateTime FailedAt { get; init; }

    /// <summary>Timestamp when the message was successfully reprocessed (if applicable).</summary>
    public DateTime? ReprocessedAt { get; set; }

    public OutboxMessage? OutboxMessage { get; set; }
}
