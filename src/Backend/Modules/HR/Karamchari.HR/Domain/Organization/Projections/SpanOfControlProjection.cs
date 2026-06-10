// -----------------------------------------------------------------------
// <copyright file="SpanOfControlProjection.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace Karamchari.HR.Domain.Organization.Projections;

public sealed class SpanOfControlProjection
{
    public string TenantId { get; set; } = string.Empty;
    public Guid ManagerPositionId { get; set; }
    public Guid? ManagerEmployeeId { get; set; }
    public string ManagerName { get; set; } = string.Empty;
    public int DirectReportCount { get; set; }
    public int IndirectReportCount { get; set; }
    public DateTimeOffset LastUpdatedAtUtc { get; set; }
}
