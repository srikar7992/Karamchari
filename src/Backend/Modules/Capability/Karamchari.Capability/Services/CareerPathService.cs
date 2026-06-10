// -----------------------------------------------------------------------
// <copyright file="CareerPathService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Karamchari.Capability.Domain.Pathing;
using Karamchari.Capability.Domain.Primitives;
using Karamchari.Capability.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Capability.Services;

/// <summary>
/// Computes career path recommendations at query time from pre-built projections.
/// No runtime scoring — reads from <see cref="Domain.Skills.EmployeeSkillGapProjection"/>,
/// <see cref="Domain.Skills.CareerReadinessProjection"/>, and <see cref="CareerProgressionEdge"/> graph.
/// </summary>
public sealed class CareerPathService
{
    private readonly CapabilityDbContext _dbContext;

    public CareerPathService(CapabilityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Returns the career path from the employee's current skill state to the target role.
    /// </summary>
    public async Task<CareerPathResult?> GetCareerPathAsync(
        string tenantId,
        Guid employeeId,
        Guid targetRoleRequirementId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        // 1. Verify target role exists
        var targetRole = await _dbContext.RoleSkillRequirements
            .AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.TenantId == tenantId && r.Id == targetRoleRequirementId && r.IsActive,
                cancellationToken);

        if (targetRole is null)
            return null;

        // 2. Load current readiness for target role
        var readiness = await _dbContext.CareerReadinessProjections
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.TenantId == tenantId
                  && p.EmployeeId == employeeId
                  && p.RoleRequirementId == targetRoleRequirementId,
                cancellationToken);

        // 3. Load skill gaps for the target role
        var gapRows = await _dbContext.EmployeeSkillGapProjections
            .AsNoTracking()
            .Where(g => g.TenantId == tenantId
                     && g.EmployeeId == employeeId
                     && g.RoleRequirementId == targetRoleRequirementId)
            .ToListAsync(cancellationToken);

        // 4. Resolve skill names
        var skillIds = gapRows.Select(g => g.SkillId).ToList();
        var skillNames = skillIds.Count > 0
            ? await _dbContext.SkillDefinitions
                .AsNoTracking()
                .Where(s => s.TenantId == tenantId && skillIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken)
            : new Dictionary<Guid, string>();

        // 5. Build ordered skill roadmap
        var steps = gapRows
            .Select(g => new CareerPathStep(
                SkillId: g.SkillId,
                SkillName: skillNames.GetValueOrDefault(g.SkillId, g.SkillId.ToString()),
                RequiredLevel: g.RequiredLevel,
                CurrentLevel: g.CurrentLevel,
                LevelsBehind: g.LevelsBehind,
                IsCritical: g.LevelsBehind >= CareerReadinessCalculator.CriticalGapLevelThreshold || g.IsMissing,
                Priority: g.IsMissing ? 1 : (g.LevelsBehind >= CareerReadinessCalculator.CriticalGapLevelThreshold ? 2 : 3)))
            .OrderBy(s => s.Priority)
            .ThenByDescending(s => s.LevelsBehind)
            .ToList();

        // 6. BFS through career progression graph to find shortest path
        var path = await FindShortestPathAsync(
            tenantId, employeeId, targetRoleRequirementId, cancellationToken);

        return new CareerPathResult
        {
            EmployeeId = employeeId,
            TargetRoleRequirementId = targetRoleRequirementId,
            TargetRoleTitle = targetRole.RoleTitle,
            CurrentBand = readiness?.Band ?? CareerReadinessBand.NeedsDevelopment,
            CurrentReadinessScore = readiness?.ReadinessScore ?? 0m,
            CurrentCoveragePercent = readiness?.CoveragePercent ?? 0m,
            MissingSkillCount = gapRows.Count(g => g.IsMissing),
            CriticalGapCount = gapRows.Count(g => g.IsMissing || g.LevelsBehind >= CareerReadinessCalculator.CriticalGapLevelThreshold),
            Steps = steps,
            Path = path,
        };
    }

    private async Task<IReadOnlyList<CareerProgressionHop>> FindShortestPathAsync(
        string tenantId,
        Guid employeeId,
        Guid targetRoleRequirementId,
        CancellationToken cancellationToken)
    {
        // Origin nodes: roles where employee is ReadyNow (they are already qualified)
        var originRoleIds = await _dbContext.CareerReadinessProjections
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId
                     && p.EmployeeId == employeeId
                     && p.Band == CareerReadinessBand.ReadyNow
                     && p.RoleRequirementId != targetRoleRequirementId)
            .Select(p => p.RoleRequirementId)
            .ToListAsync(cancellationToken);

        if (originRoleIds.Count == 0)
            return [];

        // Load full edge graph for tenant (small set — role count is typically < 100)
        var edges = await _dbContext.CareerProgressionEdges
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        if (edges.Count == 0)
            return [];

        // Build adjacency list
        var adj = edges
            .GroupBy(e => e.FromRoleRequirementId)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ToRoleRequirementId).ToList());

        // BFS from all origin nodes simultaneously
        var prev = new Dictionary<Guid, Guid>();
        var visited = new HashSet<Guid>(originRoleIds);
        var queue = new Queue<Guid>(originRoleIds);

        bool found = false;
        while (queue.Count > 0 && !found)
        {
            var current = queue.Dequeue();
            if (!adj.TryGetValue(current, out var neighbors))
                continue;

            foreach (var next in neighbors)
            {
                if (visited.Contains(next))
                    continue;

                prev[next] = current;
                visited.Add(next);

                if (next == targetRoleRequirementId)
                {
                    found = true;
                    break;
                }

                queue.Enqueue(next);
            }
        }

        if (!found)
            return [];

        // Reconstruct path (target → origin, then reverse)
        var rawPath = new List<Guid>();
        var node = targetRoleRequirementId;
        while (prev.ContainsKey(node))
        {
            rawPath.Add(node);
            node = prev[node];
        }
        rawPath.Add(node); // origin node
        rawPath.Reverse();

        // Resolve role titles for path nodes
        var pathIds = rawPath.ToHashSet();
        var roleTitles = await _dbContext.RoleSkillRequirements
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId && pathIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, r => r.RoleTitle, cancellationToken);

        return rawPath
            .Select((id, idx) => new CareerProgressionHop(
                RoleRequirementId: id,
                RoleTitle: roleTitles.GetValueOrDefault(id, id.ToString()),
                StepIndex: idx))
            .ToList();
    }
}
