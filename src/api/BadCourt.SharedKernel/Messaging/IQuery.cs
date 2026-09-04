namespace BadCourt.SharedKernel.Messaging;

/// <summary>
/// A request that only reads. Queries change nothing, so the pipeline opens no transaction around
/// them and no domain events come back from one.
/// </summary>
/// <typeparam name="TResponse">The value carried back on success.</typeparam>
public interface IQuery<TResponse>;
