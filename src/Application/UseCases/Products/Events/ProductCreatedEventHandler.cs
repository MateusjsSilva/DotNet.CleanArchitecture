using CleanArchitecture.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics.Metrics;

namespace CleanArchitecture.Application.UseCases.Products.Events;

internal sealed class ProductCreatedEventHandler(
    ILogger<ProductCreatedEventHandler> logger)
    : INotificationHandler<ProductCreatedEvent>
{
    private static readonly Meter Meter = new("CleanArchitecture.Products", "1.0.0");
    private static readonly Counter<int> ProductsCreatedCounter = Meter.CreateCounter<int>("products.created.count");

    public Task Handle(ProductCreatedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Product created: {ProductId} - {ProductName}",
            notification.ProductId,
            notification.ProductName);

        // Metrics collection only - cache invalidation is handled by CachingBehavior on the command
        RecordMetrics(notification);

        return Task.CompletedTask;
    }

    private static void RecordMetrics(ProductCreatedEvent notification)
    {
        ProductsCreatedCounter.Add(1, new KeyValuePair<string, object?>("product_id", notification.ProductId));
    }
}
