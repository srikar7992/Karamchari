using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Intelligence.Domain.Workforce;

/// <summary>
/// Append-only historical record written every time a burnout or attrition score is refreshed.
/// Provides the score history needed by VelocityCalculator and LinearForecastEngine.
/// One row per (employee, scoreType, calculatedAt).
/// </summary>
public sealed class WorkforceScoreSnapshot : AggregateRoot<Guid>, ITenantOwned
{
    /// <inheritdoc/>
    public string TenantId { get; private set; } = string.Empty;

    public Guid EmployeeId { get; private set; }

    /// <summary>"Burnout" or "Attrition".</summary>
    public string ScoreType { get; private set; } = string.Empty;

    public decimal Score { get; private set; }

    public WorkforceRiskLevel RiskLevel { get; private set; }

    public DateTime CalculatedAt { get; private set; }

    private WorkforceScoreSnapshot() { }

    public static WorkforceScoreSnapshot Record(
        string tenantId,
        Guid employeeId,
        string scoreType,
        decimal score,
        WorkforceRiskLevel riskLevel)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            ScoreType = scoreType,
            Score = score,
            RiskLevel = riskLevel,
            CalculatedAt = DateTime.UtcNow
        };
}
