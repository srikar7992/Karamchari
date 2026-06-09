using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Karamchari.HR.Domain.Organization;
using Karamchari.HR.Domain.Organization.Projections;
using Karamchari.HR.Persistence;
using Karamchari.HR.Services;
using Karamchari.HR.Tests.Integration;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Karamchari.HR.Tests.Domain;

public sealed class WorkforceGraphTests
{
    private const string TenantA = "tenant-a";
    private const string TenantB = "tenant-b";

    [Fact]
    public async Task GraphService_GetRelatedNodes_ReturnsCorrectActiveNeighbors()
    {
        var dbName = Guid.NewGuid().ToString();
        using var dbContext = HRDbContextFactory.Create(TenantA, dbName);
        var service = new WorkforceGraphService(dbContext);

        var employeeId = Guid.NewGuid();
        var positionId = Guid.NewGuid();

        // 1. Add Nodes
        var employeeNode = new WorkforceGraphNode
        {
            TenantId = TenantA,
            Id = employeeId,
            Type = WorkforceNodeType.Employee,
            DisplayName = "John Doe"
        };
        var positionNode = new WorkforceGraphNode
        {
            TenantId = TenantA,
            Id = positionId,
            Type = WorkforceNodeType.Position,
            DisplayName = "Software Engineer"
        };

        dbContext.GraphNodes.AddRange(employeeNode, positionNode);

        // 2. Add Edges (one active, one inactive)
        var activeEdge = new WorkforceGraphEdge
        {
            Id = Guid.NewGuid(),
            TenantId = TenantA,
            SourceNodeId = employeeId,
            TargetNodeId = positionId,
            Type = WorkforceEdgeType.Occupies,
            IsActive = true
        };
        var inactiveEdge = new WorkforceGraphEdge
        {
            Id = Guid.NewGuid(),
            TenantId = TenantA,
            SourceNodeId = employeeId,
            TargetNodeId = Guid.NewGuid(), // random target
            Type = WorkforceEdgeType.Occupies,
            IsActive = false
        };

        dbContext.GraphEdges.AddRange(activeEdge, inactiveEdge);
        await dbContext.SaveChangesAsync();

        // 3. Act
        var related = await service.GetRelatedNodesAsync(TenantA, employeeId, WorkforceEdgeType.Occupies, CancellationToken.None);

        // 4. Assert
        related.Should().HaveCount(1);
        related.First().DisplayName.Should().Be("Software Engineer");
    }

    [Fact]
    public async Task GraphService_GetIndirectReports_TraversesHierarchicalTreeCorrectly()
    {
        var dbName = Guid.NewGuid().ToString();
        using var dbContext = HRDbContextFactory.Create(TenantA, dbName);
        var service = new WorkforceGraphService(dbContext);

        var ceoId = Guid.NewGuid();
        var vpId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var engineerId = Guid.NewGuid();

        // Create edges representing ReportsTo hierarchy (DirectReport -> Manager)
        // E.g. VP reports to CEO, Manager reports to VP, Engineer reports to Manager
        var edge1 = new WorkforceGraphEdge
        {
            Id = Guid.NewGuid(),
            TenantId = TenantA,
            SourceNodeId = vpId,
            TargetNodeId = ceoId,
            Type = WorkforceEdgeType.ReportsTo,
            IsActive = true
        };
        var edge2 = new WorkforceGraphEdge
        {
            Id = Guid.NewGuid(),
            TenantId = TenantA,
            SourceNodeId = managerId,
            TargetNodeId = vpId,
            Type = WorkforceEdgeType.ReportsTo,
            IsActive = true
        };
        var edge3 = new WorkforceGraphEdge
        {
            Id = Guid.NewGuid(),
            TenantId = TenantA,
            SourceNodeId = engineerId,
            TargetNodeId = managerId,
            Type = WorkforceEdgeType.ReportsTo,
            IsActive = true
        };

        // Inactive edge that should not be traversed
        var inactiveEdge = new WorkforceGraphEdge
        {
            Id = Guid.NewGuid(),
            TenantId = TenantA,
            SourceNodeId = Guid.NewGuid(),
            TargetNodeId = managerId,
            Type = WorkforceEdgeType.ReportsTo,
            IsActive = false
        };

        dbContext.GraphEdges.AddRange(edge1, edge2, edge3, inactiveEdge);
        await dbContext.SaveChangesAsync();

        // Act
        var indirects = (await service.GetIndirectReportsAsync(TenantA, ceoId, CancellationToken.None)).ToList();

        // Assert
        indirects.Should().HaveCount(3);
        indirects.Should().Contain(vpId);
        indirects.Should().Contain(managerId);
        indirects.Should().Contain(engineerId);
    }

