// -----------------------------------------------------------------------
// <copyright file="ReportingRelationshipEnded.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Karamchari.Core.Domain.Primitives;

namespace Karamchari.HR.Domain.Organization.Events;

public sealed record ReportingRelationshipEnded(
    Guid RelationshipId,
    string TenantId,
    Guid ManagerPositionId,
    Guid DirectReportPositionId,
    DateTimeOffset EffectiveTo) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
