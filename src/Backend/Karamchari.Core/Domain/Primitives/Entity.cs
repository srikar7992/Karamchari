namespace Karamchari.Core.Domain.Primitives;

/// <summary>
/// Base class for entities Ã¢â‚¬â€ objects with identity, distinguished by <typeparamref name="TId"/>.
/// Equality is by identifier, not by reference.
/// </summary>
/// <typeparam name="TId">The identifier type. Must be a non-null value.</typeparam>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Entity{TId}"/> class.
    /// </summary>
    /// <param name="id">The identifier.</param>
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

    /// <summary>
    /// Gets the entity identifier.
    /// </summary>
    public TId Id { get; protected set; }

    /// <summary>
    /// Checks if two entities are equal by comparing their identifiers.
    /// </summary>
    /// <param name="other">The other entity to compare.</param>
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

    /// <summary>
    /// Checks if an object is an entity equal to this one.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    public override bool Equals(object? obj) => obj is Entity<TId> e && Equals(e);

    /// <summary>
    /// Returns the hash code of the entity identifier.
    /// </summary>
    public override int GetHashCode() => EqualityComparer<TId>.Default.GetHashCode(Id);

    /// <summary>
    /// Checks if two entities are equal.
    /// </summary>
    /// <param name="left">The left entity.</param>
    /// <param name="right">The right entity.</param>
    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>
    /// Checks if two entities are not equal.
    /// </summary>
    /// <param name="left">The left entity.</param>
    /// <param name="right">The right entity.</param>
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !(left == right);
}
