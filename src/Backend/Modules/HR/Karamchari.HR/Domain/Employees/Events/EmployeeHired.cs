using Karamchari.Core.Domain.Primitives;

namespace Karamchari.HR.Domain.Employees.Events;

/// <summary>
/// Raised when an <see cref="Employee"/> is created via <see cref="Employee.Hire"/>.
/// Sealed record so the message contract is immutable on the wire and the
/// downstream consumer can rely on positional matching.
///
/// Note that <see cref="OccurredOnUtc"/> is captured at construction (i.e. at
/// Hire time, not at publish time) so retries don't drift the timestamp.
/// </summary>
public sealed record EmployeeHired(
    Guid EmployeeId,
    string EmployeeNumber,
    string LegalName,
    string? WorkEmail,
    DateOnly HiredOn) : IDomainEvent
{
    /// <summary>
    /// Gets the unique event identifier used for idempotent processing.
    /// </summary>
    public Guid EventId { get; } = Guid.NewGuid();

    /// <summary>
    /// Gets the UTC timestamp captured when the event instance is created.
    /// </summary>
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
