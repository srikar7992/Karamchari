using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Karamchari.Compensation.Contracts.IntegrationEvents;
using Karamchari.Core.Contracts.IntegrationEvents;
using Karamchari.HR.Domain.Organization;
using Karamchari.HR.Domain.Organization.Events;
using Karamchari.HR.Domain.Organization.Projections;
using Karamchari.HR.Persistence;
using Karamchari.HR.Projections;
using Karamchari.HR.Services;
using Karamchari.HR.Tests.Integration;
using Karamchari.Performance.Contracts.IntegrationEvents;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Karamchari.HR.Tests.Domain;

public sealed class WorkforceIntelligenceTests
{
    private const string TenantA = "tenant-a";
    private const string TenantB = "tenant-b";

    [Fact]
    public void RetentionRiskCalculator_BaseScore_IsCorrect()
    {
        var employeeId = Guid.NewGuid();

        // When all inputs are null / within normal ranges
        var risk = RetentionRiskCalculator.Calculate(TenantA, employeeId, null, null, null, null);

        risk.RiskScore.Should().Be(0.05m); // base friction score
        risk.Drivers.Should().BeEmpty();
    }

    [Fact]
    public void RetentionRiskCalculator_AccumulatesFactorsAndCaps()
    {
        var employeeId = Guid.NewGuid();

        // 1. High tenure: monthsInRole = 25 (> 24) -> +0.30
        // 2. Recent manager change: monthsSinceManagerChange = 5 (< 6) -> +0.20
        // 3. Low comp increment: comp.IncrementPercent = 0.02 (< 0.03) -> +0.20
        // 4. Low performance CompositeScore = 2.5 (< 3.0) -> +0.20
        // Base score = 0.05. Expected total = 0.95
        var comp = new EmployeeCompensationProjection
        {
            TenantId = TenantA,
            EmployeeId = employeeId,
            IncrementPercent = 0.02m
        };

        var perf = new EmployeePerformanceProjection
        {
            TenantId = TenantA,
            EmployeeId = employeeId,
            CompositeScore = 2.5m,
            IsHighPerformer = false
        };

        var risk = RetentionRiskCalculator.Calculate(TenantA, employeeId, 25, 5, comp, perf);

        risk.RiskScore.Should().Be(0.95m);
        risk.Drivers.Should().HaveCount(4);
        risk.Drivers.Any(d => d.DriverType == FlightRiskDriverType.HighTenureInRole).Should().BeTrue();
        risk.Drivers.Any(d => d.DriverType == FlightRiskDriverType.RecentManagerChange).Should().BeTrue();
        risk.Drivers.Any(d => d.DriverType == FlightRiskDriverType.LowCompRange).Should().BeTrue();
        risk.Drivers.Any(d => d.DriverType == FlightRiskDriverType.LowPerformanceTrend).Should().BeTrue();
    }

    [Fact]
    public void PromotionReadinessCalculator_BaseScore_IsCorrect()
    {
        var employeeId = Guid.NewGuid();

        var readiness = PromotionReadinessCalculator.Calculate(TenantA, employeeId, null, 0, null);

        readiness.ReadinessScore.Should().Be(0.10m); // base readiness score
        readiness.Drivers.Should().BeEmpty();
    }

    [Fact]
    public void PromotionReadinessCalculator_AccumulatesFactorsAndCaps()
    {
        var employeeId = Guid.NewGuid();

        // 1. development window milestone: monthsInRole = 13 (> 12) -> +0.35
        // 2. skill milestones met: skillCount = 3 (>= 3) -> +0.25
        // 3. high performer: IsHighPerformer = true -> +0.25
        // Base score = 0.10. Expected total = 0.95
        var perf = new EmployeePerformanceProjection
        {
            TenantId = TenantA,
            EmployeeId = employeeId,
            CompositeScore = 4.5m,
            IsHighPerformer = true
        };

        var readiness = PromotionReadinessCalculator.Calculate(TenantA, employeeId, 13, 3, perf);

        readiness.ReadinessScore.Should().Be(0.95m);
        readiness.Drivers.Should().HaveCount(3);
        readiness.Drivers.Any(d => d.DriverType == PromotionReadinessDriverType.RoleTenureMilestone).Should().BeTrue();
        readiness.Drivers.Any(d => d.DriverType == PromotionReadinessDriverType.SkillMilestonesMet).Should().BeTrue();
        readiness.Drivers.Any(d => d.DriverType == PromotionReadinessDriverType.HighGoalVelocity).Should().BeTrue();
    }

