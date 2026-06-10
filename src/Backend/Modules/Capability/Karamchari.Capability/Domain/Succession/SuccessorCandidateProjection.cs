using System;
using Karamchari.Capability.Domain.Primitives;

namespace Karamchari.Capability.Domain.Succession;

/// <summary>
/// Pre-computed successor readiness for one employee against one critical position.
/// Populated and ranked by <see cref="Karamchari.Capability.Projections.SuccessorCandidateProjectionHandler"/>
/// whenever a skill is validated for an employee whose profile overlaps a critical position's role requirement.
/// PK: (TenantId, PositionId, EmployeeId).
/// Rank is ordinal within the position, 1 = highest ReadinessScore.
/// </summary>
public sealed class SuccessorCandidateProjection
{
    private SuccessorCandidateProjection() { }

    public string TenantId { get; private set; } = string.Empty;
    public Guid PositionId { get; private set; }
    public Guid EmployeeId { get; private set; }

    public decimal ReadinessScore { get; private set; }
    public CareerReadinessBand Band { get; private set; }

    /// <summary>Ordinal rank within the position bench, 1 = highest score. Updated on every re-rank.</summary>
    public int Rank { get; private set; }

    public DateTimeOffset LastUpdatedUtc { get; private set; }

    /// <summary>Creates a new projection row for this employee-position pair.</summary>
    public static SuccessorCandidateProjection Create(
        string tenantId,
        Guid positionId,
        Guid employeeId,
        decimal readinessScore,
        CareerReadinessBand band,
        int rank)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        return new SuccessorCandidateProjection
        {
            TenantId = tenantId,
            PositionId = positionId,
            EmployeeId = employeeId,
            ReadinessScore = readinessScore,
            Band = band,
            Rank = rank,
            LastUpdatedUtc = DateTimeOffset.UtcNow,
        };
    }

    /// <summary>Updates readiness values; rank is updated separately by <see cref="ApplyRank"/>.</summary>
    public void Update(decimal readinessScore, CareerReadinessBand band)
    {
        ReadinessScore = readinessScore;
        Band = band;
        LastUpdatedUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Writes the new ordinal rank after a full bench re-sort.</summary>
    public void ApplyRank(int rank) => Rank = rank;
}
