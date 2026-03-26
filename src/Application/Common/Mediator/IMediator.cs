using CleanArchitecture.Domain.Common;

namespace CleanArchitecture.Application.Common.Mediator;

/// <summary>
/// Defines the mediator interface for sending commands, queries, and publishing domain events.
/// </summary>
public interface IMediator
{
    /// <summary>
    /// Sends a query and returns the response.
    /// </summary>
    /// <typeparam name="TResponse">The type of response expected.</typeparam>
    /// <param name="query">The query to send.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response from the query handler.</returns>
    Task<TResponse> SendAsync<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a command that does not return a response.
    /// </summary>
    /// <param name="command">The command to send.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task SendAsync(ICommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a command and returns the response.
    /// </summary>
    /// <typeparam name="TResponse">The type of response expected.</typeparam>
    /// <param name="command">The command to send.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response from the command handler.</returns>
    Task<TResponse> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a domain event to all registered handlers.
    /// </summary>
    /// <param name="domainEvent">The domain event to publish.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}