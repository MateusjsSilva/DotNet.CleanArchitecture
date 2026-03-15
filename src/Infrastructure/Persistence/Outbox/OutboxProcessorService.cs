using CleanArchitecture.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CleanArchitecture.Infrastructure.Persistence.Outbox;

/// <summary>
/// Background service that polls the OutboxMessages table and dispatches
/// pending domain events via MediatR. Runs every 10 seconds.
/// </summary>
internal sealed class OutboxProcessorService(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxProcessorService> logger)
    : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox processor started.");

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
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        var messages = await dbContext.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.Error == null)
            .OrderBy(m => m.OccurredAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0) return;

        logger.LogDebug("Processing {Count} outbox message(s).", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                var type = Type.GetType(message.Type);

                if (type is null)
                {
                    message.Error = $"Cannot resolve type '{message.Type}'.";
                    logger.LogWarning("Cannot resolve outbox event type: {Type}", message.Type);
                    continue;
                }

                var domainEvent = (IDomainEvent)JsonSerializer.Deserialize(message.Content, type)!;
                await publisher.Publish(domainEvent, cancellationToken);

                message.ProcessedAt = DateTime.UtcNow;

                logger.LogDebug("Outbox message {Id} processed.", message.Id);
            }
            catch (Exception ex)
            {
                message.Error = ex.Message;
                logger.LogError(ex, "Failed to process outbox message {Id}.", message.Id);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
