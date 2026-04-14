using CleanArchitecture.Domain.Common;

namespace CleanArchitecture.Domain.Events;

public sealed record ProductUpdatedEvent(
    Guid ProductId,
    string ProductName,
    decimal NewPrice,
    string? NewDescription
) : IDomainEvent;