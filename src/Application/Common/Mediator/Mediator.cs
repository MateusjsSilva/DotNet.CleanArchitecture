using CleanArchitecture.Domain.Common;
using Microsoft.Extensions.DependencyInjection;

namespace CleanArchitecture.Application.Common.Mediator;

/// <summary>
/// Custom mediator implementation that handles commands, queries, and domain events.
/// Simplified version to replace MediatR dependency.
/// </summary>
internal sealed class Mediator(IServiceProvider serviceProvider) : IMediator
{
    public async Task<TResponse> SendAsync<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResponse));
        var handler = serviceProvider.GetRequiredService(handlerType);

        var handleMethod = handlerType.GetMethod("Handle")!;
        var result = handleMethod.Invoke(handler, [query, cancellationToken]);

        if (result is Task<TResponse> task)
        {
            return await task;
        }

        return (TResponse)result!;
    }

    public async Task SendAsync(ICommand command, CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(ICommandHandler<>).MakeGenericType(command.GetType());
        var handler = serviceProvider.GetRequiredService(handlerType);

        var handleMethod = handlerType.GetMethod("Handle")!;
        var result = handleMethod.Invoke(handler, [command, cancellationToken]);

        if (result is Task task)
        {
            await task;
        }
    }

    public async Task<TResponse> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResponse));
        var handler = serviceProvider.GetRequiredService(handlerType);

        var handleMethod = handlerType.GetMethod("Handle")!;
        var result = handleMethod.Invoke(handler, [command, cancellationToken]);

        if (result is Task<TResponse> task)
        {
            return await task;
        }

        return (TResponse)result!;
    }

    public async Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
        var handlers = serviceProvider.GetServices(handlerType);

        var tasks = new List<Task>();

        foreach (var handler in handlers)
        {
            var handleMethod = handlerType.GetMethod("Handle")!;
            if (handleMethod.Invoke(handler, [domainEvent, cancellationToken]) is Task task)
            {
                tasks.Add(task);
            }
        }

        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks);
        }
    }
}