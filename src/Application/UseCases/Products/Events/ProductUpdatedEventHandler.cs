using CleanArchitecture.Domain.Events;
using CleanArchitecture.Application.UseCases.Products.Common;
using CleanArchitecture.Application.Common.Mediator;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Application.UseCases.Products.Events;

internal sealed class ProductUpdatedEventHandler(
    ILogger<ProductUpdatedEventHandler> logger)
    : IDomainEventHandler<ProductUpdatedEvent>
{
    public Task Handle(ProductUpdatedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Product updated: {ProductId} - {ProductName} - Price: {NewPrice}",
            notification.ProductId,
            notification.ProductName,
            notification.NewPrice);

        // Metrics collection only - cache invalidation is handled by CachingBehavior on the command
        ProductTelemetry.UpdatedCounter.Add(1, new KeyValuePair<string, object?>("product_id", notification.ProductId));

        return Task.CompletedTask;
    }
}