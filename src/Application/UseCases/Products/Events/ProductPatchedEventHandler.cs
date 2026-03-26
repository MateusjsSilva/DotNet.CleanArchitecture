using CleanArchitecture.Domain.Events;
using CleanArchitecture.Application.UseCases.Products.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Application.UseCases.Products.Events;

internal sealed class ProductPatchedEventHandler(
    ILogger<ProductPatchedEventHandler> logger)
    : INotificationHandler<ProductPatchedEvent>
{
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
        ProductTelemetry.PatchedCounter.Add(1,
            new KeyValuePair<string, object?>("product_id", notification.ProductId),
            new KeyValuePair<string, object?>("fields_count", notification.ChangedFields.Count));

        return Task.CompletedTask;
    }
}