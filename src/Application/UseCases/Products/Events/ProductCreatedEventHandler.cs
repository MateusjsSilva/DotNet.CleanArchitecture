using CleanArchitecture.Domain.Events;
using CleanArchitecture.Application.UseCases.Products.Common;
using CleanArchitecture.Application.Common.Mediator;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Application.UseCases.Products.Events;

internal sealed class ProductCreatedEventHandler(
    ILogger<ProductCreatedEventHandler> logger)
    : IDomainEventHandler<ProductCreatedEvent>
{
    public Task Handle(ProductCreatedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Product created: {ProductId} - {ProductName}",
            notification.ProductId,
            notification.ProductName);

        // Metrics collection only - cache invalidation is handled by CachingBehavior on the command
        ProductTelemetry.CreatedCounter.Add(1, new KeyValuePair<string, object?>("product_id", notification.ProductId));

        return Task.CompletedTask;
    }
}
