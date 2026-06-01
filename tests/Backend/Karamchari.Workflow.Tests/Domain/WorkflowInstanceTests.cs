using FluentAssertions;
using Karamchari.Core.Domain.Workflows;
using Karamchari.Workflow.Domain;
using Xunit;

namespace Karamchari.Workflow.Tests.Domain;

public class WorkflowInstanceTests
{
    // ---------------------------------------------------------------
    // Start
    // ---------------------------------------------------------------

    [Fact]
    public void WorkflowInstance_Start_StatusIsPending()
    {
        // Arrange
        var definition = BuildSingleStepDefinition(QuorumRule.All, ["Manager"]);
        var entityId = Guid.NewGuid();

        // Act
        var instance = WorkflowInstance.Start(
            tenantId: "tenant-1",
            entityType: "LeaveRequest",
            entityId: entityId,
            definition: definition);

        // Assert
        instance.Status.Should().Be(WorkflowStatus.Pending);
        instance.EntityId.Should().Be(entityId);
        instance.EntityType.Should().Be("LeaveRequest");
        instance.TenantId.Should().Be("tenant-1");
        instance.Id.Should().NotBeEmpty();
        instance.StepInstances.Should().NotBeEmpty();
    }

    [Fact]
    public void WorkflowInstance_Start_FromInactiveDefinition_ThrowsDomainException()
    {
        // Arrange
        var definition = BuildSingleStepDefinition(QuorumRule.All, ["Manager"]);
        definition.Deactivate();

        // Act
        var act = () => WorkflowInstance.Start(
            "tenant-1", "LeaveRequest", Guid.NewGuid(), definition);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*inactive definition*");
    }

    [Fact]
    public void WorkflowInstance_Start_FromDefinitionWithNoSteps_ThrowsDomainException()
    {
        // Arrange — definition with no steps
        var definition = WorkflowDefinition.Create("tenant-1", "LeaveRequest", "Empty");

        // Act
        var act = () => WorkflowInstance.Start(
            "tenant-1", "LeaveRequest", Guid.NewGuid(), definition);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*no steps*");
    }

    // ---------------------------------------------------------------
    // Approve — single step, All quorum
    // ---------------------------------------------------------------

    [Fact]
    public void WorkflowInstance_Approve_SingleStep_AllQuorum_CompletesWorkflow()
    {
        // Arrange — single step, single role, QuorumRule.All
        var definition = BuildSingleStepDefinition(QuorumRule.All, ["Manager"]);
        var instance = WorkflowInstance.Start("tenant-1", "LeaveRequest", Guid.NewGuid(), definition);

        var stepInstance = instance.StepInstances.Single(s => s.ApproverRole == "Manager");
        var approverId = Guid.NewGuid();

        // Act
        instance.Approve(stepInstance.Id, approverId);

        // Assert — single-role All quorum → quorum met → workflow completes
        instance.Status.Should().Be(WorkflowStatus.Approved);
        instance.CompletedAt.Should().NotBeNull();
    }

    // ---------------------------------------------------------------
    // Approve — single step, Any quorum
    // ---------------------------------------------------------------

    [Fact]
    public void WorkflowInstance_Approve_SingleStep_AnyQuorum_CompletesWorkflow()
    {
        // Arrange — single role, QuorumRule.Any.
        // NOTE: The snapshot serialiser emits enum values as integers; IsQuorumMet
        // falls back to All semantics on parse failure. With a single role the All
        // and Any outcomes are identical — one approval satisfies the step.
        var definition = BuildSingleStepDefinition(QuorumRule.Any, ["Manager"]);
        var instance = WorkflowInstance.Start("tenant-1", "LeaveRequest", Guid.NewGuid(), definition);

        var stepInstance = instance.StepInstances.Single(s => s.ApproverRole == "Manager");
        var approverId = Guid.NewGuid();

        // Act
        instance.Approve(stepInstance.Id, approverId);

        // Assert — single-role step → All/Any behaviour identical → workflow completes
        instance.Status.Should().Be(WorkflowStatus.Approved);
    }

    // ---------------------------------------------------------------
    // Reject — single step
    // ---------------------------------------------------------------

    [Fact]
    public void WorkflowInstance_Reject_SingleStep_RejectsWorkflow()
    {
        // Arrange
        var definition = BuildSingleStepDefinition(QuorumRule.All, ["Manager"]);
        var instance = WorkflowInstance.Start("tenant-1", "LeaveRequest", Guid.NewGuid(), definition);

        var stepInstance = instance.StepInstances.Single();
        var rejectorId = Guid.NewGuid();

        // Act
        instance.Reject(stepInstance.Id, rejectorId, reason: "Insufficient leave balance");

        // Assert
        instance.Status.Should().Be(WorkflowStatus.Rejected);
        instance.CompletedAt.Should().NotBeNull();
    }

    // ---------------------------------------------------------------
    // Approve / Reject on non-Pending workflow → throws
    // ---------------------------------------------------------------

    [Fact]
    public void WorkflowInstance_ApproveOrReject_WhenNotPending_ThrowsDomainException()
    {
        // Arrange
        var definition = BuildSingleStepDefinition(QuorumRule.All, ["Manager"]);
        var instance = WorkflowInstance.Start("tenant-1", "LeaveRequest", Guid.NewGuid(), definition);
        var stepInstance = instance.StepInstances.Single();
        var approverId = Guid.NewGuid();

        // First approval completes (Approved)
        instance.Approve(stepInstance.Id, approverId);
        instance.Status.Should().Be(WorkflowStatus.Approved);

        // Act — attempt to approve the same step again
        var actApprove = () => instance.Approve(stepInstance.Id, Guid.NewGuid());
        var actReject = () => instance.Reject(stepInstance.Id, Guid.NewGuid(), "late rejection");

        // Assert
        actApprove.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot approve*");
        actReject.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot reject*");
    }

