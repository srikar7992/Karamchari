// -----------------------------------------------------------------------
// <copyright file="EmployeeIntelligenceProjectionHandler.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Karamchari.Compensation.Contracts.IntegrationEvents;
using Karamchari.Core.Contracts.IntegrationEvents;
using Karamchari.Core.Observability;
using Karamchari.Core.Projections;
using Karamchari.HR.Domain.Organization;
using Karamchari.HR.Domain.Organization.Events;
using Karamchari.HR.Domain.Organization.Projections;
using Karamchari.HR.Persistence;
using Karamchari.Performance.Contracts.IntegrationEvents;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.HR.Projections;

/// <summary>
/// Projection handler that recalculates retention risk and promotion readiness scores
/// for employees when assignment, reporting lines, compensation, performance, or skill validation events occur.
/// </summary>
public sealed class EmployeeIntelligenceProjectionHandler
    : IProjectionHandler<PositionAssignmentCreated>,
      IProjectionHandler<PositionAssignmentEnded>,
      IProjectionHandler<ReportingRelationshipCreated>,
      IProjectionHandler<ReportingRelationshipEnded>,
      IProjectionHandler<EmployeeCompensationRevisedIntegrationEvent>,
      IProjectionHandler<EmployeePerformanceSnapshotMaterializedIntegrationEvent>,
      IProjectionHandler<SkillValidatedIntegrationEvent>,
      IProjectionHandler<ReportingRelationshipChanged>
{
    private readonly HRDbContext _dbContext;
    private readonly KaramchariProjectionMetrics _projectionMetrics;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmployeeIntelligenceProjectionHandler"/> class.
    /// </summary>
    /// <param name="dbContext">The database context for HR module persistence.</param>
    /// <param name="projectionMetrics">Projection and prediction recalculation metrics collector.</param>
    public EmployeeIntelligenceProjectionHandler(
        HRDbContext dbContext,
        KaramchariProjectionMetrics projectionMetrics)
    {
        _dbContext = dbContext;
        _projectionMetrics = projectionMetrics;
    }

    /// <inheritdoc/>
    public async Task HandleAsync(PositionAssignmentCreated domainEvent, CancellationToken cancellationToken)
    {
        await RecalculateAllPredictionsAsync(domainEvent.TenantId, domainEvent.EmployeeId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task HandleAsync(PositionAssignmentEnded domainEvent, CancellationToken cancellationToken)
    {
        await RecalculateAllPredictionsAsync(domainEvent.TenantId, domainEvent.EmployeeId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task HandleAsync(ReportingRelationshipCreated domainEvent, CancellationToken cancellationToken)
    {
        var occupant = await _dbContext.PositionOccupantProjections.AsNoTracking()
            .FirstOrDefaultAsync(o => o.TenantId == domainEvent.TenantId && o.PositionId == domainEvent.DirectReportPositionId, cancellationToken);
        if (occupant != null)
        {
            await RecalculateAllPredictionsAsync(domainEvent.TenantId, occupant.EmployeeId, cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task HandleAsync(ReportingRelationshipEnded domainEvent, CancellationToken cancellationToken)
    {
        var occupant = await _dbContext.PositionOccupantProjections.AsNoTracking()
            .FirstOrDefaultAsync(o => o.TenantId == domainEvent.TenantId && o.PositionId == domainEvent.DirectReportPositionId, cancellationToken);
        if (occupant != null)
        {
            await RecalculateAllPredictionsAsync(domainEvent.TenantId, occupant.EmployeeId, cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task HandleAsync(EmployeeCompensationRevisedIntegrationEvent domainEvent, CancellationToken cancellationToken)
    {
        await RecalculateAllPredictionsAsync(domainEvent.TenantId, domainEvent.EmployeeId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task HandleAsync(EmployeePerformanceSnapshotMaterializedIntegrationEvent domainEvent, CancellationToken cancellationToken)
    {
        await RecalculateAllPredictionsAsync(domainEvent.TenantId, domainEvent.EmployeeId, cancellationToken);
    }

    /// <summary>
    /// Handles the reporting relationship changed event by identifying the occupant of the report's position 
    /// and triggering their prediction score recalculation.
    /// </summary>
    public async Task HandleAsync(ReportingRelationshipChanged domainEvent, CancellationToken cancellationToken)
    {
        var occupant = await _dbContext.PositionOccupantProjections.AsNoTracking()
            .FirstOrDefaultAsync(o => o.TenantId == domainEvent.TenantId && o.PositionId == domainEvent.DirectReportPositionId, cancellationToken);
        if (occupant != null)
        {
            await RecalculateAllPredictionsAsync(domainEvent.TenantId, occupant.EmployeeId, cancellationToken);
        }
    }

    /// <summary>
    /// Handles the skill validation event by ensuring the corresponding skill and employee nodes exist in the graph, 
    /// upserting a 'HasSkill' graph edge, and triggering prediction score recalculation.
    /// </summary>
    public async Task HandleAsync(SkillValidatedIntegrationEvent domainEvent, CancellationToken cancellationToken)
    {
        // 1. Ensure the Skill node exists in GraphNodes to avoid FK issues
        var skillNodeExists = await _dbContext.GraphNodes.AnyAsync(
            n => n.TenantId == domainEvent.TenantId && n.Id == domainEvent.SkillId, cancellationToken);
        if (!skillNodeExists)
        {
            _dbContext.GraphNodes.Add(new WorkforceGraphNode
            {
                TenantId = domainEvent.TenantId,
                Id = domainEvent.SkillId,
                Type = WorkforceNodeType.Skill,
                DisplayName = domainEvent.SkillName
            });
        }

        // 2. Ensure the Employee node exists in GraphNodes to avoid FK issues
        var employeeNodeExists = await _dbContext.GraphNodes.AnyAsync(
            n => n.TenantId == domainEvent.TenantId && n.Id == domainEvent.EmployeeId, cancellationToken);
        if (!employeeNodeExists)
        {
            var emp = await _dbContext.Employees.AsNoTracking()
                .FirstOrDefaultAsync(e => e.TenantId == domainEvent.TenantId && e.Id == domainEvent.EmployeeId, cancellationToken);
            _dbContext.GraphNodes.Add(new WorkforceGraphNode
            {
                TenantId = domainEvent.TenantId,
                Id = domainEvent.EmployeeId,
                Type = WorkforceNodeType.Employee,
                DisplayName = emp?.LegalName ?? $"Employee {domainEvent.EmployeeId}"
            });
        }

        // 3. Upsert the HasSkill Edge in GraphEdges
        var edge = await _dbContext.GraphEdges
            .FirstOrDefaultAsync(e => e.TenantId == domainEvent.TenantId
                                      && e.SourceNodeId == domainEvent.EmployeeId
                                      && e.TargetNodeId == domainEvent.SkillId
                                      && e.Type == WorkforceEdgeType.HasSkill, cancellationToken);
        if (edge != null)
        {
            edge.IsActive = true;
            edge.EffectiveFrom = domainEvent.ValidatedAt;
        }
        else
        {
            _dbContext.GraphEdges.Add(new WorkforceGraphEdge
            {
                Id = Guid.NewGuid(),
                TenantId = domainEvent.TenantId,
                SourceNodeId = domainEvent.EmployeeId,
                TargetNodeId = domainEvent.SkillId,
                Type = WorkforceEdgeType.HasSkill,
                EffectiveFrom = domainEvent.ValidatedAt,
                IsActive = true
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // 4. Recalculate predictions
        await RecalculateAllPredictionsAsync(domainEvent.TenantId, domainEvent.EmployeeId, cancellationToken);
    }


    private async Task RecalculateAllPredictionsAsync(string tenantId, Guid employeeId, CancellationToken cancellationToken)
    {
        // 1. Fetch related graph telemetry and input read model metrics
        double? monthsInRole = null;
        double? monthsSinceManagerChange = null;

        var occupiesEdge = await _dbContext.GraphEdges.AsNoTracking()
            .FirstOrDefaultAsync(e => e.TenantId == tenantId && e.SourceNodeId == employeeId && e.Type == WorkforceEdgeType.Occupies && e.IsActive, cancellationToken);

        if (occupiesEdge?.EffectiveFrom.HasValue == true)
        {
            monthsInRole = (DateTimeOffset.UtcNow - occupiesEdge.EffectiveFrom.Value).TotalDays / 30.0;

            var managerEdge = await _dbContext.GraphEdges.AsNoTracking()
                .FirstOrDefaultAsync(e => e.TenantId == tenantId && e.SourceNodeId == occupiesEdge.TargetNodeId && e.Type == WorkforceEdgeType.ReportsTo && e.IsActive, cancellationToken);

            if (managerEdge?.EffectiveFrom.HasValue == true)
            {
                monthsSinceManagerChange = (DateTimeOffset.UtcNow - managerEdge.EffectiveFrom.Value).TotalDays / 30.0;
            }
        }

        var skillCount = await _dbContext.GraphEdges.AsNoTracking()
            .CountAsync(e => e.TenantId == tenantId && e.SourceNodeId == employeeId && e.Type == WorkforceEdgeType.HasSkill && e.IsActive, cancellationToken);

        var comp = await _dbContext.EmployeeCompProjections.AsNoTracking()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.EmployeeId == employeeId, cancellationToken);

        var perf = await _dbContext.EmployeePerfProjections.AsNoTracking()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.EmployeeId == employeeId, cancellationToken);

        // 2. Compute predictions using pure scoring calculators
        var calculatedRisk = Services.RetentionRiskCalculator.Calculate(tenantId, employeeId, monthsInRole, monthsSinceManagerChange, comp, perf);
        var calculatedReadiness = Services.PromotionReadinessCalculator.Calculate(tenantId, employeeId, monthsInRole, skillCount, perf);

        // 3. Upsert Flight Risk Predictions in-place to prevent duplicate keys
        var existingRisk = await _dbContext.FlightRiskPredictions
            .Include(p => p.Drivers)
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.EmployeeId == employeeId, cancellationToken);

        if (existingRisk != null)
        {
            existingRisk.RiskScore = calculatedRisk.RiskScore;
            existingRisk.LastUpdatedUtc = calculatedRisk.LastUpdatedUtc;

            // In-place updates of child drivers to avoid EF Core tracked duplicate key errors
            var toRemove = existingRisk.Drivers.Where(d => !calculatedRisk.Drivers.Any(c => c.DriverType == d.DriverType)).ToList();
            foreach (var r in toRemove)
            {
                existingRisk.Drivers.Remove(r);
            }

            foreach (var driver in calculatedRisk.Drivers)
            {
                var existingDriver = existingRisk.Drivers.FirstOrDefault(d => d.DriverType == driver.DriverType);
                if (existingDriver != null)
                {
                    existingDriver.FactorScore = driver.FactorScore;
                    existingDriver.Explanation = driver.Explanation;
                }
                else
                {
                    existingRisk.Drivers.Add(driver);
                }
            }
        }
        else
        {
            _dbContext.FlightRiskPredictions.Add(calculatedRisk);
        }

        // 4. Upsert Promotion Readiness Predictions in-place
        var existingReadiness = await _dbContext.PromotionReadinessPredictions
            .Include(p => p.Drivers)
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.EmployeeId == employeeId, cancellationToken);

        if (existingReadiness != null)
        {
            existingReadiness.ReadinessScore = calculatedReadiness.ReadinessScore;
            existingReadiness.LastUpdatedUtc = calculatedReadiness.LastUpdatedUtc;

            var toRemove = existingReadiness.Drivers.Where(d => !calculatedReadiness.Drivers.Any(c => c.DriverType == d.DriverType)).ToList();
            foreach (var r in toRemove)
            {
                existingReadiness.Drivers.Remove(r);
            }

            foreach (var driver in calculatedReadiness.Drivers)
            {
                var existingDriver = existingReadiness.Drivers.FirstOrDefault(d => d.DriverType == driver.DriverType);
                if (existingDriver != null)
                {
                    existingDriver.FactorScore = driver.FactorScore;
                    existingDriver.Explanation = driver.Explanation;
                }
                else
                {
                    existingReadiness.Drivers.Add(driver);
                }
            }
        }
        else
        {
            _dbContext.PromotionReadinessPredictions.Add(calculatedReadiness);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _projectionMetrics.RecordPredictionRecalculation("FlightRisk", tenantId);
        _projectionMetrics.RecordPredictionRecalculation("PromotionReadiness", tenantId);
    }
}
