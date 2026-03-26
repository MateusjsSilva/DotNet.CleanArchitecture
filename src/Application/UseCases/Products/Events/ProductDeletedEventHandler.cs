using CleanArchitecture.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics.Metrics;

namespace CleanArchitecture.Application.UseCases.Products.Events;

internal sealed class ProductDeletedEventHandler(
    ILogger<ProductDeletedEventHandler> logger)
    : INotificationHandler<ProductDeletedEvent>
{
    private static readonly Meter Meter = new("CleanArchitecture.Products", "1.0.0");
    private static readonly Counter<int> ProductsDeletedCounter = Meter.CreateCounter<int>("products.deleted.count");

    public Task Handle(ProductDeletedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogWarning(
            "Product deleted: {ProductId} - {ProductName}",
            notification.ProductId,
            notification.ProductName);

        // Metrics collection only - cache invalidation is handled by CachingBehavior on the command
        RecordMetrics(notification);

        return Task.CompletedTask;
    }

    private static void RecordMetrics(ProductDeletedEvent notification)
    {
        ProductsDeletedCounter.Add(1, new KeyValuePair<string, object?>("product_id", notification.ProductId));
    }
}