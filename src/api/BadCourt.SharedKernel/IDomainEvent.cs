namespace BadCourt.SharedKernel;

/// <summary>
/// Something that has happened inside the domain, raised by an aggregate and handled
/// in-process within the same transaction as the change that caused it.
/// </summary>
/// <remarks>
/// Deliberately carries no timestamp. Domain code has no clock — a handler that needs the
/// time of the event takes <see cref="TimeProvider"/> — and anything that must leave the
/// process travels as an integration event instead.
/// </remarks>
public interface IDomainEvent;
