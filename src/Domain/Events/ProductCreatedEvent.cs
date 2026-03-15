using CleanArchitecture.Domain.Common;

namespace CleanArchitecture.Domain.Events;

public sealed record ProductCreatedEvent(Guid ProductId, string ProductName) : IDomainEvent;
