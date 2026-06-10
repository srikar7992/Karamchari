// -----------------------------------------------------------------------
// <copyright file="OrganizationProjectionTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Karamchari.Core.Multitenancy;
using Karamchari.HR.Domain.Departments;
using Karamchari.HR.Domain.Employees;
using Karamchari.HR.Domain.Organization;
using Karamchari.HR.Domain.Organization.Events;
using Karamchari.HR.Domain.Organization.Projections;
using Karamchari.HR.Persistence;
using Karamchari.HR.Projections;
using Karamchari.HR.Tests.Integration;
using Karamchari.Succession.Domain;
using Karamchari.Succession.Domain.Events;
using Karamchari.Succession.Domain.Projections;
using Karamchari.Succession.Persistence;
using Karamchari.Succession.Projections;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace Karamchari.HR.Tests.Domain;

public sealed class OrganizationProjectionTests
{
    private const string TenantId = "tenant-a";

    // ─── Helpers ────────────────────────────────────────────────────────────

    private static SuccessionDbContext CreateSuccessionDb(string tenantId, string databaseName)
    {
        var tenantProvider = Substitute.For<ITenantProvider>();
        tenantProvider.GetCurrentTenantId().Returns(tenantId);

        var envelope = new TenantExecutionEnvelope(
            tenantId,
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            ExecutionSource.Test,
            TenantSource.AdminOverride);

        tenantProvider.GetTenant().Returns(envelope);
        tenantProvider.TryGetCurrentTenantId(out Arg.Any<string?>()).Returns(x =>
        {
            x[0] = tenantId;
            return true;
        });

        var options = new DbContextOptionsBuilder<SuccessionDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new SuccessionDbContext(options, tenantProvider);
    }

    // ─── Organization Hierarchy Tests ───────────────────────────────────────

    [Fact]
    public async Task OrganizationHierarchy_CreatedAndDeactivated_UpdatesProjections()
    {
        var dbName = Guid.NewGuid().ToString();
        using var dbContext = HRDbContextFactory.Create(TenantId, dbName);
        var handler = new OrganizationHierarchyProjectionHandler(dbContext);

        var rootId = Guid.NewGuid();
        var parentEvent = new OrganizationUnitCreated(
            rootId,
            TenantId,
            "Headquarters",
            "HQ",
            OrganizationUnitType.Company,
            null,
            null,
            null);

        var childId = Guid.NewGuid();
        var childEvent = new OrganizationUnitCreated(
            childId,
            TenantId,
            "Engineering",
            "ENG",
            OrganizationUnitType.Department,
            rootId,
            null,
            null);

        // 1. Handle root created
        await handler.HandleAsync(parentEvent, CancellationToken.None);

        var parentProj = await dbContext.OrgHierarchyProjections.FindAsync(TenantId, rootId);
        parentProj.Should().NotBeNull();
        parentProj!.HierarchyPath.Should().Be("/HQ");
        parentProj.Level.Should().Be(0);

        var parentMetrics = await dbContext.OrgMetricsProjections.FindAsync(TenantId, rootId);
        parentMetrics.Should().NotBeNull();
        parentMetrics!.ActivePositionCount.Should().Be(0);

        // 2. Handle child created
        await handler.HandleAsync(childEvent, CancellationToken.None);

        var childProj = await dbContext.OrgHierarchyProjections.FindAsync(TenantId, childId);
        childProj.Should().NotBeNull();
        childProj!.HierarchyPath.Should().Be("/HQ/ENG");
        childProj.Level.Should().Be(1);

        // 3. Handle child deactivated
        var deactivateEvent = new OrganizationUnitDeactivated(childId, TenantId);
        await handler.HandleAsync(deactivateEvent, CancellationToken.None);

        var childProjAfter = await dbContext.OrgHierarchyProjections.FindAsync(TenantId, childId);
        childProjAfter.Should().BeNull();

        var childMetricsAfter = await dbContext.OrgMetricsProjections.FindAsync(TenantId, childId);
        childMetricsAfter.Should().BeNull();
    }

    // ─── Position Occupancy Tests ──────────────────────────────────────────

