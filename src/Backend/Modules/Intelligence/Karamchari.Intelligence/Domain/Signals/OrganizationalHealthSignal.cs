// -----------------------------------------------------------------------
// <copyright file="OrganizationalHealthSignal.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.Intelligence.Domain.Primitives;

namespace Karamchari.Intelligence.Domain.Signals;

/// <summary>
/// Value object representing the standardized pressure level (1-10) across organizational domains.
/// Prevents semantic drift between operational and strategic risk definitions.
/// </summary>
public sealed record WorkforcePressureIndex(int Level)
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static WorkforcePressureIndex Create(int level)
    {
        if (level is < 1 or > 10)
            throw new ArgumentOutOfRangeException(nameof(level), "Pressure index must be between 1 and 10.");
        return new WorkforcePressureIndex(level);
    }
}

/// <summary>
/// Aggregate root representing the high-level health of an organizational unit.
/// Correlates signals from Attendance (OT), HR (Turnover), and Learning (Upskilling).
/// </summary>
public sealed class OrganizationalHealthSignal : AggregateRoot<Guid>, ITenantOwned
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string OrgUnitId { get; private set; } = string.Empty; // Department or Team

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal OverallHealthScore { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public WorkforcePressureIndex BurnoutRisk { get; private set; } = null!;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public WorkforcePressureIndex StaffingStress { get; private set; } = null!;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public SignalConfidence Confidence { get; private set; } = null!;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public ScoreExplanation Explanation { get; private set; } = null!;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset EvaluatedAtUtc { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    private OrganizationalHealthSignal() { }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static OrganizationalHealthSignal Evaluate(
        string tenantId,
        string orgUnitId,
        decimal score,
        WorkforcePressureIndex burnout,
        WorkforcePressureIndex staffing,
        SignalConfidence confidence,
        ScoreExplanation explanation)
    {
        return new OrganizationalHealthSignal
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrgUnitId = orgUnitId,
            OverallHealthScore = score,
            BurnoutRisk = burnout,
            StaffingStress = staffing,
            Confidence = confidence,
            Explanation = explanation,
            EvaluatedAtUtc = DateTimeOffset.UtcNow
        };
    }
}
