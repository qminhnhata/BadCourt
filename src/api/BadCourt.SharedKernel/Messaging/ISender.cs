namespace BadCourt.SharedKernel.Messaging;

/// <summary>
/// The single entry point into the pipeline. Presentation code depends on this and never resolves
/// a handler itself, which is what guarantees that every request meets the decorators.
/// </summary>
/// <remarks>
/// No overload defaults its cancellation token. A request that cannot be cancelled should be a
/// decision someone made, not one that a forgotten argument made quietly.
/// </remarks>
public interface ISender
{
    /// <summary>
    /// Dispatches a command to its handler.
    /// </summary>
    Task<Result> Send(ICommand command, CancellationToken ct);

    /// <summary>
    /// Dispatches a command that produces a value to its handler.
    /// </summary>
    Task<Result<TResponse>> Send<TResponse>(ICommand<TResponse> command, CancellationToken ct);

    /// <summary>
    /// Dispatches a query to its handler.
    /// </summary>
    Task<Result<TResponse>> Send<TResponse>(IQuery<TResponse> query, CancellationToken ct);
}
