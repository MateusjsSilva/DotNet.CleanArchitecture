using CleanArchitecture.Domain.Common;

namespace CleanArchitecture.Domain.Events;

public sealed record ProductDeletedEvent(
    Guid ProductId,
    string ProductName
) : IDomainEvent;