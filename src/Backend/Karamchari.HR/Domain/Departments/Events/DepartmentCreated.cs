using Karamchari.Core.Domain.Primitives;

namespace Karamchari.HR.Domain.Departments.Events;

/// <summary>
/// TODO: Add XML documentation.
/// </summary>
public sealed record DepartmentCreated(
    Guid DepartmentId,
    string Name,
    string? Description) : IDomainEvent
{
    /// <summary>
    /// TODO: Add XML documentation.
    /// </summary>
    public Guid EventId { get; } = Guid.NewGuid();

    /// <summary>
    /// TODO: Add XML documentation.
    /// </summary>
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
