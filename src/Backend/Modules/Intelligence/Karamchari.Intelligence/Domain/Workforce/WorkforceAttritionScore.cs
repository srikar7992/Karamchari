using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.Intelligence.Domain.Signals;

namespace Karamchari.Intelligence.Domain.Workforce;

/// <summary>
/// Latest attrition risk score for a single employee.
/// One row per employee — upserted each scoring cycle.
/// </summary>
public sealed class WorkforceAttritionScore : AggregateRoot<Guid>, ITenantOwned
{
    /// <inheritdoc/>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Employee this score belongs to.</summary>
    public Guid EmployeeId { get; private set; }

    /// <summary>Score in [0, 100]. Higher = higher attrition probability.</summary>
    public decimal Score { get; private set; }

    /// <summary>Bucketed risk level.</summary>
    public WorkforceRiskLevel RiskLevel { get; private set; }

    /// <summary>Data completeness confidence.</summary>
    public WorkforceScoreConfidence Confidence { get; private set; }

    /// <summary>Which signals contributed and why.</summary>
    public ScoreExplanation Explanation { get; private set; } = null!;

    /// <summary>Weighted component scores JSON (for auditability).</summary>
    public string ComponentScoresJson { get; private set; } = string.Empty;

    /// <summary>UTC timestamp of the most recent calculation.</summary>
    public DateTime CalculatedAt { get; private set; }

    /// <summary>Optimistic concurrency token.</summary>
    public byte[] RowVersion { get; private set; } = [];

    private WorkforceAttritionScore() { }

    /// <summary>Creates a new attrition score (first calculation for this employee).</summary>
    public static WorkforceAttritionScore Create(
        string tenantId,
        Guid employeeId,
        decimal score,
        WorkforceRiskLevel riskLevel,
        WorkforceScoreConfidence confidence,
        ScoreExplanation explanation,
        string componentScoresJson)
    {
        return new WorkforceAttritionScore
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

    /// <summary>Refreshes score in place.</summary>
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
