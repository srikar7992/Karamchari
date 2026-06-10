// -----------------------------------------------------------------------
// <copyright file="WorkforceHealthInput.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Intelligence.Services.Scoring;

/// <summary>Aggregated inputs for org-level workforce health calculation.</summary>
public sealed record WorkforceHealthInput(
    string SiteCode,
    decimal AttendanceRate,
    decimal AverageBurnoutScore,
    decimal AverageCoveragePct,
    decimal LeaveApprovalRatio,
    decimal OvertimePayrollFraction,
    decimal AvgSwapsPerEmployeePerMonth,
    int EmployeeCount
);

/// <summary>Result from WorkforceHealthCalculator.</summary>
public sealed record WorkforceHealthResult(
    decimal Score,
    decimal AttendanceHealth,
    decimal BurnoutHealth,
    decimal StaffingHealth,
    decimal LeaveHealth,
    decimal PayrollHealth,
    decimal StabilityHealth,
    Karamchari.Intelligence.Domain.Signals.ScoreExplanation Explanation
);

/// <summary>Aggregated inputs for manager effectiveness calculation.</summary>
public sealed record ManagerEffectivenessInput(
    Guid ManagerId,
    decimal TeamAttendanceRate,
    decimal HighBurnoutFraction,
    decimal HighAttritionFraction,
    decimal LeaveDenialRatio,
    decimal AvgSwapsPerMemberPerMonth,
    decimal AvgOvertimeHoursPerMember,
    decimal OrgAvgOvertimeHoursPerEmployee,
    int DirectReportCount
);

/// <summary>Result from ManagerEffectivenessCalculator.</summary>
public sealed record ManagerEffectivenessResult(
    decimal Score,
    decimal AttendanceDimension,
    decimal BurnoutDimension,
    decimal AttritionDimension,
    decimal LeaveHealthDimension,
    decimal StabilityDimension,
    decimal OvertimeDimension,
    Karamchari.Intelligence.Domain.Signals.ScoreExplanation Explanation
);
