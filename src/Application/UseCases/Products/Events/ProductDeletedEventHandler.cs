using CleanArchitecture.Domain.Events;
using CleanArchitecture.Application.UseCases.Products.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Application.UseCases.Products.Events;

internal sealed class ProductDeletedEventHandler(
    ILogger<ProductDeletedEventHandler> logger)
    : INotificationHandler<ProductDeletedEvent>
{
    public Task Handle(ProductDeletedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogWarning(
            "Product deleted: {ProductId} - {ProductName}",
            notification.ProductId,
            notification.ProductName);

        // Metrics collection only - cache invalidation is handled by CachingBehavior on the command
        ProductTelemetry.DeletedCounter.Add(1, new KeyValuePair<string, object?>("product_id", notification.ProductId));

        return Task.CompletedTask;
    }
}