namespace BadCourt.SharedKernel.Messaging;

/// <summary>
/// Runs one command. A handler reports business failure by returning a failed <see cref="Result"/>
/// rather than by throwing, so the decorators wrapped around it can read the outcome without
/// catching anything.
/// </summary>
/// <typeparam name="TCommand">The command this handler runs.</typeparam>
public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    /// <summary>
    /// Runs the command.
    /// </summary>
    Task<Result> Handle(TCommand command, CancellationToken ct);
}

/// <summary>
/// Runs one command and produces a value.
/// </summary>
/// <typeparam name="TCommand">The command this handler runs.</typeparam>
/// <typeparam name="TResponse">The value produced on success.</typeparam>
public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    /// <summary>
    /// Runs the command.
    /// </summary>
    Task<Result<TResponse>> Handle(TCommand command, CancellationToken ct);
}
