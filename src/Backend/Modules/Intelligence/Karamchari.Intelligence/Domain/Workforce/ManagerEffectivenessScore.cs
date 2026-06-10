// -----------------------------------------------------------------------
// <copyright file="ManagerEffectivenessScore.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.Intelligence.Domain.Signals;

namespace Karamchari.Intelligence.Domain.Workforce;

/// <summary>
/// Effectiveness score for a manager, derived from their team's operational metrics.
/// One row per manager — upserted each scoring cycle.
/// Not computed for managers with fewer than 5 direct reports (statistically unreliable).
///
/// Formula weights:
///   Attendance 20% + Burnout 25% + Attrition 25% + LeaveHealth 10% + Stability 10% + OT 10%
/// </summary>
public sealed class ManagerEffectivenessScore : AggregateRoot<Guid>, ITenantOwned
{
    /// <inheritdoc/>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Manager whose team metrics produced this score.</summary>
    public Guid ManagerId { get; private set; }

    /// <summary>Effectiveness score in [0, 100]. Higher = more effective team leadership.</summary>
    public decimal Score { get; private set; }

    // ── Dimension scores (all [0,100]) ──────────────────────────────────────
    /// <summary>Team average attendance score.</summary>
    public decimal AttendanceDimension { get; private set; }

    /// <summary>Inverted fraction of team members at High/Critical burnout.</summary>
    public decimal BurnoutDimension { get; private set; }

    /// <summary>Inverted fraction of team members at High/Critical attrition risk.</summary>
    public decimal AttritionDimension { get; private set; }

    /// <summary>Leave approval health (low denial ratio = higher score).</summary>
    public decimal LeaveHealthDimension { get; private set; }

    /// <summary>Team shift stability (low swap frequency = higher score).</summary>
    public decimal StabilityDimension { get; private set; }

    /// <summary>Overtime dependency (lower OT hours per head = higher score).</summary>
    public decimal OvertimeDimension { get; private set; }

    /// <summary>Number of direct reports used for this calculation.</summary>
    public int DirectReportCount { get; private set; }

    /// <summary>Explanation for executive dashboards.</summary>
    public ScoreExplanation Explanation { get; private set; } = null!;

    /// <summary>UTC timestamp of the most recent calculation.</summary>
    public DateTime CalculatedAt { get; private set; }

    /// <summary>Optimistic concurrency token.</summary>
    public byte[] RowVersion { get; private set; } = [];

    private ManagerEffectivenessScore() { }

    /// <summary>Creates a new manager effectiveness score.</summary>
    public static ManagerEffectivenessScore Create(
        string tenantId,
        Guid managerId,
        decimal attendance,
        decimal burnout,
        decimal attrition,
        decimal leaveHealth,
        decimal stability,
        decimal overtime,
        int directReportCount,
        ScoreExplanation explanation)
    {
        return new ManagerEffectivenessScore
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ManagerId = managerId,
            Score = ComputeOverall(attendance, burnout, attrition, leaveHealth, stability, overtime),
            AttendanceDimension = attendance,
            BurnoutDimension = burnout,
            AttritionDimension = attrition,
            LeaveHealthDimension = leaveHealth,
            StabilityDimension = stability,
            OvertimeDimension = overtime,
            DirectReportCount = directReportCount,
            Explanation = explanation,
            CalculatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Refreshes all dimensions in place.</summary>
    public void Refresh(
        decimal attendance,
        decimal burnout,
        decimal attrition,
        decimal leaveHealth,
        decimal stability,
        decimal overtime,
        int directReportCount,
        ScoreExplanation explanation)
    {
        Score = ComputeOverall(attendance, burnout, attrition, leaveHealth, stability, overtime);
        AttendanceDimension = attendance;
        BurnoutDimension = burnout;
        AttritionDimension = attrition;
        LeaveHealthDimension = leaveHealth;
        StabilityDimension = stability;
        OvertimeDimension = overtime;
        DirectReportCount = directReportCount;
        Explanation = explanation;
        CalculatedAt = DateTime.UtcNow;
    }

    private static decimal ComputeOverall(
        decimal attendance, decimal burnout, decimal attrition,
        decimal leaveHealth, decimal stability, decimal overtime)
        => (attendance * 0.20m) + (burnout * 0.25m) + (attrition * 0.25m)
         + (leaveHealth * 0.10m) + (stability * 0.10m) + (overtime * 0.10m);
}
