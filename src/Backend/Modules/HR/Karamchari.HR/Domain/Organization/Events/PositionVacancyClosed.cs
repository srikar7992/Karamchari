// -----------------------------------------------------------------------
// <copyright file="PositionVacancyClosed.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Karamchari.Core.Domain.Primitives;

namespace Karamchari.HR.Domain.Organization.Events;

public sealed record PositionVacancyClosed(
    Guid VacancyId,
    string TenantId,
    Guid PositionId,
    VacancyStatus Status,
    DateTimeOffset ClosedUtc,
    string? ClosedReason,
    Guid? FilledByEmployeeId,
    Guid? FilledPositionAssignmentId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
