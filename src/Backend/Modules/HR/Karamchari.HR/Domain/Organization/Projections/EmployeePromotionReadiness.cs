// -----------------------------------------------------------------------
// <copyright file="EmployeePromotionReadiness.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Karamchari.HR.Domain.Organization.Projections;

/// <summary>
/// Read model representing the promotion readiness score and drivers computed for an employee.
/// </summary>
public sealed class EmployeePromotionReadiness
{
    /// <summary>Gets or sets the tenant identifier associated with the employee.</summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>Gets or sets the unique identifier of the employee.</summary>
    public Guid EmployeeId { get; set; }

    /// <summary>Gets or sets the overall promotion readiness score, typically between 0.10 and 0.95.</summary>
    public decimal ReadinessScore { get; set; }

    /// <summary>Gets or sets the timestamp of when this prediction was computed in UTC.</summary>
    public DateTimeOffset LastUpdatedUtc { get; set; }

    /// <summary>Gets or sets the collection of contributing readiness drivers.</summary>
    public List<PromotionReadinessDriverProjection> Drivers { get; set; } = [];
}