    [Fact]
    public async Task PositionOccupancy_Lifecycle_UpdatesMetricsAndOccupancy()
    {
        var dbName = Guid.NewGuid().ToString();
        using var dbContext = HRDbContextFactory.Create(TenantId, dbName);
        var handler = new PositionOccupancyProjectionHandler(dbContext);

        var orgUnitId = Guid.NewGuid();
        var departmentId = Guid.NewGuid();
        var designationId = Guid.NewGuid();
        var positionId = Guid.NewGuid();

        // Setup designation and org unit metrics (which is seeded when org unit is created)
        var designation = Designation.Create(TenantId, "Engineer", "ENG", 3);
        dbContext.Designations.Add(designation);

        var metrics = new OrganizationMetricsProjection
        {
            TenantId = TenantId,
            OrganizationUnitId = orgUnitId,
            ActivePositionCount = 0,
            ApprovedHeadcount = 0,
            CurrentHeadcount = 0,
            OpenVacancyCount = 0,
            LastUpdatedAtUtc = DateTimeOffset.UtcNow
        };
        dbContext.OrgMetricsProjections.Add(metrics);
        await dbContext.SaveChangesAsync();

        // 1. Position Created
        var createdEvent = new PositionCreated(
            positionId,
            TenantId,
            "POS-001",
            departmentId,
            designationId,
            null,
            null,
            null,
            null,
            orgUnitId,
            PositionStatus.Draft,
            false,
            1);

        await handler.HandleAsync(createdEvent, CancellationToken.None);

        var positionProj = await dbContext.PositionOccupancyProjections.FindAsync(TenantId, positionId);
        positionProj.Should().NotBeNull();
        positionProj!.VacancyCount.Should().Be(1);
        positionProj.CurrentOccupantCount.Should().Be(0);

        var metricsAfterCreate = await dbContext.OrgMetricsProjections.FindAsync(TenantId, orgUnitId);
        metricsAfterCreate!.ActivePositionCount.Should().Be(1);
        metricsAfterCreate.ApprovedHeadcount.Should().Be(1);
        metricsAfterCreate.OpenVacancyCount.Should().Be(1);

        // 2. Position Status Changed
        var statusEvent = new PositionStatusChanged(positionId, TenantId, PositionStatus.Approved);
        await handler.HandleAsync(statusEvent, CancellationToken.None);

        positionProj = await dbContext.PositionOccupancyProjections.FindAsync(TenantId, positionId);
        positionProj!.Status.Should().Be("Approved");

        // 3. Position Assignment Created (Occupant added)
        // Setup mock employee
        var employee = Employee.Hire("EMP-100", "John Doe", null, new DateOnly(2020, 1, 1));
        var employeeId = employee.Id;
        var assignmentId = Guid.NewGuid();
        dbContext.Employees.Add(employee);
        await dbContext.SaveChangesAsync();

        var assignEvent = new PositionAssignmentCreated(assignmentId, TenantId, positionId, employeeId, DateTimeOffset.UtcNow);
        await handler.HandleAsync(assignEvent, CancellationToken.None);

        positionProj = await dbContext.PositionOccupancyProjections.FindAsync(TenantId, positionId);
        positionProj!.CurrentOccupantCount.Should().Be(1);
        positionProj.VacancyCount.Should().Be(0);
        positionProj.OccupancyRate.Should().Be(1.0);

        var occupantProj = await dbContext.PositionOccupantProjections.FindAsync(TenantId, positionId, employeeId, assignmentId);
        occupantProj.Should().NotBeNull();
        occupantProj!.EmployeeName.Should().Be("John Doe");

        var metricsAfterAssign = await dbContext.OrgMetricsProjections.FindAsync(TenantId, orgUnitId);
        metricsAfterAssign!.CurrentHeadcount.Should().Be(1);
        metricsAfterAssign.OpenVacancyCount.Should().Be(0);

        // 4. Position Assignment Ended
        var endEvent = new PositionAssignmentEnded(assignmentId, TenantId, positionId, employeeId, DateTimeOffset.UtcNow);
        await handler.HandleAsync(endEvent, CancellationToken.None);

        positionProj = await dbContext.PositionOccupancyProjections.FindAsync(TenantId, positionId);
        positionProj!.CurrentOccupantCount.Should().Be(0);
        positionProj.VacancyCount.Should().Be(1);
        positionProj.OccupancyRate.Should().Be(0.0);

        var occupantProjAfter = await dbContext.PositionOccupantProjections.FindAsync(TenantId, positionId, employeeId, assignmentId);
        occupantProjAfter.Should().BeNull();

        var metricsAfterEnd = await dbContext.OrgMetricsProjections.FindAsync(TenantId, orgUnitId);
        metricsAfterEnd!.CurrentHeadcount.Should().Be(0);
        metricsAfterEnd.OpenVacancyCount.Should().Be(1);
    }

