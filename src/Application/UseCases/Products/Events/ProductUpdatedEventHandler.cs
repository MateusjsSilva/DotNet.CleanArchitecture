using CleanArchitecture.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics.Metrics;

namespace CleanArchitecture.Application.UseCases.Products.Events;

internal sealed class ProductUpdatedEventHandler(
    ILogger<ProductUpdatedEventHandler> logger)
    : INotificationHandler<ProductUpdatedEvent>
{
    private static readonly Meter Meter = new("CleanArchitecture.Products", "1.0.0");
    private static readonly Counter<int> ProductsUpdatedCounter = Meter.CreateCounter<int>("products.updated.count");

    public Task Handle(ProductUpdatedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Product updated: {ProductId} - {ProductName} - Price: {NewPrice}",
            notification.ProductId,
            notification.ProductName,
            notification.NewPrice);

        // Metrics collection only - cache invalidation is handled by CachingBehavior on the command
        RecordMetrics(notification);

        return Task.CompletedTask;
    }

    private static void RecordMetrics(ProductUpdatedEvent notification)
    {
        ProductsUpdatedCounter.Add(1, new KeyValuePair<string, object?>("product_id", notification.ProductId));
    }
}