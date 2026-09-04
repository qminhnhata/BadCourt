namespace BadCourt.SharedKernel.Messaging;

/// <summary>
/// A request to change something, answering only with success or failure.
/// </summary>
/// <remarks>
/// Deliberately not a base of <see cref="ICommand{TResponse}"/>. A request that were both would
/// have two handler shapes serving it and two ways to dispatch, and which one ran would be settled
/// by overload resolution rather than by whoever wrote the command.
/// </remarks>
public interface ICommand;

/// <summary>
/// A request to change something that also produces a value - most often the identity of what
/// it created.
/// </summary>
/// <typeparam name="TResponse">The value carried back on success.</typeparam>
public interface ICommand<TResponse>;
