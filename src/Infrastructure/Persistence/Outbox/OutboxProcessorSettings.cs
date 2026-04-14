namespace CleanArchitecture.Infrastructure.Persistence.Outbox;

public sealed class OutboxProcessorSettings
{
    public const string SectionName = "OutboxProcessor";

    /// <summary>How often the processor polls for pending outbox messages.</summary>
    public int IntervalSeconds { get; init; } = 10;

    /// <summary>Maximum dispatch retries before a message is moved to the Dead Letter Queue.</summary>
    public int MaxRetries { get; init; } = 3;

    /// <summary>Number of messages processed per polling cycle.</summary>
    public int BatchSize { get; init; } = 20;
}