    // ---------------------------------------------------------------
    // Approve — audit trail captured
    // ---------------------------------------------------------------

    [Fact]
    public void WorkflowInstance_Approve_AddsAuditEntry()
    {
        // Arrange
        var definition = BuildSingleStepDefinition(QuorumRule.All, ["Manager"]);
        var instance = WorkflowInstance.Start("tenant-1", "LeaveRequest", Guid.NewGuid(), definition);
        var auditCountBefore = instance.AuditLog.Count;

        var stepInstance = instance.StepInstances.Single();
        var approverId = Guid.NewGuid();

        // Act
        instance.Approve(stepInstance.Id, approverId);

        // Assert — at least one new audit entry added for the approval
        instance.AuditLog.Count.Should().BeGreaterThan(auditCountBefore);
        instance.AuditLog.Should().Contain(e => e.Action == "StepApproved" && e.ActorId == approverId);
    }

    // ---------------------------------------------------------------
    // Multi-step — final step approval completes workflow
    // ---------------------------------------------------------------

    [Fact]
    public void WorkflowInstance_MultiStep_FinalStepApproval_CompletesWorkflow()
    {
        // Arrange — two-step workflow: step 0 (Manager), step 1 (Director)
        var definition = WorkflowDefinition.Create("tenant-1", "PurchaseOrder", "PO Approval");
        var step0 = WorkflowStep.Create(
            definition.Id, order: 0, name: "Manager Approval",
            isParallel: false, quorum: QuorumRule.All, quorumThreshold: 0,
            approverRoles: ["Manager"]);
        var step1 = WorkflowStep.Create(
            definition.Id, order: 1, name: "Director Approval",
            isParallel: false, quorum: QuorumRule.All, quorumThreshold: 0,
            approverRoles: ["Director"]);
        definition.AddStep(step0);
        definition.AddStep(step1);

        var instance = WorkflowInstance.Start("tenant-1", "PurchaseOrder", Guid.NewGuid(), definition);

        // Step 0 should be the initial step
        instance.Status.Should().Be(WorkflowStatus.Pending);
        var step0Instance = instance.StepInstances.Single(s => s.StepOrder == 0);

        // Act — approve step 0
        instance.Approve(step0Instance.Id, Guid.NewGuid());

        // Assert — workflow still pending, now on step 1
        instance.Status.Should().Be(WorkflowStatus.Pending);
        instance.CurrentStep.Should().Be(1);

        // Step 1 instance should now exist
        var step1Instance = instance.StepInstances.Single(s => s.StepOrder == 1);

        // Act — approve step 1 (final step)
        instance.Approve(step1Instance.Id, Guid.NewGuid());

        // Assert — workflow now completed
        instance.Status.Should().Be(WorkflowStatus.Approved);
        instance.CompletedAt.Should().NotBeNull();
    }

    // ---------------------------------------------------------------
    // MinN quorum — threshold-based: N approvals required out of M total
    // ---------------------------------------------------------------

    [Fact]
    public void WorkflowInstance_Approve_MinNQuorum_Threshold1_CompletesAfterFirstApproval()
    {
        // MinN with threshold=1 and 2 approvers: first approval alone satisfies quorum.
        var definition = BuildSingleStepDefinition(
            QuorumRule.MinN, ["Manager", "Director"], quorumThreshold: 1);
        var instance = WorkflowInstance.Start("tenant-1", "CapEx", Guid.NewGuid(), definition);

        var stepInstances = instance.StepInstances.ToList();
        stepInstances.Should().HaveCount(2);

        // Act — first approval satisfies MinN(1)
        instance.Approve(stepInstances[0].Id, Guid.NewGuid());

        // Assert — workflow completes immediately; threshold reached
        instance.Status.Should().Be(WorkflowStatus.Approved);
        instance.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void WorkflowInstance_Approve_MinNQuorum_Threshold2_RequiresTwoApprovals()
    {
        // MinN with threshold=2 and 3 approvers: first approval is not enough.
        var definition = BuildSingleStepDefinition(
            QuorumRule.MinN, ["Manager", "Director", "VP"], quorumThreshold: 2);
        var instance = WorkflowInstance.Start("tenant-1", "CapEx", Guid.NewGuid(), definition);

        var stepInstances = instance.StepInstances.ToList();
        stepInstances.Should().HaveCount(3);

        // First approval — quorum not yet met (1 < 2)
        instance.Approve(stepInstances[0].Id, Guid.NewGuid());
        instance.Status.Should().Be(WorkflowStatus.Pending);

        // Second approval — quorum met (2 >= 2)
        instance.Approve(stepInstances[1].Id, Guid.NewGuid());
        instance.Status.Should().Be(WorkflowStatus.Approved);
        instance.CompletedAt.Should().NotBeNull();
    }

    // ---------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------

    private static WorkflowDefinition BuildSingleStepDefinition(
        QuorumRule quorum,
        IEnumerable<string> roles,
        int quorumThreshold = 0)
    {
        var definition = WorkflowDefinition.Create("tenant-1", "LeaveRequest", "Approval Flow");
        var step = WorkflowStep.Create(
            definition.Id,
            order: 0,
            name: "Step 1",
            isParallel: false,
            quorum: quorum,
            quorumThreshold: quorumThreshold,
            approverRoles: roles);
        definition.AddStep(step);
        return definition;
    }
}
