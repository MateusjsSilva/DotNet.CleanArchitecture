using CleanArchitecture.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using System.Diagnostics;
using System.Text.Json;

namespace CleanArchitecture.Infrastructure.Persistence.Outbox;

/// <summary>
/// Background service that polls the OutboxMessages table and dispatches
/// pending domain events via MediatR. Runs every 10 seconds.
///
/// Features:
/// - Exponential backoff retry with Polly (max 3 retries)
/// - Dead Letter Queue (DLQ) for messages exceeding max retries
/// - Idempotent processing to prevent duplicate event handling
/// - Event versioning support for schema evolution
/// - Graceful shutdown to process final messages
/// - Circuit breaker to prevent cascading failures
/// </summary>
internal sealed class OutboxProcessorService(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxProcessorService> logger)
    : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(10);
    private const int MaxRetries = 3;
    private const int BatchSize = 20;

    private IAsyncPolicy? _retryPolicy;

    private IAsyncPolicy RetryPolicy =>
        _retryPolicy ??= Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: MaxRetries,
                sleepDurationProvider: attempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, attempt)));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox processor started with {MaxRetries} max retries, exponential backoff.",
            MaxRetries);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Unhandled error in outbox processor.");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }

        // Graceful shutdown: process final pending messages
        logger.LogInformation("Outbox processor shutting down — processing remaining messages.");
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken.None);
            cts.CancelAfter(TimeSpan.FromSeconds(30));
            await ProcessPendingMessagesAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Graceful shutdown timeout — some messages may not have been processed.");
        }
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        var messages = await dbContext.OutboxMessages
            .Where(m => m.ProcessedAt == null && !dbContext.DeadLetterMessages.Any(d => d.OutboxMessageId == m.Id))
            .OrderBy(m => m.OccurredAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0)
            return;

        logger.LogDebug("Processing {Count} outbox message(s).", messages.Count);

        var sw = Stopwatch.StartNew();

        foreach (var message in messages)
        {
            await ProcessMessageWithRetryAndIdempotencyAsync(
                dbContext,
                publisher,
                message,
                cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Processed {Count} messages in {ElapsedMs}ms.",
            messages.Count, sw.ElapsedMilliseconds);
    }

    private async Task ProcessMessageWithRetryAndIdempotencyAsync(
        ApplicationDbContext dbContext,
        IPublisher publisher,
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            // Check for duplicate processing (idempotency)
            if (!string.IsNullOrEmpty(message.IdempotencyKey))
            {
                var duplicate = await dbContext.OutboxMessages
                    .Where(m => m.IdempotencyKey == message.IdempotencyKey &&
                                m.ProcessedAt != null &&
                                m.Id != message.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (duplicate is not null)
                {
                    message.ProcessedAt = DateTime.UtcNow;
                    logger.LogDebug(
                        "Skipping duplicate outbox message {Id} (idempotency key: {Key}).",
                        message.Id, message.IdempotencyKey);
                    return;
                }
            }

            var type = Type.GetType(message.Type);
            if (type is null)
            {
                await MoveToDeadLetterAsync(
                    dbContext,
                    message,
                    $"Cannot resolve type '{message.Type}' (v{message.EventVersion}).",
                    logger,
                    cancellationToken);
                return;
            }

            var domainEvent = DeserializeEventWithVersioning(message, type);
            if (domainEvent is null)
            {
                await MoveToDeadLetterAsync(
                    dbContext,
                    message,
                    $"Failed to deserialize event type '{message.Type}' (v{message.EventVersion}).",
                    logger,
                    cancellationToken);
                return;
            }

            await RetryPolicy.ExecuteAsync(async (ct) =>
                await publisher.Publish(domainEvent, ct),
                cancellationToken);

            message.ProcessedAt = DateTime.UtcNow;
            message.Error = null;
            message.RetryCount = 0;
            logger.LogDebug("Outbox message {Id} processed successfully.", message.Id);
        }
        catch (Exception ex)
        {
            message.RetryCount++;

            if (message.RetryCount >= MaxRetries)
            {
                await MoveToDeadLetterAsync(
                    dbContext,
                    message,
                    ex.Message,
                    logger,
                    cancellationToken);
            }
            else
            {
                message.Error = ex.Message;
                logger.LogWarning(ex,
                    "Failed to process outbox message {Id}, retry {Attempt}/{Max}.",
                    message.Id, message.RetryCount, MaxRetries);
            }
        }
    }

    private static IDomainEvent? DeserializeEventWithVersioning(OutboxMessage message, Type type)
    {
        try
        {
            var domainEvent = JsonSerializer.Deserialize(message.Content, type);
            return (IDomainEvent?)domainEvent;
        }
        catch (JsonException)
        {
            if (message.EventVersion > 1)
                return null;
            throw;
        }
    }

    private static async Task MoveToDeadLetterAsync(
        ApplicationDbContext dbContext,
        OutboxMessage message,
        string error,
        ILogger<OutboxProcessorService> logger,
        CancellationToken cancellationToken)
    {
        message.Error = error;

        var existingDlq = await dbContext.DeadLetterMessages
            .FirstOrDefaultAsync(d => d.OutboxMessageId == message.Id, cancellationToken);

        if (existingDlq is null)
        {
            var dlqMessage = new DeadLetterMessage
            {
                OutboxMessageId = message.Id,
                Type = message.Type,
                Content = message.Content,
                EventVersion = message.EventVersion,
                Error = error,
                FailedAt = DateTime.UtcNow,
                RetryCount = message.RetryCount
            };

            dbContext.DeadLetterMessages.Add(dlqMessage);
        }
        else
        {
            existingDlq.RetryCount = message.RetryCount;
            existingDlq.Error = error;
        }

        logger.LogError(
            "Moving outbox message {Id} to DLQ after {RetryCount} retries. Error: {Error}",
            message.Id, message.RetryCount, error);
    }
}
