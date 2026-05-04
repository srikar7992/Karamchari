namespace Karamchari.Core.Domain.Primitives;

/// <summary>
/// Implemented by entities that should be stamped with creation and modification
/// metadata automatically by the persistence layer (a <c>SaveChangesInterceptor</c>
/// in <c>Karamchari.Core.Persistence</c> populates these on the way to the DB).
///
/// Keeping the interface here â€” rather than as separate <c>ICreatable</c> /
/// <c>IModifiable</c> halves â€” keeps the audit-stamp interceptor simple and
/// avoids reflection branching.
/// </summary>
public interface IAuditable
{
    /// <summary>
    /// Gets the date and time the entity was created in UTC.
    /// </summary>
    DateTimeOffset CreatedOnUtc { get; }

    /// <summary>
    /// Gets the identifier of the user who created the entity.
    /// </summary>
    string CreatedBy { get; }

    /// <summary>
    /// Gets the date and time the entity was last updated in UTC.
    /// </summary>
    DateTimeOffset? UpdatedOnUtc { get; }

    /// <summary>
    /// Gets the identifier of the user who last updated the entity.
    /// </summary>
    string? UpdatedBy { get; }
}
