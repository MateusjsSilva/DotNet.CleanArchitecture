using CleanArchitecture.Domain.Common;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Reflection;

namespace CleanArchitecture.Application.Common.Mediator;

/// <summary>
/// Custom mediator implementation that handles commands, queries, and domain events
/// through a composable pipeline of IPipelineBehavior decorators.
/// Behaviors are resolved per concrete request type so open-generic registrations
/// (e.g. LoggingBehavior&lt;,&gt;) are correctly instantiated by the DI container.
/// </summary>
internal sealed class Mediator(IServiceProvider serviceProvider) : IMediator
{
    // Caches MethodInfo per handler/pipeline type to avoid repeated reflection overhead.
    private static readonly ConcurrentDictionary<Type, MethodInfo> _methodCache = new();

    // ── Public API ────────────────────────────────────────────────────────────

    public Task<TResponse> SendAsync<TResponse>(
        IQuery<TResponse> query,
        CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResponse));
        var handler = serviceProvider.GetRequiredService(handlerType);

        Task<TResponse> HandlerDelegate() =>
            (Task<TResponse>)GetMethod(handlerType).Invoke(handler, [query, cancellationToken])!;

        return ExecutePipelineAsync(query.GetType(), typeof(TResponse), query, HandlerDelegate, cancellationToken);
    }

    public Task SendAsync(
        ICommand command,
        CancellationToken cancellationToken = default)
    {
        var concreteType = command.GetType();
        var handlerType = typeof(ICommandHandler<>).MakeGenericType(concreteType);
        var handler = serviceProvider.GetRequiredService(handlerType);

        // ICommand has no return value; wrap in bool so the pipeline stays uniform.
        async Task<bool> HandlerDelegate()
        {
            await (Task)GetMethod(handlerType).Invoke(handler, [command, cancellationToken])!;
            return true;
        }

        return ExecutePipelineAsync(concreteType, typeof(bool), command, HandlerDelegate, cancellationToken);
    }

    public Task<TResponse> SendAsync<TResponse>(
        ICommand<TResponse> command,
        CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResponse));
        var handler = serviceProvider.GetRequiredService(handlerType);

        Task<TResponse> HandlerDelegate() =>
            (Task<TResponse>)GetMethod(handlerType).Invoke(handler, [command, cancellationToken])!;

        return ExecutePipelineAsync(command.GetType(), typeof(TResponse), command, HandlerDelegate, cancellationToken);
    }

    public async Task PublishAsync(
        IDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
        var handlers = serviceProvider.GetServices(handlerType);
        var handleMethod = GetMethod(handlerType);

        // Domain events fan-out to all handlers in parallel — no pipeline by design.
        var tasks = handlers
            .Select(h => (Task)handleMethod.Invoke(h, [domainEvent, cancellationToken])!)
            .ToList();

        if (tasks.Count > 0)
            await Task.WhenAll(tasks);
    }

    // ── Pipeline execution ────────────────────────────────────────────────────

    /// <summary>
    /// Resolves <see cref="IPipelineBehavior{TRequest,TResponse}"/> using the *concrete*
    /// request type so that open-generic DI registrations are properly matched.
    /// Builds a Russian-doll chain: first-registered behavior is the outermost wrapper.
    /// </summary>
    private Task<TResponse> ExecutePipelineAsync<TResponse>(
        Type concreteRequestType,
        Type responseType,
        object request,
        Func<Task<TResponse>> handlerDelegate,
        CancellationToken cancellationToken)
    {
        var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(concreteRequestType, responseType);
        var behaviors = serviceProvider.GetServices(behaviorType).ToList();

        if (behaviors.Count == 0)
            return handlerDelegate();

        // Build from innermost outward so the first-registered behavior executes first.
        Func<Task<TResponse>> pipeline = handlerDelegate;

        for (var i = behaviors.Count - 1; i >= 0; i--)
        {
            var behavior = behaviors[i]!;
            var next = pipeline;
            var handleMethod = GetMethod(behaviorType);
            pipeline = () => (Task<TResponse>)handleMethod.Invoke(behavior, [request, next, cancellationToken])!;
        }

        return pipeline();
    }

    // ── Reflection cache ──────────────────────────────────────────────────────

    private static MethodInfo GetMethod(Type type) =>
        _methodCache.GetOrAdd(type, static t =>
            t.GetMethod("Handle") ?? throw new InvalidOperationException(
                $"Method 'Handle' not found on type '{t.FullName}'."));
}
