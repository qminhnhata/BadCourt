using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BadCourt.SharedKernel.Messaging;

/// <summary>
/// Builds the request pipeline: the dispatcher, and every handler discovered in the assemblies
/// handed to it.
/// </summary>
public static class MessagingServiceCollectionExtensions
{
    private static readonly Type[] HandlerInterfaces =
    [
        typeof(ICommandHandler<>),
        typeof(ICommandHandler<,>),
        typeof(IQueryHandler<,>),
    ];

    /// <summary>
    /// Registers <see cref="ISender"/> and scans <paramref name="assemblies"/> for handlers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Handlers are scoped, so one request gets one handler instance, and a module's handlers can
    /// be internal: a handler is called through the dispatcher and never named by a caller, so
    /// nothing outside its own assembly has any business referring to it.
    /// </para>
    /// <para>
    /// Only the handler interfaces are registered, not every interface a handler happens to
    /// implement. A handler that also implements <see cref="IDisposable"/> should not thereby
    /// become the answer to a request for an <see cref="IDisposable"/>.
    /// </para>
    /// </remarks>
    /// <param name="services">The collection being built.</param>
    /// <param name="assemblies">The assemblies to search for handlers.</param>
    /// <returns>The same collection, so calls can be chained.</returns>
    public static IServiceCollection AddMessaging(this IServiceCollection services, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);

        if (assemblies.Length == 0)
        {
            throw new ArgumentException(
                "At least one assembly is needed; a pipeline with no handlers can serve no request.",
                nameof(assemblies));
        }

        services.Scan(scan => scan
            .FromAssemblies(assemblies)
            .AddClasses(types => types.AssignableTo(typeof(ICommandHandler<>)), publicOnly: false)
                .AsImplementedInterfaces(IsHandlerInterface)
                .WithScopedLifetime()
            .AddClasses(types => types.AssignableTo(typeof(ICommandHandler<,>)), publicOnly: false)
                .AsImplementedInterfaces(IsHandlerInterface)
                .WithScopedLifetime()
            .AddClasses(types => types.AssignableTo(typeof(IQueryHandler<,>)), publicOnly: false)
                .AsImplementedInterfaces(IsHandlerInterface)
                .WithScopedLifetime());

        // Modules each call AddMessaging for their own assembly; the dispatcher they share is
        // registered by whichever calls first.
        services.TryAddScoped<ISender, Dispatcher>();

        return services;
    }

    private static bool IsHandlerInterface(Type type) =>
        type.IsGenericType && Array.IndexOf(HandlerInterfaces, type.GetGenericTypeDefinition()) >= 0;
}
