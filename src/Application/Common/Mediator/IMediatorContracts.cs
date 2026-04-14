using CleanArchitecture.Domain.Common;

namespace CleanArchitecture.Application.Common.Mediator;

/// <summary>
/// Marker interface for queries that return a result of type <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the query.</typeparam>
public interface IQuery<out TResponse>
{
}

/// <summary>
/// Marker interface for commands that do not return a result.
/// </summary>
public interface ICommand
{
}

/// <summary>
/// Marker interface for commands that return a result of type <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the command.</typeparam>
public interface ICommand<out TResponse>
{
}

/// <summary>
/// Defines a handler for a query of type <typeparamref name="TQuery"/>.
/// </summary>
/// <typeparam name="TQuery">The type of query being handled.</typeparam>
/// <typeparam name="TResponse">The type of response from the handler.</typeparam>
public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<TResponse> Handle(TQuery query, CancellationToken cancellationToken);
}

/// <summary>
/// Defines a handler for a command of type <typeparamref name="TCommand"/>.
/// </summary>
/// <typeparam name="TCommand">The type of command being handled.</typeparam>
public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    Task Handle(TCommand command, CancellationToken cancellationToken);
}

/// <summary>
/// Defines a handler for a command of type <typeparamref name="TCommand"/> that returns a response.
/// </summary>
/// <typeparam name="TCommand">The type of command being handled.</typeparam>
/// <typeparam name="TResponse">The type of response from the handler.</typeparam>
public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<TResponse> Handle(TCommand command, CancellationToken cancellationToken);
}

/// <summary>
/// Defines a handler for a domain event of type <typeparamref name="TEvent"/>.
/// </summary>
/// <typeparam name="TEvent">The type of domain event being handled.</typeparam>
public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    Task Handle(TEvent domainEvent, CancellationToken cancellationToken);
}