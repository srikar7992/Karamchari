using Karamchari.Core.Multitenancy;

namespace Karamchari.Intelligence.Domain.Workforce;

/// <summary>
/// Upstream signal that predicts burnout by measuring schedule ergonomic quality.
/// Quality score is inverted risk: 100 = perfect ergonomics, 0 = severely degraded.
/// PoorScheduleRiskLevel translates quality into a standard WorkforceRiskLevel
/// so it can be compared with burnout risk tiers.
///
/// Upserted nightly. Maps to Intel_ScheduleQualityScores.
/// </summary>
public sealed class EmployeeScheduleQuality : ITenantOwned
{
    public Guid Id { get; private set; }

    /// <inheritdoc/>
    public string TenantId { get; private set; } = string.Empty;

    public Guid EmployeeId { get; private set; }

    /// <summary>Overall schedule quality [0, 100]. Higher = better ergonomics.</summary>
    public decimal QualityScore { get; private set; }

    /// <summary>Risk level derived from quality (inverted): Low quality → High/Critical risk.</summary>
    public WorkforceRiskLevel PoorScheduleRiskLevel { get; private set; }

    /// <summary>Stress points from night→morning transition pattern [0–35].</summary>
    public decimal RotationStressComponent { get; private set; }

    /// <summary>Stress points from insufficient rest gaps [0–35].</summary>
    public decimal RestViolationComponent { get; private set; }

    /// <summary>Stress points from weekend clustering [0–15].</summary>
    public decimal WeekendClusterComponent { get; private set; }

    /// <summary>Stress points from split shifts [0–15].</summary>
    public decimal SplitShiftComponent { get; private set; }

    public DateTime ComputedAt { get; private set; }

    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    private EmployeeScheduleQuality() { }

    public static EmployeeScheduleQuality Create(
        string tenantId,
        Guid employeeId,
        decimal qualityScore,
        WorkforceRiskLevel poorScheduleRiskLevel,
        decimal rotationStressComponent,
        decimal restViolationComponent,
        decimal weekendClusterComponent,
        decimal splitShiftComponent)
    {
        return new EmployeeScheduleQuality
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            QualityScore = qualityScore,
            PoorScheduleRiskLevel = poorScheduleRiskLevel,
            RotationStressComponent = rotationStressComponent,
            RestViolationComponent = restViolationComponent,
            WeekendClusterComponent = weekendClusterComponent,
            SplitShiftComponent = splitShiftComponent,
            ComputedAt = DateTime.UtcNow
        };
    }

    public void Refresh(
        decimal qualityScore,
        WorkforceRiskLevel poorScheduleRiskLevel,
        decimal rotationStressComponent,
        decimal restViolationComponent,
        decimal weekendClusterComponent,
        decimal splitShiftComponent)
    {
        QualityScore = qualityScore;
        PoorScheduleRiskLevel = poorScheduleRiskLevel;
        RotationStressComponent = rotationStressComponent;
        RestViolationComponent = restViolationComponent;
        WeekendClusterComponent = weekendClusterComponent;
        SplitShiftComponent = splitShiftComponent;
        ComputedAt = DateTime.UtcNow;
    }
}
