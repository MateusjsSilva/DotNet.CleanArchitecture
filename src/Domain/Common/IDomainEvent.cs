using MediatR;

namespace CleanArchitecture.Domain.Common;

/// <summary>
/// Marker interface for domain events.
/// Implements INotification so events can be dispatched via MediatR.
/// </summary>
public interface IDomainEvent : INotification;
