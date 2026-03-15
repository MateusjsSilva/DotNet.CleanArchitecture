using CleanArchitecture.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Application.UseCases.Products.Events;

/// <summary>
/// Example domain event handler. Receives ProductCreatedEvent after it is
/// dispatched (synchronously or via Outbox) and performs any side effects,
/// such as sending notifications, invalidating caches, or publishing
/// integration events to external systems.
/// </summary>
internal sealed class ProductCreatedEventHandler(
    ILogger<ProductCreatedEventHandler> logger)
    : INotificationHandler<ProductCreatedEvent>
{
    public Task Handle(ProductCreatedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Product created: {ProductId} — {ProductName}",
            notification.ProductId,
            notification.ProductName);

        // TODO: send integration event, update read model, etc.

        return Task.CompletedTask;
    }
}
