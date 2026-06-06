using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.Intelligence.Domain.Signals;

namespace Karamchari.Intelligence.Domain.Workforce;

/// <summary>
/// Latest burnout risk score for a single employee.
/// One row per employee — upserted every time the scoring engine runs.
/// Historical trends are captured via WorkforceScoreSnapshot (written alongside each upsert).
/// </summary>
public sealed class WorkforceBurnoutScore : AggregateRoot<Guid>, ITenantOwned
{
    /// <inheritdoc/>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Employee this score belongs to.</summary>
    public Guid EmployeeId { get; private set; }

    /// <summary>Score in [0, 100]. Higher = higher burnout risk.</summary>
    public decimal Score { get; private set; }

    /// <summary>Bucketed risk level derived from Score.</summary>
    public WorkforceRiskLevel RiskLevel { get; private set; }

    /// <summary>How much to trust this score given data history length.</summary>
    public WorkforceScoreConfidence Confidence { get; private set; }

    /// <summary>Breakdown of which signals contributed and why.</summary>
    public ScoreExplanation Explanation { get; private set; } = null!;

    /// <summary>Weighted component scores (signal name → component contribution).</summary>
    public string ComponentScoresJson { get; private set; } = string.Empty;

    /// <summary>UTC timestamp of the most recent calculation.</summary>
    public DateTime CalculatedAt { get; private set; }

    /// <summary>Optimistic concurrency token.</summary>
    public byte[] RowVersion { get; private set; } = [];

    private WorkforceBurnoutScore() { }

    /// <summary>Creates a new burnout score (first calculation for this employee).</summary>
    public static WorkforceBurnoutScore Create(
        string tenantId,
        Guid employeeId,
        decimal score,
        WorkforceRiskLevel riskLevel,
        WorkforceScoreConfidence confidence,
        ScoreExplanation explanation,
        string componentScoresJson)
    {
        return new WorkforceBurnoutScore
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            Score = score,
            RiskLevel = riskLevel,
            Confidence = confidence,
            Explanation = explanation,
            ComponentScoresJson = componentScoresJson,
            CalculatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Refreshes score in place — called on subsequent recalculations.</summary>
    public void Refresh(
        decimal score,
        WorkforceRiskLevel riskLevel,
        WorkforceScoreConfidence confidence,
        ScoreExplanation explanation,
        string componentScoresJson)
    {
        Score = score;
        RiskLevel = riskLevel;
        Confidence = confidence;
        Explanation = explanation;
        ComponentScoresJson = componentScoresJson;
        CalculatedAt = DateTime.UtcNow;
    }
}
