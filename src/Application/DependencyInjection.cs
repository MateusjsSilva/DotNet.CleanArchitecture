using CleanArchitecture.Application.Behaviors;
using CleanArchitecture.Application.Common.Mediator;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CleanArchitecture.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Register custom mediator
        services.AddScoped<IMediator, Mediator>();

        // Register all handlers
        RegisterHandlers(services, assembly);

        // Pipeline behaviors — executed in registration order (first registered = outermost wrapper).
        // Logging → Validation → Caching → Handler
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));

        // Register validators
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }

    private static readonly Type[] HandlerInterfaces =
    [
        typeof(IQueryHandler<,>),
        typeof(ICommandHandler<,>),
        typeof(ICommandHandler<>),
        typeof(IDomainEventHandler<>),
    ];

    private static void RegisterHandlers(IServiceCollection services, Assembly assembly)
    {
        // Single reflection pass over all types — registers every handler interface variant.
        foreach (var type in assembly.GetTypes())
        {
            foreach (var @interface in type.GetInterfaces())
            {
                if (!@interface.IsGenericType) continue;

                var definition = @interface.GetGenericTypeDefinition();
                if (Array.IndexOf(HandlerInterfaces, definition) < 0) continue;

                services.AddScoped(@interface, type);
            }
        }
    }
}
