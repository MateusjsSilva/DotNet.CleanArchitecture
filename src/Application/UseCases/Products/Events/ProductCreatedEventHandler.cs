using CleanArchitecture.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Application.UseCases.Products.Events;

/// <summary>
/// Handles the <see cref="ProductCreatedEvent"/> domain event.
///
/// This handler is invoked by the <c>OutboxProcessorService</c> background worker after the
/// event has been reliably persisted to the outbox table. Add side effects here.
///
/// Common extension points:
///
///   1. Publish an integration event to a message broker (e.g. RabbitMQ / Azure Service Bus):
///      <code>
///      await integrationEventPublisher.PublishAsync(
///          new ProductCreatedIntegrationEvent(notification.ProductId, notification.ProductName),
///          cancellationToken);
///      </code>
///
///   2. Update a read model / projection (CQRS read side):
///      <code>
///      await readModelRepository.UpsertProductSummaryAsync(
///          notification.ProductId, notification.ProductName, cancellationToken);
///      </code>
///
///   3. Send a notification (e.g. e-mail, push, webhook):
///      <code>
///      await notificationService.SendAsync(
///          $"New product available: {notification.ProductName}", cancellationToken);
///      </code>
///
/// To add any of the above, inject the relevant service via the primary constructor
/// and replace the placeholder log statement below.
/// </summary>
internal sealed class ProductCreatedEventHandler(
    ILogger<ProductCreatedEventHandler> logger)
    : INotificationHandler<ProductCreatedEvent>
{
    public Task Handle(ProductCreatedEvent notification, CancellationToken cancellationToken)
    {
        // Replace or extend this with real side effects (see XML doc above).
        logger.LogInformation(
            "Product created: {ProductId} — {ProductName}",
            notification.ProductId,
            notification.ProductName);

        return Task.CompletedTask;
    }
}
