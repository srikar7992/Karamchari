namespace Karamchari.Core.Domain.Primitives;

/// <summary>
/// Implemented by entities that should be stamped with creation and modification
/// metadata automatically by the persistence layer (a <c>SaveChangesInterceptor</c>
/// in <c>Karamchari.Core.Persistence</c> populates these on the way to the DB).
///
/// Keeping the interface here — rather than as separate <c>ICreatable</c> /
/// <c>IModifiable</c> halves — keeps the audit-stamp interceptor simple and
/// avoids reflection branching.
/// </summary>
public interface IAuditable
{
    DateTimeOffset CreatedOnUtc { get; }

    string CreatedBy { get; }

    DateTimeOffset? UpdatedOnUtc { get; }

    string? UpdatedBy { get; }
}
