// -----------------------------------------------------------------------
// <copyright file="DepartmentCreated.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;

namespace Karamchari.HR.Domain.Departments.Events;

/// <summary>
/// Domain event raised after a department is created.
/// </summary>
public sealed record DepartmentCreated(
    Guid DepartmentId,
    string Name,
    string? Description) : IDomainEvent
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
