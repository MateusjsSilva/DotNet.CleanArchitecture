using CleanArchitecture.Domain.Common;

namespace CleanArchitecture.Domain.Exceptions;

public sealed class ConcurrencyException : DomainException
{
    public ConcurrencyException(string entityName, Guid entityId)
        : base($"The {entityName} with ID {entityId} was modified by another user. Please refresh and try again.")
    {
        EntityName = entityName;
        EntityId = entityId;
    }

    public string EntityName { get; }
    public Guid EntityId { get; }
}