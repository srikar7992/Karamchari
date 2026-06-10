// -----------------------------------------------------------------------
// <copyright file="ManagerLeaveScorecard.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Multitenancy;

namespace Karamchari.TimeAttendance.Persistence;

/// <summary>
/// Monthly manager effectiveness scorecard on leave management dimensions.
/// Computed by aggregating ApprovalEscalation, LeaveApproval, LeaveCalendarEntry,
/// and WorkforceAvailabilityIndex for the manager's team.
/// </summary>
public sealed class ManagerLeaveScorecard : ITenantOwned
{
    private ManagerLeaveScorecard() { TenantId = string.Empty; }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; }
    public Guid ManagerId { get; private set; }
    public int Year { get; private set; }
    public int Month { get; private set; }
    public int TeamSize { get; private set; }

    // Approval SLA dimension
    public int TotalApprovalsRequired { get; private set; }
    public int ApprovedWithinSla { get; private set; }
    public int SlaBreaches { get; private set; }
    public decimal SlaCompliancePercent => TotalApprovalsRequired == 0
        ? 100m
        : Math.Round((decimal)ApprovedWithinSla / TotalApprovalsRequired * 100m, 1);

    // Decision quality dimension
    public int TotalDecisions { get; private set; }
    public int Approved { get; private set; }
    public int Rejected { get; private set; }
    public decimal RejectionRate => TotalDecisions == 0
        ? 0m
        : Math.Round((decimal)Rejected / TotalDecisions * 100m, 1);

    // Leave utilization dimension
    public decimal TeamLeaveUtilizationPercent { get; private set; }

    // Coverage management dimension
    public int CriticalCoverageDays { get; private set; }
    public decimal AverageTeamAvailabilityPercent { get; private set; }

    // Composite score 0-100
    public decimal CompositeScore { get; private set; }
    public DateTimeOffset ComputedAt { get; private set; }

    public static ManagerLeaveScorecard Compute(
        string tenantId,
        Guid managerId,
        int year,
        int month,
        int teamSize,
        int totalApprovalsRequired,
        int approvedWithinSla,
        int slaBreaches,
        int totalDecisions,
        int approved,
        int rejected,
        decimal teamLeaveUtilizationPercent,
        int criticalCoverageDays,
        decimal averageTeamAvailabilityPercent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        decimal slaScore = totalApprovalsRequired == 0
            ? 100m
            : (decimal)approvedWithinSla / totalApprovalsRequired * 100m;

        // Penalize rejection outliers: >40% rejection is poor signal
        decimal rejectionPenalty = totalDecisions == 0
            ? 0m
            : Math.Max(0m, ((decimal)rejected / totalDecisions * 100m) - 40m);

        decimal utilizationScore = Math.Min(teamLeaveUtilizationPercent, 100m);
        decimal availabilityScore = averageTeamAvailabilityPercent;

        // Weighted: SLA 40%, Availability 35%, Utilization 15%, Rejection penalty -10% max
        decimal composite = Math.Round(
            slaScore * 0.4m
            + availabilityScore * 0.35m
            + utilizationScore * 0.15m
            - rejectionPenalty * 0.1m,
            1);

        composite = Math.Max(0m, Math.Min(100m, composite));

        return new ManagerLeaveScorecard
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ManagerId = managerId,
            Year = year,
            Month = month,
            TeamSize = teamSize,
            TotalApprovalsRequired = totalApprovalsRequired,
            ApprovedWithinSla = approvedWithinSla,
            SlaBreaches = slaBreaches,
            TotalDecisions = totalDecisions,
            Approved = approved,
            Rejected = rejected,
            TeamLeaveUtilizationPercent = teamLeaveUtilizationPercent,
            CriticalCoverageDays = criticalCoverageDays,
            AverageTeamAvailabilityPercent = averageTeamAvailabilityPercent,
            CompositeScore = composite,
            ComputedAt = DateTimeOffset.UtcNow,
        };
    }
}
