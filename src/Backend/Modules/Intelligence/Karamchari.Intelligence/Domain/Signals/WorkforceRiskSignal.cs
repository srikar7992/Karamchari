// -----------------------------------------------------------------------
// <copyright file="WorkforceRiskSignal.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.Intelligence.Domain.Primitives;

namespace Karamchari.Intelligence.Domain.Signals;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum RiskCategory
{
    /// <inheritdoc/>
    Operational,
    /// <inheritdoc/>
    Capability,
    /// <inheritdoc/>
    Leadership,
    /// <inheritdoc/>
    Retention,
    /// <inheritdoc/>
    Compliance
}

/// <summary>
/// Aggregate root for tracking strategic workforce risks.
/// Provides explainable indicators of instability (e.g., succession gaps).
/// </summary>
public sealed class WorkforceRiskSignal : AggregateRoot<Guid>, ITenantOwned
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string SubjectId { get; private set; } = string.Empty; // Employee, Role, or Department

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public RiskCategory Category { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public WorkforcePressureIndex Severity { get; private set; } = null!;

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
    public DateTimeOffset DetectedAtUtc { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    private WorkforceRiskSignal() { }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static WorkforceRiskSignal Detect(
        string tenantId,
        string subjectId,
        RiskCategory category,
        WorkforcePressureIndex severity,
        SignalConfidence confidence,
        ScoreExplanation explanation)
    {
        return new WorkforceRiskSignal
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SubjectId = subjectId,
            Category = category,
            Severity = severity,
            Confidence = confidence,
            Explanation = explanation,
            DetectedAtUtc = DateTimeOffset.UtcNow
        };
    }
}
