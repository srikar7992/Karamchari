// -----------------------------------------------------------------------
// <copyright file="OrganizationMetricsProjection.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace Karamchari.HR.Domain.Organization.Projections;

public sealed class OrganizationMetricsProjection
{
    public string TenantId { get; set; } = string.Empty;
    public Guid OrganizationUnitId { get; set; }
    public Guid? LeadEmployeeId { get; set; }
    public string? LeadEmployeeName { get; set; }
    public int ActivePositionCount { get; set; }
    public int ApprovedHeadcount { get; set; }
    public int CurrentHeadcount { get; set; }
    public int OpenVacancyCount { get; set; }
    public DateTimeOffset LastUpdatedAtUtc { get; set; }
}