    // ─── Vacancy Pipeline Tests ─────────────────────────────────────────────

    [Fact]
    public async Task VacancyPipeline_Lifecycle_UpdatesProjections()
    {
        var dbName = Guid.NewGuid().ToString();
        using var dbContext = HRDbContextFactory.Create(TenantId, dbName);
        var publishEndpoint = NSubstitute.Substitute.For<MassTransit.IPublishEndpoint>();
        var handler = new VacancyPipelineProjectionHandler(dbContext, publishEndpoint);

        var vacancyId = Guid.NewGuid();

        // Seed position
        var position = Position.Create(TenantId, "POS-ABC", Guid.NewGuid(), Guid.NewGuid(), null);
        var positionId = position.Id;
        dbContext.Positions.Add(position);
        await dbContext.SaveChangesAsync();

        // 1. Vacancy Opened
        var openEvent = new PositionVacancyOpened(
            vacancyId,
            TenantId,
            positionId,
            DateTimeOffset.UtcNow,
            VacancyStatus.Open,
            "Growth",
            null,
            "EXT-111");

        await handler.HandleAsync(openEvent, CancellationToken.None);

        var vacancyProj = await dbContext.VacancyPipelineProjections.FindAsync(TenantId, vacancyId);
        vacancyProj.Should().NotBeNull();
        vacancyProj!.PositionCode.Should().Be("POS-ABC");
        vacancyProj.Status.Should().Be("Open");

        // 2. Vacancy Closed
        var employeeId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        var closeEvent = new PositionVacancyClosed(
            vacancyId,
            TenantId,
            positionId,
            VacancyStatus.Filled,
            DateTimeOffset.UtcNow,
            "Hired candidate",
            employeeId,
            assignmentId);

        await handler.HandleAsync(closeEvent, CancellationToken.None);

        vacancyProj = await dbContext.VacancyPipelineProjections.FindAsync(TenantId, vacancyId);
        vacancyProj!.Status.Should().Be("Filled");
        vacancyProj.ClosedReason.Should().Be("Hired candidate");
        vacancyProj.FilledByEmployeeId.Should().Be(employeeId);
        vacancyProj.FilledPositionAssignmentId.Should().Be(assignmentId);
    }

    // ─── Span of Control Tests ──────────────────────────────────────────────

    [Fact]
    public async Task SpanOfControl_ReportingRelationships_UpdatesProjections()
    {
        var dbName = Guid.NewGuid().ToString();
        using var dbContext = HRDbContextFactory.Create(TenantId, dbName);
        var handler = new SpanOfControlProjectionHandler(dbContext);

        var managerPosId = Guid.NewGuid();
        var reportPosId = Guid.NewGuid();

        // Setup position occupants so names are resolved
        var managerOccupant = new PositionOccupantProjection
        {
            TenantId = TenantId,
            PositionId = managerPosId,
            EmployeeId = Guid.NewGuid(),
            AssignmentId = Guid.NewGuid(),
            EmployeeName = "Manager Joe"
        };
        var reportOccupant = new PositionOccupantProjection
        {
            TenantId = TenantId,
            PositionId = reportPosId,
            EmployeeId = Guid.NewGuid(),
            AssignmentId = Guid.NewGuid(),
            EmployeeName = "Report Tim"
        };
        dbContext.PositionOccupantProjections.AddRange(managerOccupant, reportOccupant);
        await dbContext.SaveChangesAsync();

        // 1. Relationship Created
        var createdEvent = new ReportingRelationshipCreated(Guid.NewGuid(), TenantId, managerPosId, reportPosId, DateTimeOffset.UtcNow);
        await handler.HandleAsync(createdEvent, CancellationToken.None);

        var span = await dbContext.SpanOfControlProjections.FindAsync(TenantId, managerPosId);
        span.Should().NotBeNull();
        span!.ManagerName.Should().Be("Manager Joe");
        span.DirectReportCount.Should().Be(1);

        var directReport = await dbContext.ManagerDirectReportProjections.FindAsync(TenantId, managerPosId, reportPosId);
        directReport.Should().NotBeNull();
        directReport!.DirectReportEmployeeName.Should().Be("Report Tim");

        // 2. Relationship Ended
        var endedEvent = new ReportingRelationshipEnded(Guid.NewGuid(), TenantId, managerPosId, reportPosId, DateTimeOffset.UtcNow);
        await handler.HandleAsync(endedEvent, CancellationToken.None);

        span = await dbContext.SpanOfControlProjections.FindAsync(TenantId, managerPosId);
        span!.DirectReportCount.Should().Be(0);

        directReport = await dbContext.ManagerDirectReportProjections.FindAsync(TenantId, managerPosId, reportPosId);
        directReport.Should().BeNull();
    }

