// -----------------------------------------------------------------------
// <copyright file="GoalCycleActivated.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Performance.Domain.Goals.Events;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record GoalCycleActivated(
    Guid CycleId,
    string TenantId,
    string CycleName,
    DateOnly StartDate,
    DateOnly EndDate,
    DateTimeOffset OccurredOnUtc) : IDomainEvent
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid EventId { get; } = Guid.NewGuid();
}
