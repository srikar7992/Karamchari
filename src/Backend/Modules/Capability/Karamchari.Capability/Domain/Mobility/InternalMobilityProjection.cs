using Karamchari.Capability.Domain.Primitives;

namespace Karamchari.Capability.Domain.Mobility;

/// <summary>
/// Pre-computed mobility score for an employee against an open vacancy.
/// PK: (TenantId, EmployeeId, VacancyId) — one row per employee-vacancy pair.
/// Row exists only while the vacancy is open; deleted when vacancy closes.
/// Enables <c>GET /positions/{id}/candidates</c> and <c>GET /employees/{id}/recommended-roles</c>
/// as single index scans with no runtime scoring.
/// </summary>
public sealed class InternalMobilityProjection
{
    private InternalMobilityProjection() { }

    public string TenantId { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }
    public Guid VacancyId { get; private set; }

    public Guid PositionId { get; private set; }
    public Guid RoleRequirementId { get; private set; }

    public decimal CoveragePercent { get; private set; }
    public decimal ReadinessScore { get; private set; }
    public CareerReadinessBand Band { get; private set; }

    /// <summary>Count of skills where employee has no verified level at all (IsMissing == true).</summary>
    public int MissingSkillCount { get; private set; }

    /// <summary>Count of skills that are missing or two or more levels behind requirement.</summary>
    public int CriticalGapCount { get; private set; }

    /// <summary>True when Band is ReadyNow or ReadySoon — employee can enter the mobility pool.</summary>
    public bool Eligible { get; private set; }

    public DateTimeOffset LastUpdatedUtc { get; private set; }

    public static InternalMobilityProjection Create(
        string tenantId,
        Guid employeeId,
        Guid vacancyId,
        Guid positionId,
        Guid roleRequirementId,
        decimal coveragePercent,
        decimal readinessScore,
        CareerReadinessBand band,
        int missingSkillCount,
        int criticalGapCount)
    {
        return new InternalMobilityProjection
        {
            TenantId = tenantId,
            EmployeeId = employeeId,
            VacancyId = vacancyId,
            PositionId = positionId,
            RoleRequirementId = roleRequirementId,
            CoveragePercent = coveragePercent,
            ReadinessScore = readinessScore,
            Band = band,
            MissingSkillCount = missingSkillCount,
            CriticalGapCount = criticalGapCount,
            Eligible = band != CareerReadinessBand.NeedsDevelopment,
            LastUpdatedUtc = DateTimeOffset.UtcNow,
        };
    }

    public void Update(
        decimal coveragePercent,
        decimal readinessScore,
        CareerReadinessBand band,
        int missingSkillCount,
        int criticalGapCount)
    {
        CoveragePercent = coveragePercent;
        ReadinessScore = readinessScore;
        Band = band;
        MissingSkillCount = missingSkillCount;
        CriticalGapCount = criticalGapCount;
        Eligible = band != CareerReadinessBand.NeedsDevelopment;
        LastUpdatedUtc = DateTimeOffset.UtcNow;
    }
}