    [Fact]
    public async Task GraphService_EnforcesStrictTenantIsolation()
    {
        var dbName = Guid.NewGuid().ToString();

        // Contexts sharing the same database name, but with Tenant A and Tenant B
        using var dbContextA = HRDbContextFactory.Create(TenantA, dbName);
        using var dbContextB = HRDbContextFactory.Create(TenantB, dbName);

        var serviceA = new WorkforceGraphService(dbContextA);

        var commonId = Guid.NewGuid();
        var targetA = Guid.NewGuid();
        var targetB = Guid.NewGuid();

        // Seed Tenant A Node & Edge
        dbContextA.GraphNodes.Add(new WorkforceGraphNode { TenantId = TenantA, Id = commonId, Type = WorkforceNodeType.Employee, DisplayName = "Acme CEO" });
        dbContextA.GraphNodes.Add(new WorkforceGraphNode { TenantId = TenantA, Id = targetA, Type = WorkforceNodeType.Position, DisplayName = "Acme Assistant" });
        dbContextA.GraphEdges.Add(new WorkforceGraphEdge { Id = Guid.NewGuid(), TenantId = TenantA, SourceNodeId = commonId, TargetNodeId = targetA, Type = WorkforceEdgeType.Occupies, IsActive = true });
        await dbContextA.SaveChangesAsync();

        // Seed Tenant B Node & Edge (sharing the same commonId source node)
        dbContextB.GraphNodes.Add(new WorkforceGraphNode { TenantId = TenantB, Id = commonId, Type = WorkforceNodeType.Employee, DisplayName = "Contoso CEO" });
        dbContextB.GraphNodes.Add(new WorkforceGraphNode { TenantId = TenantB, Id = targetB, Type = WorkforceNodeType.Position, DisplayName = "Contoso Advisor" });
        dbContextB.GraphEdges.Add(new WorkforceGraphEdge { Id = Guid.NewGuid(), TenantId = TenantB, SourceNodeId = commonId, TargetNodeId = targetB, Type = WorkforceEdgeType.Occupies, IsActive = true });
        await dbContextB.SaveChangesAsync();

        // Act: Query with Tenant A service
        var results = await serviceA.GetRelatedNodesAsync(TenantA, commonId, WorkforceEdgeType.Occupies, CancellationToken.None);

        // Assert: Query returns Tenant A target only
        results.Should().HaveCount(1);
        results.First().DisplayName.Should().Be("Acme Assistant");
    }

    [Fact]
    public async Task GraphEdges_DuplicateEdge_ViolatesUniqueIndex()
    {
        var dbName = Guid.NewGuid().ToString();
        using var dbContext = HRDbContextFactory.Create(TenantA, dbName);

        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var effectiveFrom = DateTimeOffset.UtcNow;

        var edge1 = new WorkforceGraphEdge
        {
            Id = Guid.NewGuid(),
            TenantId = TenantA,
            SourceNodeId = sourceId,
            TargetNodeId = targetId,
            Type = WorkforceEdgeType.ReportsTo,
            EffectiveFrom = effectiveFrom,
            IsActive = true
        };

        var edge2 = new WorkforceGraphEdge
        {
            Id = Guid.NewGuid(),
            TenantId = TenantA,
            SourceNodeId = sourceId,
            TargetNodeId = targetId,
            Type = WorkforceEdgeType.ReportsTo,
            EffectiveFrom = effectiveFrom,
            IsActive = true
        };

        dbContext.GraphEdges.Add(edge1);
        await dbContext.SaveChangesAsync();

        // Adding edge2 with identical unique properties should trigger index violation
        dbContext.GraphEdges.Add(edge2);

        if (dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            // InMemory provider doesn't enforce unique constraints
            return;
        }

        Func<Task> saveAction = async () => await dbContext.SaveChangesAsync();
        await saveAction.Should().ThrowAsync<DbUpdateException>();
    }
}
