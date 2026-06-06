using Karamchari.Core.Multitenancy;

namespace Karamchari.Intelligence.Domain.Workforce;

/// <summary>
/// Measures business-continuity exposure from a single employee's departure.
/// Distinct from attrition risk (likelihood of leaving) — this answers
/// "how much damage occurs if they leave?"
///
/// Upserted nightly. Maps to Intel_DependencyScores.
/// </summary>
public sealed class EmployeeDependencyScore : ITenantOwned
{
    public Guid Id { get; private set; }

    /// <inheritdoc/>
    public string TenantId { get; private set; } = string.Empty;

    public Guid EmployeeId { get; private set; }

    /// <summary>Composite dependency risk score [0, 100]. Higher = harder to replace.</summary>
    public decimal Score { get; private set; }

    public WorkforceRiskLevel RiskLevel { get; private set; }

    /// <summary>Contribution from fraction of critical shifts owned [0–40].</summary>
    public decimal CriticalShiftComponent { get; private set; }

    /// <summary>Contribution from inverse cross-training depth [0–25].</summary>
    public decimal CrossTrainingComponent { get; private set; }

    /// <summary>Contribution from unique-role flag [0–25].</summary>
    public decimal UniqueRoleComponent { get; private set; }

    /// <summary>Contribution from approval authority held [0–10].</summary>
    public decimal ApprovalComponent { get; private set; }

    public DateTime ComputedAt { get; private set; }

    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    private EmployeeDependencyScore() { }

    public static EmployeeDependencyScore Create(
        string tenantId,
        Guid employeeId,
        decimal score,
        WorkforceRiskLevel riskLevel,
        decimal criticalShiftComponent,
        decimal crossTrainingComponent,
        decimal uniqueRoleComponent,
        decimal approvalComponent)
    {
        return new EmployeeDependencyScore
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            Score = score,
            RiskLevel = riskLevel,
            CriticalShiftComponent = criticalShiftComponent,
            CrossTrainingComponent = crossTrainingComponent,
            UniqueRoleComponent = uniqueRoleComponent,
            ApprovalComponent = approvalComponent,
            ComputedAt = DateTime.UtcNow
        };
    }

    public void Refresh(
        decimal score,
        WorkforceRiskLevel riskLevel,
        decimal criticalShiftComponent,
        decimal crossTrainingComponent,
        decimal uniqueRoleComponent,
        decimal approvalComponent)
    {
        Score = score;
        RiskLevel = riskLevel;
        CriticalShiftComponent = criticalShiftComponent;
        CrossTrainingComponent = crossTrainingComponent;
        UniqueRoleComponent = uniqueRoleComponent;
        ApprovalComponent = approvalComponent;
        ComputedAt = DateTime.UtcNow;
    }
}
