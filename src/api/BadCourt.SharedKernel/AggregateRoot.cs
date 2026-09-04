namespace BadCourt.SharedKernel;

/// <summary>
/// The entity at the boundary of a consistency rule: the only kind a repository loads and
/// saves, and the only kind that raises domain events.
/// </summary>
public abstract class AggregateRoot(Guid id) : Entity(id)
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// The events raised since the aggregate was loaded. This is a snapshot, so a caller
    /// that reads the events and then clears them still holds what it read.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => [.. _domainEvents];

    /// <summary>
    /// Records that something happened. Only the aggregate itself may say so.
    /// </summary>
    protected void Raise(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Called by infrastructure once the events have been taken for dispatch.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
