using CleanArchitecture.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics.Metrics;

namespace CleanArchitecture.Application.UseCases.Products.Events;

internal sealed class ProductPatchedEventHandler(
    ILogger<ProductPatchedEventHandler> logger)
    : INotificationHandler<ProductPatchedEvent>
{
    private static readonly Meter Meter = new("CleanArchitecture.Products", "1.0.0");
    private static readonly Counter<int> ProductsPatchedCounter = Meter.CreateCounter<int>("products.patched.count");

    public Task Handle(ProductPatchedEvent notification, CancellationToken cancellationToken)
    {
        var changedFieldsInfo = string.Join(", ",
            notification.ChangedFields.Select(kvp => $"{kvp.Key}={kvp.Value}"));

        logger.LogInformation(
            "Product patched: {ProductId} - {ProductName} - Changed: {ChangedFields}",
            notification.ProductId,
            notification.ProductName,
            changedFieldsInfo);

        // Metrics collection only - cache invalidation is handled by CachingBehavior on the command
        RecordMetrics(notification);

        return Task.CompletedTask;
    }

    private static void RecordMetrics(ProductPatchedEvent notification)
    {
        ProductsPatchedCounter.Add(1,
            new KeyValuePair<string, object?>("product_id", notification.ProductId),
            new KeyValuePair<string, object?>("fields_count", notification.ChangedFields.Count));
    }
}