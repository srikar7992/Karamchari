// -----------------------------------------------------------------------
// <copyright file="SuccessionEvents.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Succession.Domain.Events;

public sealed record SuccessorAddedEvent(
    string TenantId,
    SuccessionPlanId PlanId,
    CriticalRoleId RoleId,
    Guid EmployeeId,
    ReadinessLevel Readiness) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record SuccessorPromotedEvent(
    string TenantId,
    SuccessionPlanId PlanId,
    CriticalRoleId RoleId,
    Guid EmployeeId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
