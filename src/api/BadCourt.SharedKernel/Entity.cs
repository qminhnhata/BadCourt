namespace BadCourt.SharedKernel;

/// <summary>
/// A domain object with a lifetime and an identity of its own. Two entities are the same
/// entity when they are the same type and carry the same <see cref="Id"/>, whatever their
/// other fields say: a court that has been renamed is still that court.
/// </summary>
public abstract class Entity : IEquatable<Entity>
{
    protected Entity(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("An entity must have an identity.", nameof(id));
        }

        Id = id;
    }

    public Guid Id { get; }

    public bool Equals(Entity? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        // A Court and a Facility that happen to share an id are still different things.
        return other.GetType() == GetType() && other.Id == Id;
    }

    public override bool Equals(object? obj) => obj is Entity entity && Equals(entity);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity? left, Entity? right) => Equals(left, right);

    public static bool operator !=(Entity? left, Entity? right) => !Equals(left, right);
}