    // ─── Succession Coverage Tests ─────────────────────────────────────────

    [Fact]
    public async Task SuccessionCoverage_Lifecycle_UpdatesProjections()
    {
        var dbName = Guid.NewGuid().ToString();
        using var dbContext = CreateSuccessionDb(TenantId, dbName);
        var handler = new SuccessionCoverageProjectionHandler(dbContext);

        var positionId = Guid.NewGuid();
        var positionCode = "POS-KEY-99";

        // 1. Seed position created
        var positionCreated = new PositionCreated(
            positionId,
            TenantId,
            positionCode,
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null,
            null,
            null,
            Guid.NewGuid(),
            PositionStatus.Approved,
            false,
            1);

        await handler.HandleAsync(positionCreated, CancellationToken.None);

        var covProj = await dbContext.SuccessionCoverageProjections.FindAsync(TenantId, positionId);
        covProj.Should().NotBeNull();
        covProj!.PositionCode.Should().Be(positionCode);
        covProj.IsKeyPosition.Should().BeFalse();
        covProj.VacancyRisk.Should().Be("Low");

        // 2. Register Critical Role and plan directly
        var criticalRole = CriticalRole.Register(TenantId, positionCode, "Engineering", null, "Key position");
        dbContext.CriticalRoles.Add(criticalRole);
        var plan = SuccessionPlan.Create(TenantId, criticalRole.Id, Guid.NewGuid());
        dbContext.SuccessionPlans.Add(plan);
        await dbContext.SaveChangesAsync();

        // 3. Add Candidate
        var successorId = Guid.NewGuid();
        var addedEvent = new SuccessorAddedEvent(TenantId, plan.Id, criticalRole.Id, successorId, ReadinessLevel.ReadyNow);

        // Setup Plan with candidate in DB memory
        plan.AddCandidate(successorId, ReadinessLevel.ReadyNow, 4, 4, "Ready!");
        await dbContext.SaveChangesAsync();

        await handler.HandleAsync(addedEvent, CancellationToken.None);

        covProj = await dbContext.SuccessionCoverageProjections.FindAsync(TenantId, positionId);
        covProj!.IsKeyPosition.Should().BeTrue();
        covProj.SuccessorCount.Should().Be(1);
        covProj.ReadyNowCount.Should().Be(1);
        covProj.VacancyRisk.Should().Be("Medium"); // 1 ready-now = Medium risk in recomputation

        // 4. Update Occupant
        var occupantId = Guid.NewGuid();
        var assignCreated = new PositionAssignmentCreated(Guid.NewGuid(), TenantId, positionId, occupantId, DateTimeOffset.UtcNow);
        await handler.HandleAsync(assignCreated, CancellationToken.None);

        covProj = await dbContext.SuccessionCoverageProjections.FindAsync(TenantId, positionId);
        covProj!.CurrentOccupantId.Should().Be(occupantId);

        // 5. End Occupant Assignment
        var assignEnded = new PositionAssignmentEnded(Guid.NewGuid(), TenantId, positionId, occupantId, DateTimeOffset.UtcNow);
        await handler.HandleAsync(assignEnded, CancellationToken.None);

        covProj = await dbContext.SuccessionCoverageProjections.FindAsync(TenantId, positionId);
        covProj!.CurrentOccupantId.Should().BeNull();
    }
}
