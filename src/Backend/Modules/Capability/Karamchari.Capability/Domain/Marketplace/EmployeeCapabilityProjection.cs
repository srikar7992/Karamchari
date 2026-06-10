// -----------------------------------------------------------------------
// <copyright file="EmployeeCapabilityProjection.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Karamchari.Capability.Domain.Marketplace;

/// <summary>
/// Read model representing an employee's aggregated skills, endorsements, and active opportunity gigs.
/// </summary>
public sealed class EmployeeCapabilityProjection
{
    /// <summary>
    /// Gets or sets the employee identifier.
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the employee name.
    /// </summary>
    public string EmployeeName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of verified skills.
    /// </summary>
    public List<string> VerifiedSkills { get; set; } = [];

    /// <summary>
    /// Gets or sets the count of active gigs.
    /// </summary>
    public int ActiveGigsCount { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the projection was last updated.
    /// </summary>
    public DateTimeOffset LastUpdatedAtUtc { get; set; }
}
