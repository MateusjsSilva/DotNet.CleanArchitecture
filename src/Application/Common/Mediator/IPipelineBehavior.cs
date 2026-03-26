namespace CleanArchitecture.Application.Common.Mediator;

/// <summary>
/// Defines a pipeline behavior that can wrap handlers with cross-cutting concerns.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of response.</typeparam>
public interface IPipelineBehavior<in TRequest, TResponse>
{
    /// <summary>
    /// Pipeline handler. Performs any additional behavior before and after the main handler.
    /// </summary>
    /// <param name="request">The request object.</param>
    /// <param name="next">The next behavior in the pipeline or the actual handler.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response.</returns>
    Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken);
}