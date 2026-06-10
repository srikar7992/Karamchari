using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Karamchari.Capability.Domain.Primitives;
using Karamchari.Capability.Domain.Succession;
using Karamchari.Capability.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Capability.Services;

/// <summary>
/// Composes succession intelligence at query time from <see cref="CriticalPosition"/> and
/// <see cref="SuccessorCandidateProjection"/>. No scoring logic here — readiness is pre-computed.
/// </summary>
public sealed class SuccessionQueryService
{
    private readonly CapabilityDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="SuccessionQueryService"/> class.
    /// </summary>
    public SuccessionQueryService(CapabilityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Returns bench strength (per-band counts + risk level) for a single critical position.
    /// Returns null if the position is not designated critical or is inactive.
    /// </summary>
    public async Task<BenchStrength?> GetBenchStrengthAsync(
        string tenantId,
        Guid positionId,
        CancellationToken cancellationToken = default)
    {
        var cp = await _dbContext.CriticalPositions
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId && x.PositionId == positionId && x.IsActive,
                cancellationToken);

        if (cp is null) return null;

        var bands = await _dbContext.SuccessorCandidateProjections
            .Where(p => p.TenantId == tenantId && p.PositionId == positionId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                ReadyNow = g.Count(p => p.Band == CareerReadinessBand.ReadyNow),
                ReadySoon = g.Count(p => p.Band == CareerReadinessBand.ReadySoon),
                NeedsDevelopment = g.Count(p => p.Band == CareerReadinessBand.NeedsDevelopment),
            })
            .FirstOrDefaultAsync(cancellationToken);

        var readyNow = bands?.ReadyNow ?? 0;

        return new BenchStrength(
            positionId,
            cp.Criticality,
            readyNow,
            bands?.ReadySoon ?? 0,
            bands?.NeedsDevelopment ?? 0,
            BenchStrength.DeriveRisk(readyNow));
    }

    /// <summary>
    /// Returns succession risk summaries for all active critical positions in the tenant.
    /// Ordered by risk level descending (High first) then by criticality descending.
    /// </summary>
    public async Task<IReadOnlyList<SuccessionRiskSummary>> GetSuccessionRiskAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        var criticalPositions = await _dbContext.CriticalPositions
            .Where(cp => cp.TenantId == tenantId && cp.IsActive)
            .ToListAsync(cancellationToken);

        if (criticalPositions.Count == 0) return [];

        var positionIds = criticalPositions.Select(cp => cp.PositionId).ToList();
        var requirementIds = criticalPositions.Select(cp => cp.RoleRequirementId).Distinct().ToList();

        // Load ReadyNow counts per position
        var readyNowCounts = await _dbContext.SuccessorCandidateProjections
            .Where(p => p.TenantId == tenantId
                     && positionIds.Contains(p.PositionId)
                     && p.Band == CareerReadinessBand.ReadyNow)
            .GroupBy(p => p.PositionId)
            .Select(g => new { PositionId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PositionId, x => x.Count, cancellationToken);

        // Load role titles from requirements
        var roleTitles = await _dbContext.RoleSkillRequirements
            .Where(r => r.TenantId == tenantId && requirementIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, r => r.RoleTitle, cancellationToken);

        var summaries = criticalPositions
            .Select(cp =>
            {
                var readyNow = readyNowCounts.GetValueOrDefault(cp.PositionId, 0);
                return new SuccessionRiskSummary(
                    cp.PositionId,
                    cp.RoleRequirementId,
                    roleTitles.GetValueOrDefault(cp.RoleRequirementId, string.Empty),
                    cp.Criticality,
                    readyNow,
                    BenchStrength.DeriveRisk(readyNow));
            })
            .OrderByDescending(s => s.RiskLevel)
            .ThenByDescending(s => s.Criticality)
            .ToList();

        return summaries;
    }

    /// <summary>
    /// Returns the ranked successor candidates for a critical position, ordered by rank ascending.
    /// Returns empty list if the position is not active or has no tracked candidates.
    /// </summary>
    public async Task<IReadOnlyList<SuccessorCandidateProjection>> GetSuccessorsAsync(
        string tenantId,
        Guid positionId,
        CancellationToken cancellationToken = default)
    {
        var isActive = await _dbContext.CriticalPositions
            .AnyAsync(cp => cp.TenantId == tenantId && cp.PositionId == positionId && cp.IsActive,
                cancellationToken);

        if (!isActive) return [];

        return await _dbContext.SuccessorCandidateProjections
            .Where(p => p.TenantId == tenantId && p.PositionId == positionId)
            .OrderBy(p => p.Rank)
            .ToListAsync(cancellationToken);
    }
}
