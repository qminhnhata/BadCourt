namespace BadCourt.SharedKernel.Messaging;

/// <summary>
/// Answers one query. Like a command handler it returns a <see cref="Result{TResponse}"/>, so
/// "not found" is an ordinary answer rather than an exception thrown across the pipeline.
/// </summary>
/// <typeparam name="TQuery">The query this handler answers.</typeparam>
/// <typeparam name="TResponse">The value produced on success.</typeparam>
public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    /// <summary>
    /// Answers the query.
    /// </summary>
    Task<Result<TResponse>> Handle(TQuery query, CancellationToken ct);
}
