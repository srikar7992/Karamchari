using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.Intelligence.Domain.Primitives;

namespace Karamchari.Intelligence.Domain.Signals;

public enum RiskCategory
{
    Operational,
    Capability,
    Leadership,
    Retention,
    Compliance
}

/// <summary>
/// Aggregate root for tracking strategic workforce risks.
/// Provides explainable indicators of instability (e.g., succession gaps).
/// </summary>
public sealed class WorkforceRiskSignal : AggregateRoot<Guid>, ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public string SubjectId { get; private set; } = string.Empty; // Employee, Role, or Department
    
    public RiskCategory Category { get; private set; }
    public WorkforcePressureIndex Severity { get; private set; } = null!;
    
    public SignalConfidence Confidence { get; private set; } = null!;
    public ScoreExplanation Explanation { get; private set; } = null!;

    public DateTimeOffset DetectedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    private WorkforceRiskSignal() { }

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
