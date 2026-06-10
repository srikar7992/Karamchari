// -----------------------------------------------------------------------
// <copyright file="WorkforceHealthScore.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.Intelligence.Domain.Signals;

namespace Karamchari.Intelligence.Domain.Workforce;

/// <summary>
/// Organisation-wide (or site-level) workforce health score.
/// One row per (TenantId, SiteCode) — upserted each scoring cycle.
///
/// Formula weights:
///   Attendance 25% + Burnout 25% + Coverage 20% + Leave 10% + Payroll 10% + Stability 10%
/// </summary>
public sealed class WorkforceHealthScore : AggregateRoot<Guid>, ITenantOwned
{
    /// <inheritdoc/>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>
    /// Site/location code scoping this score.
    /// Use the sentinel value "ALL" to represent the entire tenant.
    /// </summary>
    public string SiteCode { get; private set; } = string.Empty;

    /// <summary>Overall health score in [0, 100]. Higher = healthier.</summary>
    public decimal Score { get; private set; }

    // ── Dimension scores (all [0,100]) ──────────────────────────────────────
    /// <summary>Attendance health: present rate vs expected headcount.</summary>
    public decimal AttendanceHealth { get; private set; }

    /// <summary>Burnout health: inverted average burnout score across employees.</summary>
    public decimal BurnoutHealth { get; private set; }

    /// <summary>Staffing health: average coverage % from WFP module.</summary>
    public decimal StaffingHealth { get; private set; }

    /// <summary>Leave health: approved leave ratio.</summary>
    public decimal LeaveHealth { get; private set; }

    /// <summary>Payroll health: inverted OT spend as % of total payroll.</summary>
    public decimal PayrollHealth { get; private set; }

    /// <summary>Schedule stability: inverted shift-change frequency.</summary>
    public decimal StabilityHealth { get; private set; }

    /// <summary>How many employees contributed to this score.</summary>
    public int EmployeeCount { get; private set; }

    /// <summary>Breakdown explanation for executive dashboards.</summary>
    public ScoreExplanation Explanation { get; private set; } = null!;

    /// <summary>UTC timestamp of the most recent calculation.</summary>
    public DateTime CalculatedAt { get; private set; }

    /// <summary>Optimistic concurrency token.</summary>
    public byte[] RowVersion { get; private set; } = [];

    private WorkforceHealthScore() { }

    /// <summary>Creates a new workforce health score.</summary>
    public static WorkforceHealthScore Create(
        string tenantId,
        string siteCode,
        decimal attendanceHealth,
        decimal burnoutHealth,
        decimal staffingHealth,
        decimal leaveHealth,
        decimal payrollHealth,
        decimal stabilityHealth,
        int employeeCount,
        ScoreExplanation explanation)
    {
        var score = ComputeOverall(attendanceHealth, burnoutHealth, staffingHealth, leaveHealth, payrollHealth, stabilityHealth);
        return new WorkforceHealthScore
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SiteCode = siteCode,
            Score = score,
            AttendanceHealth = attendanceHealth,
            BurnoutHealth = burnoutHealth,
            StaffingHealth = staffingHealth,
            LeaveHealth = leaveHealth,
            PayrollHealth = payrollHealth,
            StabilityHealth = stabilityHealth,
            EmployeeCount = employeeCount,
            Explanation = explanation,
            CalculatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Refreshes all dimensions in place.</summary>
    public void Refresh(
        decimal attendanceHealth,
        decimal burnoutHealth,
        decimal staffingHealth,
        decimal leaveHealth,
        decimal payrollHealth,
        decimal stabilityHealth,
        int employeeCount,
        ScoreExplanation explanation)
    {
        Score = ComputeOverall(attendanceHealth, burnoutHealth, staffingHealth, leaveHealth, payrollHealth, stabilityHealth);
        AttendanceHealth = attendanceHealth;
        BurnoutHealth = burnoutHealth;
        StaffingHealth = staffingHealth;
        LeaveHealth = leaveHealth;
        PayrollHealth = payrollHealth;
        StabilityHealth = stabilityHealth;
        EmployeeCount = employeeCount;
        Explanation = explanation;
        CalculatedAt = DateTime.UtcNow;
    }

    private static decimal ComputeOverall(
        decimal attendance, decimal burnout, decimal staffing,
        decimal leave, decimal payroll, decimal stability)
        => (attendance * 0.25m) + (burnout * 0.25m) + (staffing * 0.20m)
         + (leave * 0.10m) + (payroll * 0.10m) + (stability * 0.10m);
}
