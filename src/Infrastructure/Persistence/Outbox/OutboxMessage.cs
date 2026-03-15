namespace CleanArchitecture.Infrastructure.Persistence.Outbox;

/// <summary>
/// Persists domain events to the database so they can be dispatched
/// reliably by the OutboxProcessorService even if the process restarts.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    /// <summary>Assembly-qualified type name used to deserialize the event.</summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>JSON-serialized domain event payload.</summary>
    public string Content { get; init; } = string.Empty;

    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public string? Error { get; set; }
}
