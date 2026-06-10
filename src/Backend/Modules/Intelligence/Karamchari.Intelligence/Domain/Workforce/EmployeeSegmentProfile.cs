// -----------------------------------------------------------------------
// <copyright file="EmployeeSegmentProfile.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Multitenancy;

namespace Karamchari.Intelligence.Domain.Workforce;

/// <summary>
/// Assigns an employee to a workforce segment so that segment-specific risk thresholds
/// can be applied during scoring. Without segmentation, a 20-hour overtime week would
/// trigger High burnout for an office worker and an emergency-services officer equally —
/// which is incorrect.
///
/// Maps to Intel_EmployeeSegments.
/// </summary>
public sealed class EmployeeSegmentProfile : ITenantOwned
{
    public Guid Id { get; private set; }

    /// <inheritdoc/>
    public string TenantId { get; private set; } = string.Empty;

    public Guid EmployeeId { get; private set; }

    public WorkforceSegment Segment { get; private set; }

    /// <summary>UTC timestamp when the segment assignment was last set.</summary>
    public DateTime AssignedAt { get; private set; }

    /// <summary>Optional identifier of the user or process that assigned the segment.</summary>
    public string? AssignedBy { get; private set; }

    private EmployeeSegmentProfile() { }

    public static EmployeeSegmentProfile Assign(
        string tenantId,
        Guid employeeId,
        WorkforceSegment segment,
        string? assignedBy = null)
    {
        return new EmployeeSegmentProfile
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            Segment = segment,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = assignedBy
        };
    }

    public void Reassign(WorkforceSegment segment, string? assignedBy = null)
    {
        Segment = segment;
        AssignedAt = DateTime.UtcNow;
        AssignedBy = assignedBy;
    }
}
