// -----------------------------------------------------------------------
// <copyright file="WorkflowDefinitionTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Core.Domain.Workflows;
using Karamchari.Workflow.Domain;
using Xunit;

namespace Karamchari.Workflow.Tests.Domain;

public class WorkflowDefinitionTests
{
    // ---------------------------------------------------------------
    // Create
    // ---------------------------------------------------------------

    [Fact]
    public void WorkflowDefinition_Create_IsActive()
    {
        // Act
        var definition = WorkflowDefinition.Create(
            tenantId: "tenant-1",
            entityType: "LeaveRequest",
            name: "Standard Leave Approval");

        // Assert
        definition.IsActive.Should().BeTrue();
        definition.Name.Should().Be("Standard Leave Approval");
        definition.EntityType.Should().Be("LeaveRequest");
        definition.TenantId.Should().Be("tenant-1");
        definition.Id.Should().NotBeEmpty();
        definition.Steps.Should().BeEmpty();
        definition.Priority.Should().Be(0);
    }

    // ---------------------------------------------------------------
    // AddStep
    // ---------------------------------------------------------------

    [Fact]
    public void WorkflowDefinition_AddStep_IncreasesStepCount()
    {
        // Arrange
        var definition = BuildDefinition();
        var step = WorkflowStep.Create(
            definition.Id, order: 0, name: "Manager Approval",
            isParallel: false, quorum: QuorumRule.All, quorumThreshold: 0,
            approverRoles: ["Manager"]);

        // Act
        definition.AddStep(step);

        // Assert
        definition.Steps.Should().HaveCount(1);
        definition.Steps[0].Name.Should().Be("Manager Approval");
        definition.Steps[0].Order.Should().Be(0);
    }

    [Fact]
    public void WorkflowDefinition_AddStep_DuplicateOrder_ThrowsDomainException()
    {
        // Arrange
        var definition = BuildDefinition();
        var step1 = WorkflowStep.Create(
            definition.Id, order: 0, name: "Manager Approval",
            isParallel: false, quorum: QuorumRule.All, quorumThreshold: 0,
            approverRoles: ["Manager"]);
        var step2 = WorkflowStep.Create(
            definition.Id, order: 0, name: "Director Approval", // same order!
            isParallel: false, quorum: QuorumRule.All, quorumThreshold: 0,
            approverRoles: ["Director"]);

        definition.AddStep(step1);

        // Act
        var act = () => definition.AddStep(step2);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*step with order 0*");
    }

    [Fact]
    public void WorkflowDefinition_AddStep_MultipleSteps_AreOrdered()
    {
        // Arrange
        var definition = BuildDefinition();
        var step1 = WorkflowStep.Create(
            definition.Id, order: 1, name: "Director Approval",
            isParallel: false, quorum: QuorumRule.All, quorumThreshold: 0,
            approverRoles: ["Director"]);
        var step0 = WorkflowStep.Create(
            definition.Id, order: 0, name: "Manager Approval",
            isParallel: false, quorum: QuorumRule.All, quorumThreshold: 0,
            approverRoles: ["Manager"]);

        // Act — add in reverse order
        definition.AddStep(step1);
        definition.AddStep(step0);

        // Assert — sorted by order
        definition.Steps[0].Order.Should().Be(0);
        definition.Steps[1].Order.Should().Be(1);
    }

    // ---------------------------------------------------------------
    // Deactivate
    // ---------------------------------------------------------------

    [Fact]
    public void WorkflowDefinition_Deactivate_SetsIsActiveFalse()
    {
        // Arrange
        var definition = BuildDefinition();
        definition.IsActive.Should().BeTrue();

        // Act
        definition.Deactivate();

        // Assert
        definition.IsActive.Should().BeFalse();
    }

    // ---------------------------------------------------------------
    // SetPriority
    // ---------------------------------------------------------------

    [Fact]
    public void WorkflowDefinition_SetPriority_UpdatesPriority()
    {
        // Arrange
        var definition = BuildDefinition();
        definition.Priority.Should().Be(0);

        // Act
        definition.SetPriority(10);

        // Assert
        definition.Priority.Should().Be(10);
    }

    // ---------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------

    private static WorkflowDefinition BuildDefinition() =>
        WorkflowDefinition.Create("tenant-1", "LeaveRequest", "Standard Approval");
}
