// -----------------------------------------------------------------------
// <copyright file="PositionOccupantProjection.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace Karamchari.HR.Domain.Organization.Projections;

public sealed class PositionOccupantProjection
{
    public string TenantId { get; set; } = string.Empty;
    public Guid PositionId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid AssignmentId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
}
