using BadCourt.SharedKernel;
using BadCourt.SharedKernel.Messaging;
using Shouldly;
using Xunit;

namespace BadCourt.UnitTests.SharedKernel.Messaging;

/// <summary>
/// The messaging interfaces carry no implementation of their own, so these are typing tests: a
/// wrong shape surfaces as a compile error in this file rather than as a red test. They pin that
/// the interfaces can be implemented at all, that a closed handler type can be built from a
/// request type through reflection alone, and that the sender's overloads bind to the kinds of
/// request they were each written for.
/// </summary>
public class MessagingContractTests
{
    [Fact]
    public async Task A_command_handler_is_reached_through_its_interface()
    {
        ICommandHandler<RenameCourt> handler = new RenameCourtHandler();

        Result result = await handler.Handle(new RenameCourt("Centre"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task A_command_handler_carries_its_value_back()
    {
        ICommandHandler<CreateCourt, Guid> handler = new CreateCourtHandler();

        Result<Guid> result = await handler.Handle(new CreateCourt("Centre"), TestContext.Current.CancellationToken);

        result.Value.ShouldBe(CreateCourtHandler.Created);
    }

    [Fact]
    public async Task A_query_handler_carries_its_value_back()
    {
        IQueryHandler<GetCourtName, string> handler = new GetCourtNameHandler();

        Result<string> result = await handler.Handle(new GetCourtName(Guid.CreateVersion7()), TestContext.Current.CancellationToken);

        result.Value.ShouldBe("Centre");
    }

    [Fact]
    public void The_closed_handler_interface_is_reachable_from_the_request_type()
    {
        // What a dispatcher must do: it holds a request whose type it only learns at run time, and
        // has to name the one handler interface that serves it.
        Type closed = typeof(ICommandHandler<,>).MakeGenericType(typeof(CreateCourt), typeof(Guid));

        closed.IsAssignableFrom(typeof(CreateCourtHandler)).ShouldBeTrue();
    }

    [Fact]
    public async Task Each_kind_of_request_binds_to_its_own_sender_overload()
    {
        RecordingSender sender = new();
        CancellationToken ct = TestContext.Current.CancellationToken;

        await sender.Send(new RenameCourt("Centre"), ct);
        await sender.Send(new CreateCourt("Centre"), ct);
        await sender.Send(new GetCourtName(Guid.CreateVersion7()), ct);

        sender.Calls.ShouldBe(["command", "command with response", "query"]);
    }

    private sealed record RenameCourt(string Name) : ICommand;

    private sealed class RenameCourtHandler : ICommandHandler<RenameCourt>
    {
        public Task<Result> Handle(RenameCourt command, CancellationToken ct) =>
            Task.FromResult(Result.Success());
    }

    private sealed record CreateCourt(string Name) : ICommand<Guid>;

    private sealed class CreateCourtHandler : ICommandHandler<CreateCourt, Guid>
    {
        internal static readonly Guid Created = Guid.CreateVersion7();

        public Task<Result<Guid>> Handle(CreateCourt command, CancellationToken ct) =>
            Task.FromResult(Result.Success(Created));
    }

    private sealed record GetCourtName(Guid CourtId) : IQuery<string>;

    private sealed class GetCourtNameHandler : IQueryHandler<GetCourtName, string>
    {
        public Task<Result<string>> Handle(GetCourtName query, CancellationToken ct) =>
            Task.FromResult(Result.Success("Centre"));
    }

    /// <summary>
    /// Records which overload the compiler chose. The canned return values are not the point;
    /// the binding is.
    /// </summary>
    private sealed class RecordingSender : ISender
    {
        private readonly List<string> _calls = [];

        public IReadOnlyList<string> Calls => _calls;

        public Task<Result> Send(ICommand command, CancellationToken ct)
        {
            _calls.Add("command");
            return Task.FromResult(Result.Success());
        }

        public Task<Result<TResponse>> Send<TResponse>(ICommand<TResponse> command, CancellationToken ct)
        {
            _calls.Add("command with response");
            return Task.FromResult(Result.Success(default(TResponse)!));
        }

        public Task<Result<TResponse>> Send<TResponse>(IQuery<TResponse> query, CancellationToken ct)
        {
            _calls.Add("query");
            return Task.FromResult(Result.Success(default(TResponse)!));
        }
    }
}
