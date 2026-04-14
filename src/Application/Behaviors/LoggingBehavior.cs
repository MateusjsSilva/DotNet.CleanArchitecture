using CleanArchitecture.Application.Telemetry;
using CleanArchitecture.Application.Common.Mediator;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace CleanArchitecture.Application.Behaviors;

internal sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        Func<Task<TResponse>> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        using var activity = ApplicationActivitySource.Instance.StartActivity(requestName);
        activity?.SetTag("mediator.request", requestName);

        logger.LogInformation("Handling {RequestName}: {@Request}", requestName, request);

        try
        {
            var response = await next();

            activity?.SetStatus(ActivityStatusCode.Ok);
            logger.LogInformation("Handled {RequestName}", requestName);

            return response;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);
            throw;
        }
    }
}
