using CleanArchitecture.Domain.Common;

namespace CleanArchitecture.Domain.Events;

public sealed record ProductPatchedEvent(
    Guid ProductId,
    string ProductName,
    IDictionary<string, object?> ChangedFields
) : IDomainEvent;