    [Fact]
    public async Task IntelligenceProjectionHandler_SkillValidated_AddsGraphElementsAndCalculates()
    {
        var dbName = Guid.NewGuid().ToString();
        using var dbContext = HRDbContextFactory.Create(TenantA, dbName);
        var handler = new EmployeeIntelligenceProjectionHandler(dbContext, new Karamchari.Core.Observability.KaramchariProjectionMetrics());

        var employeeId = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        // Seed Performance Projection (for high performer check)
        var perf = new EmployeePerformanceProjection
        {
            TenantId = TenantA,
            EmployeeId = employeeId,
            CompositeScore = 4.0m,
            IsHighPerformer = true,
            PerformanceBucket = "High"
        };
        dbContext.EmployeePerfProjections.Add(perf);
        await dbContext.SaveChangesAsync();

        // Act: Validate 3 skills sequentially
        for (int i = 0; i < 3; i++)
        {
            var sId = i == 0 ? skillId : Guid.NewGuid();
            var skillEvent = new SkillValidatedIntegrationEvent(
                employeeId,
                TenantA,
                sId,
                $"SKILL-00{i}",
                $"Skill {i}",
                3,
                DateTimeOffset.UtcNow);

            await handler.HandleAsync(skillEvent, CancellationToken.None);
        }

        // Assert: Verify graph elements
        var skillNode = await dbContext.GraphNodes.FindAsync(TenantA, skillId);
        skillNode.Should().NotBeNull();
        skillNode!.Type.Should().Be(WorkforceNodeType.Skill);

        var skillEdge = await dbContext.GraphEdges.FirstOrDefaultAsync(
            e => e.TenantId == TenantA && e.SourceNodeId == employeeId && e.TargetNodeId == skillId && e.Type == WorkforceEdgeType.HasSkill);
        skillEdge.Should().NotBeNull();
        skillEdge!.IsActive.Should().BeTrue();

        var graphEdges = await dbContext.GraphEdges.Where(e => e.TenantId == TenantA && e.SourceNodeId == employeeId && e.Type == WorkforceEdgeType.HasSkill).ToListAsync();
        graphEdges.Should().HaveCount(3);

        // Assert: Verify promotion readiness prediction recalculation
        var prediction = await dbContext.PromotionReadinessPredictions
            .Include(p => p.Drivers)
            .FirstOrDefaultAsync(p => p.TenantId == TenantA && p.EmployeeId == employeeId);

        prediction.Should().NotBeNull();
        prediction!.ReadinessScore.Should().BeGreaterThan(0.10m);
        prediction.Drivers.Any(d => d.DriverType == PromotionReadinessDriverType.SkillMilestonesMet).Should().BeTrue();
    }

