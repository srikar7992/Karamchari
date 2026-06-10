// -----------------------------------------------------------------------
// <copyright file="PositionCreated.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Karamchari.Core.Domain.Primitives;

namespace Karamchari.HR.Domain.Organization.Events;

public sealed record PositionCreated(
    Guid PositionId,
    string TenantId,
    string Code,
    Guid DepartmentId,
    Guid DesignationId,
    Guid? ReportingToPositionId,
    Guid? CostCenterId,
    Guid? LocationId,
    Guid? BusinessUnitId,
    Guid? OrgUnitId,
    PositionStatus Status,
    bool AllowMultipleOccupants,
    int ApprovedHeadcount) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
