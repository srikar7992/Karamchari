// -----------------------------------------------------------------------
// <copyright file="LearningEnrollmentCompleted.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Capability.Domain.Learning.Events;

/// <summary>
/// Raised when a LearningEnrollment transitions to Completed.
/// Consumed by LearningEnrollmentCompletedConsumer to update the employee's CapabilityProfile.
/// </summary>
public sealed record LearningEnrollmentCompleted(
    Guid EnrollmentId,
    Guid EmployeeId,
    Guid ModuleId,
    string TenantId,
    DateTimeOffset CompletedAtUtc) : IDomainEvent
{
    /// <inheritdoc/>
    public Guid EventId { get; } = Guid.NewGuid();

    /// <inheritdoc/>
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
