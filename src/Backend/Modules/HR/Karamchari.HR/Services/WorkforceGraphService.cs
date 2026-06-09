using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Karamchari.HR.Domain.Organization;
using Karamchari.HR.Domain.Organization.Projections;
using Karamchari.HR.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.HR.Services;

/// <summary>
/// Implements the workforce graph service querying relationships and hierarchical reports 
/// directly from the database using recursive CTEs.
/// </summary>
public sealed class WorkforceGraphService : IWorkforceGraphService
{
    private readonly HRDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkforceGraphService"/> class.
    /// </summary>
    /// <param name="dbContext">The database context for HR module persistence.</param>
    public WorkforceGraphService(HRDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<WorkforceGraphNode>> GetRelatedNodesAsync(
        string tenantId,
        Guid sourceNodeId,
        WorkforceEdgeType edgeType,
        CancellationToken cancellationToken = default)
    {
        var targetIds = await _dbContext.GraphEdges
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId && e.SourceNodeId == sourceNodeId && e.Type == edgeType && e.IsActive)
            .Select(e => e.TargetNodeId)
            .ToListAsync(cancellationToken);

        return await _dbContext.GraphNodes
            .AsNoTracking()
            .Where(n => n.TenantId == tenantId && targetIds.Contains(n.Id))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Guid>> GetIndirectReportsAsync(
        string tenantId,
        Guid managerPositionId,
        CancellationToken cancellationToken = default)
    {
        // For testing environments using in-memory databases, fall back to in-memory BFS queue traversal
        if (_dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            var allDirectsAndIndirects = new HashSet<Guid>();
            var queue = new Queue<Guid>();
            queue.Enqueue(managerPositionId);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var directs = await _dbContext.GraphEdges
                    .AsNoTracking()
                    .Where(e => e.TenantId == tenantId && e.TargetNodeId == current && e.Type == WorkforceEdgeType.ReportsTo && e.IsActive)
                    .Select(e => e.SourceNodeId)
                    .ToListAsync(cancellationToken);

                foreach (var direct in directs)
                {
                    if (allDirectsAndIndirects.Add(direct))
                    {
                        queue.Enqueue(direct);
                    }
                }
            }
            return allDirectsAndIndirects;
        }

        // Production recursive CTE query with safety MAXRECURSION option to prevent infinite loops from hierarchy cycles
        var query = @"
            WITH ReportingHierarchy AS (
                -- Anchor Member
                SELECT SourceNodeId
                FROM HR_GraphEdges
                WHERE TenantId = {0} AND TargetNodeId = {1} AND Type = 'ReportsTo' AND IsActive = 1
                
                UNION ALL
                
                -- Recursive Member
                SELECT child.SourceNodeId
                FROM HR_GraphEdges child
                INNER JOIN ReportingHierarchy parent ON child.TargetNodeId = parent.SourceNodeId
                WHERE child.TenantId = {0} AND child.Type = 'ReportsTo' AND child.IsActive = 1
            )
            SELECT DISTINCT SourceNodeId FROM ReportingHierarchy
            OPTION (MAXRECURSION 100)";

        return await _dbContext.Database
            .SqlQueryRaw<Guid>(query, tenantId, managerPositionId)
            .ToListAsync(cancellationToken);
    }
}