    [Fact]
    public async Task IntelligenceProjectionHandler_ReportingRelationshipChanged_Calculates()
    {
        var dbName = Guid.NewGuid().ToString();
        using var dbContext = HRDbContextFactory.Create(TenantA, dbName);
        var handler = new EmployeeIntelligenceProjectionHandler(dbContext, new Karamchari.Core.Observability.KaramchariProjectionMetrics());

        var employeeId = Guid.NewGuid();
        var positionId = Guid.NewGuid();

        // Seed occupies edge in graph
        var occupies = new WorkforceGraphEdge
        {
            Id = Guid.NewGuid(),
            TenantId = TenantA,
            SourceNodeId = employeeId,
            TargetNodeId = positionId,
            Type = WorkforceEdgeType.Occupies,
            IsActive = true,
            EffectiveFrom = DateTimeOffset.UtcNow.AddMonths(-3)
        };
        dbContext.GraphEdges.Add(occupies);

        // Seed occupant projection
        var occupant = new PositionOccupantProjection
        {
            TenantId = TenantA,
            PositionId = positionId,
            EmployeeId = employeeId,
            AssignmentId = Guid.NewGuid(),
            EmployeeName = "Jane Doe"
        };
        dbContext.PositionOccupantProjections.Add(occupant);
        await dbContext.SaveChangesAsync();

        // Act: Fire ReportingRelationshipChanged event for positionId
        var changedEvent = new ReportingRelationshipChanged(
            Guid.NewGuid(),
            TenantA,
            positionId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        await handler.HandleAsync(changedEvent, CancellationToken.None);

        // Assert: Recalculates risk for employee
        var risk = await dbContext.FlightRiskPredictions
            .FirstOrDefaultAsync(p => p.TenantId == TenantA && p.EmployeeId == employeeId);

        risk.Should().NotBeNull();
    }

    [Fact]
    public async Task IntelligenceProjectionHandler_RepeatedUpdates_DoesNotCauseDuplicationErrors()
    {
        var dbName = Guid.NewGuid().ToString();
        using var dbContext = HRDbContextFactory.Create(TenantA, dbName);
        var handler = new EmployeeIntelligenceProjectionHandler(dbContext, new Karamchari.Core.Observability.KaramchariProjectionMetrics());

        var employeeId = Guid.NewGuid();
        var positionId = Guid.NewGuid();

        // First execution creates prediction & drivers
        var assignEvent1 = new PositionAssignmentCreated(Guid.NewGuid(), TenantA, positionId, employeeId, DateTimeOffset.UtcNow);
        await handler.HandleAsync(assignEvent1, CancellationToken.None);

        var risk1 = await dbContext.FlightRiskPredictions
            .Include(p => p.Drivers)
            .FirstOrDefaultAsync(p => p.TenantId == TenantA && p.EmployeeId == employeeId);
        risk1.Should().NotBeNull();
        var countBefore = risk1!.Drivers.Count;

        // Second execution should do an in-place update without duplicates or primary key violations
        var assignEvent2 = new PositionAssignmentEnded(Guid.NewGuid(), TenantA, positionId, employeeId, DateTimeOffset.UtcNow);
        await handler.HandleAsync(assignEvent2, CancellationToken.None);

        var risk2 = await dbContext.FlightRiskPredictions
            .Include(p => p.Drivers)
            .FirstOrDefaultAsync(p => p.TenantId == TenantA && p.EmployeeId == employeeId);
        risk2.Should().NotBeNull();
        risk2!.Drivers.Count.Should().Be(countBefore);
    }

    [Fact]
    public async Task IntelligenceProjectionHandler_EnforcesTenantIsolation()
    {
        var dbName = Guid.NewGuid().ToString();
        using var dbContextA = HRDbContextFactory.Create(TenantA, dbName);
        using var dbContextB = HRDbContextFactory.Create(TenantB, dbName);

        var employeeId = Guid.NewGuid();
        var positionId = Guid.NewGuid();

        // Seed Tenant A initial prediction
        var existingA = new EmployeeFlightRiskPrediction
        {
            TenantId = TenantA,
            EmployeeId = employeeId,
            RiskScore = 0.50m,
            LastUpdatedUtc = DateTimeOffset.UtcNow.AddDays(-1)
        };
        dbContextA.FlightRiskPredictions.Add(existingA);
        await dbContextA.SaveChangesAsync();

        // Fire event for Tenant B using Tenant B's DbContext handler
        var handlerB = new EmployeeIntelligenceProjectionHandler(dbContextB, new Karamchari.Core.Observability.KaramchariProjectionMetrics());
        var assignEventB = new PositionAssignmentCreated(Guid.NewGuid(), TenantB, positionId, employeeId, DateTimeOffset.UtcNow);
        await handlerB.HandleAsync(assignEventB, CancellationToken.None);

        // Verify Tenant B got the prediction
        var riskB = await dbContextB.FlightRiskPredictions.FindAsync(TenantB, employeeId);
        riskB.Should().NotBeNull();

        // Verify Tenant A's prediction remained isolated and completely untouched
        var riskA = await dbContextA.FlightRiskPredictions.FindAsync(TenantA, employeeId);
        riskA.Should().NotBeNull();
        riskA!.RiskScore.Should().Be(0.50m); // Unchanged
    }
}
