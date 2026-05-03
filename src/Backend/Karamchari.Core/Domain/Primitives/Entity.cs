namespace Karamchari.Core.Domain.Primitives;

/// <summary>
/// Base class for entities — objects with identity, distinguished by <typeparamref name="TId"/>.
/// Equality is by identifier, not by reference.
/// </summary>
/// <typeparam name="TId">The identifier type. Must be a non-null value.</typeparam>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    protected Entity(TId id)
    {
        ArgumentNullException.ThrowIfNull(id);
        Id = id;
    }

    /// <summary>EF Core requires a parameterless constructor for materialization. Do not call directly.</summary>
    protected Entity()
    {
        // EF Core: id is materialized via reflection. Default!() is acceptable here
        // because EF will overwrite it before the entity becomes observable.
        Id = default!;
    }

    public TId Id { get; protected set; }

    public bool Equals(Entity<TId>? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        // Different EF proxy types of the same identity should still equate.
        if (GetType() != other.GetType())
        {
            return false;
        }

        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override bool Equals(object? obj) => obj is Entity<TId> e && Equals(e);

    public override int GetHashCode() => EqualityComparer<TId>.Default.GetHashCode(Id);

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !(left == right);
}
