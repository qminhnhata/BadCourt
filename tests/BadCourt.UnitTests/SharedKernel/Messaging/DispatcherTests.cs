using BadCourt.SharedKernel;
using BadCourt.SharedKernel.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace BadCourt.UnitTests.SharedKernel.Messaging;

/// <summary>
/// These run against the container <see cref="MessagingServiceCollectionExtensions.AddMessaging"/>
/// actually builds. A dispatcher assembled by hand in a test would prove that a hand-assembled
/// dispatcher works; what has to hold is that scanning finds the handlers, that the registrations
/// have the lifetimes claimed for them, and that a request arrives at exactly one handler.
/// </summary>
public class DispatcherTests
{
    /// <summary>
    /// Scope validation is on because the mistake this class exists to avoid - resolving a handler
    /// from the root provider - is invisible in a test that never opens a scope.
    /// </summary>
    private static ServiceProvider BuildContainer() =>
        new ServiceCollection()
            .AddSingleton<DispatchLog>()
            .AddMessaging(typeof(DispatcherTests).Assembly)
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });

    [Fact]
    public async Task A_command_reaches_its_handler()
    {
        using ServiceProvider provider = BuildContainer();
        using IServiceScope scope = provider.CreateScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

        Result result = await sender.Send(new Ping(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        scope.ServiceProvider.GetRequiredService<DispatchLog>().Calls.ShouldBe(["ping"]);
    }

    [Fact]
    public async Task A_command_carries_its_value_back()
    {
        using ServiceProvider provider = BuildContainer();
        using IServiceScope scope = provider.CreateScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

        Result<Guid> result = await sender.Send(new Register("Centre"), TestContext.Current.CancellationToken);

        result.Value.ShouldBe(RegisterHandler.Registered);
        scope.ServiceProvider.GetRequiredService<DispatchLog>().Calls.ShouldBe(["register Centre"]);
    }

    [Fact]
    public async Task A_query_reaches_its_handler()
    {
        using ServiceProvider provider = BuildContainer();
        using IServiceScope scope = provider.CreateScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

        Result<string> result = await sender.Send(new GetCourt(7), TestContext.Current.CancellationToken);

        result.Value.ShouldBe("court 7");
        scope.ServiceProvider.GetRequiredService<DispatchLog>().Calls.ShouldBe(["get 7"]);
    }

    [Fact]
    public async Task A_handlers_failure_comes_back_as_a_result()
    {
        using ServiceProvider provider = BuildContainer();
        using IServiceScope scope = provider.CreateScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

        Result result = await sender.Send(new Refuse(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(RefuseHandler.Refused);
    }

    [Fact]
    public async Task The_callers_cancellation_token_reaches_the_handler()
    {
        using ServiceProvider provider = BuildContainer();
        using IServiceScope scope = provider.CreateScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();
        using var cts = new CancellationTokenSource();

        await sender.Send(new Ping(), cts.Token);

        scope.ServiceProvider.GetRequiredService<DispatchLog>().Token.ShouldBe(cts.Token);
    }

    /// <summary>
    /// The handler that ran must be the one this scope owns. Were the dispatcher holding the root
    /// provider, it would build a second handler - and, in a running application, a second
    /// connection - unrelated to the request in flight.
    /// </summary>
    [Fact]
    public async Task The_handler_comes_from_the_calling_scope()
    {
        using ServiceProvider provider = BuildContainer();
        using IServiceScope scope = provider.CreateScope();
        ICommandHandler<Ping> expected = scope.ServiceProvider.GetRequiredService<ICommandHandler<Ping>>();

        await scope.ServiceProvider.GetRequiredService<ISender>().Send(new Ping(), TestContext.Current.CancellationToken);

        scope.ServiceProvider.GetRequiredService<DispatchLog>().Handlers.Single().ShouldBeSameAs(expected);
    }

    [Fact]
    public void Each_scope_gets_its_own_handler()
    {
        using ServiceProvider provider = BuildContainer();
        using IServiceScope first = provider.CreateScope();
        using IServiceScope second = provider.CreateScope();

        ICommandHandler<Ping> one = first.ServiceProvider.GetRequiredService<ICommandHandler<Ping>>();
        ICommandHandler<Ping> two = second.ServiceProvider.GetRequiredService<ICommandHandler<Ping>>();

        one.ShouldNotBeSameAs(two);
        first.ServiceProvider.GetRequiredService<ICommandHandler<Ping>>().ShouldBeSameAs(one);
    }

    [Fact]
    public async Task A_request_with_no_handler_is_a_wiring_failure()
    {
        using ServiceProvider provider = BuildContainer();
        using IServiceScope scope = provider.CreateScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

        // Not a failed Result: nobody sent anything invalid, the container was assembled wrong.
        await Should.ThrowAsync<InvalidOperationException>(
            () => sender.Send(new Unhandled(), TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Scanning registers a handler under its handler interfaces and nothing else. Registering
    /// every interface a class happens to implement would make a handler the answer to requests
    /// that have nothing to do with it.
    /// </summary>
    [Fact]
    public async Task Only_the_handler_interfaces_are_registered()
    {
        using ServiceProvider provider = BuildContainer();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetService<IDisposable>().ShouldBeNull();

        // The handler that also implements IDisposable is still reachable as a handler.
        Result result = await scope.ServiceProvider
            .GetRequiredService<ISender>()
            .Send(new Closable(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task A_null_request_is_rejected()
    {
        using ServiceProvider provider = BuildContainer();
        using IServiceScope scope = provider.CreateScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

        await Should.ThrowAsync<ArgumentNullException>(
            () => sender.Send((ICommand)null!, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Scanning_nothing_is_rejected()
    {
        Should.Throw<ArgumentException>(() => new ServiceCollection().AddMessaging());
    }
}

/// <summary>
/// Records what the handlers below were asked to do. Registered as a singleton so it outlives the
/// scopes the handlers are created in.
/// </summary>
internal sealed class DispatchLog
{
    private readonly List<string> _calls = [];
    private readonly List<object> _handlers = [];

    public IReadOnlyList<string> Calls => _calls;

    public IReadOnlyList<object> Handlers => _handlers;

    public CancellationToken Token { get; private set; }

    public void Record(string call, object handler, CancellationToken ct)
    {
        _calls.Add(call);
        _handlers.Add(handler);
        Token = ct;
    }
}

internal sealed record Ping : ICommand;

internal sealed class PingHandler(DispatchLog log) : ICommandHandler<Ping>
{
    public Task<Result> Handle(Ping command, CancellationToken ct)
    {
        log.Record("ping", this, ct);
        return Task.FromResult(Result.Success());
    }
}

internal sealed record Register(string Name) : ICommand<Guid>;

internal sealed class RegisterHandler(DispatchLog log) : ICommandHandler<Register, Guid>
{
    internal static readonly Guid Registered = Guid.Parse("0199a6f4-1c5b-7c2e-9a3d-6f1b8c4d5e2a");

    public Task<Result<Guid>> Handle(Register command, CancellationToken ct)
    {
        log.Record($"register {command.Name}", this, ct);
        return Task.FromResult(Result.Success(Registered));
    }
}

internal sealed record GetCourt(int Id) : IQuery<string>;

internal sealed class GetCourtHandler(DispatchLog log) : IQueryHandler<GetCourt, string>
{
    public Task<Result<string>> Handle(GetCourt query, CancellationToken ct)
    {
        log.Record($"get {query.Id}", this, ct);
        return Task.FromResult(Result.Success($"court {query.Id}"));
    }
}

internal sealed record Refuse : ICommand;

internal sealed class RefuseHandler : ICommandHandler<Refuse>
{
    internal static readonly Error Refused = new("court.unavailable", "The court is already booked.");

    public Task<Result> Handle(Refuse command, CancellationToken ct) => Task.FromResult(Result.Failure(Refused));
}

/// <summary>A command whose handler is deliberately never registered.</summary>
internal sealed record Unhandled : ICommand;

internal sealed record Closable : ICommand;

/// <summary>
/// A handler that implements an interface of its own. Only its handler interface should reach the
/// container.
/// </summary>
internal sealed class ClosableHandler : ICommandHandler<Closable>, IDisposable
{
    public Task<Result> Handle(Closable command, CancellationToken ct) => Task.FromResult(Result.Success());

    public void Dispose() => GC.SuppressFinalize(this);
}
