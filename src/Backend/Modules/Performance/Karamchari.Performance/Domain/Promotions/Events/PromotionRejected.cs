// -----------------------------------------------------------------------
// <copyright file="PromotionRejected.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Performance.Domain.Promotions.Events;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record PromotionRejected(
    Guid RecommendationId,
    string TenantId,
    Guid EmployeeId,
    string Reason,
    DateTimeOffset OccurredOnUtc) : IDomainEvent
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid EventId { get; } = Guid.NewGuid();
}
