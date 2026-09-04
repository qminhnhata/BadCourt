using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace BadCourt.SharedKernel.Messaging;

/// <summary>
/// Finds the one handler that serves a request and calls it. The caller knows the type of the
/// request it is sending; this class does not, and has to arrive at the closed handler interface
/// from an instance alone.
/// </summary>
/// <remarks>
/// <para>
/// Handlers are resolved from the provider this instance was built with. Because the dispatcher is
/// registered as a scoped service, that provider is the scope the request arrived on, so a handler
/// and everything it depends on - a connection, a unit of work - belong to that request and to no
/// other. Resolving from the root provider instead would quietly share one handler, and one
/// connection, across every concurrent request in the process.
/// </para>
/// <para>
/// A request with no registered handler throws instead of returning a failed result. Nobody asked
/// for anything invalid: the composition root was assembled wrong, and that should surface at the
/// first dispatch rather than being reported to a user as a business failure.
/// </para>
/// </remarks>
internal sealed class Dispatcher(IServiceProvider provider) : ISender
{
    // Building a closed generic type and constructing it costs far more than the lookup that
    // replaces it here. Invokers close over nothing - the provider arrives as an argument on
    // every call - so one cache serves every container in the process.
    private static readonly ConcurrentDictionary<Type, CommandInvoker> CommandInvokers = new();

    private static readonly ConcurrentDictionary<(Type Invoker, Type Request, Type Response), object> ValueInvokers =
        new();

    /// <inheritdoc />
    public Task<Result> Send(ICommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        CommandInvoker invoker = CommandInvokers.GetOrAdd(
            command.GetType(),
            static request => (CommandInvoker)Create(typeof(CommandInvoker<>), request));

        return invoker.Invoke(provider, command, ct);
    }

    /// <inheritdoc />
    public Task<Result<TResponse>> Send<TResponse>(ICommand<TResponse> command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        return ResolveValueInvoker<TResponse>(typeof(CommandInvoker<,>), command).Invoke(provider, command, ct);
    }

    /// <inheritdoc />
    public Task<Result<TResponse>> Send<TResponse>(IQuery<TResponse> query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);

        return ResolveValueInvoker<TResponse>(typeof(QueryInvoker<,>), query).Invoke(provider, query, ct);
    }

    /// <summary>
    /// The response type is a type argument the caller supplied, but the request type is only
    /// available from the instance, so the pair identifies the invoker.
    /// </summary>
    private static ValueInvoker<TResponse> ResolveValueInvoker<TResponse>(Type definition, object request)
    {
        object invoker = ValueInvokers.GetOrAdd(
            (definition, request.GetType(), typeof(TResponse)),
            static key => Create(key.Invoker, key.Request, key.Response));

        return (ValueInvoker<TResponse>)invoker;
    }

    private static object Create(Type definition, params Type[] arguments) =>
        Activator.CreateInstance(definition.MakeGenericType(arguments))!;

    /// <summary>
    /// Lets the dispatcher hold an invoker whose request type it cannot name, and lets the
    /// subclass do the cast that reflection would otherwise have to repeat on every call.
    /// </summary>
    private abstract class CommandInvoker
    {
        public abstract Task<Result> Invoke(IServiceProvider provider, ICommand command, CancellationToken ct);
    }

    private sealed class CommandInvoker<TCommand> : CommandInvoker
        where TCommand : ICommand
    {
        public override Task<Result> Invoke(IServiceProvider provider, ICommand command, CancellationToken ct) =>
            provider.GetRequiredService<ICommandHandler<TCommand>>().Handle((TCommand)command, ct);
    }

    /// <summary>
    /// The counterpart for requests that carry a value back. The response type is known statically
    /// at the call site, so only the request type has to be erased.
    /// </summary>
    private abstract class ValueInvoker<TResponse>
    {
        public abstract Task<Result<TResponse>> Invoke(
            IServiceProvider provider,
            object request,
            CancellationToken ct);
    }

    private sealed class CommandInvoker<TCommand, TResponse> : ValueInvoker<TResponse>
        where TCommand : ICommand<TResponse>
    {
        public override Task<Result<TResponse>> Invoke(
            IServiceProvider provider,
            object request,
            CancellationToken ct) =>
            provider.GetRequiredService<ICommandHandler<TCommand, TResponse>>().Handle((TCommand)request, ct);
    }

    private sealed class QueryInvoker<TQuery, TResponse> : ValueInvoker<TResponse>
        where TQuery : IQuery<TResponse>
    {
        public override Task<Result<TResponse>> Invoke(
            IServiceProvider provider,
            object request,
            CancellationToken ct) =>
            provider.GetRequiredService<IQueryHandler<TQuery, TResponse>>().Handle((TQuery)request, ct);
    }
}
