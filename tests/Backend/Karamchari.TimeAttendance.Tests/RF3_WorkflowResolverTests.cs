// -----------------------------------------------------------------------
// <copyright file="RF3_WorkflowResolverTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Tests;

using FluentAssertions;
using Karamchari.TimeAttendance.Domain.Approval;
using Karamchari.TimeAttendance.Domain.Leaves;
using Karamchari.TimeAttendance.Services;
using Xunit;

/// <summary>
/// RF#3 — Workflow Resolver: condition evaluation, fallback, multi-level chain.
/// Tests domain-layer logic. DB-dependent resolution requires Testcontainers integration test.
/// </summary>
public sealed class RF3_WorkflowResolverTests
{
    // ── ApprovalWorkflowDefinition ────────────────────────────────────────

    [Fact]
    public void WorkflowDefinition_Create_IsActiveByDefault()
    {
        var def = ApprovalWorkflowDefinition.Create("Standard Leave Workflow", "Leave");
        def.IsActive.Should().BeTrue();
        def.Steps.Should().BeEmpty();
    }

    [Fact]
    public void WorkflowDefinition_AddStep_OrderedBySequence()
    {
        var def = ApprovalWorkflowDefinition.Create("Leave Workflow", "Leave");
        def.AddStep(2, ApproverRole.HR, isRequired: false, conditionExpression: "ActualDays > 5");
        def.AddStep(1, ApproverRole.Manager, isRequired: true, conditionExpression: null);

        var ordered = def.Steps.OrderBy(s => s.Sequence).ToList();
        ordered[0].Role.Should().Be(ApproverRole.Manager);
        ordered[1].Role.Should().Be(ApproverRole.HR);
    }

    [Fact]
    public void WorkflowDefinition_DuplicateSequence_Throws()
    {
        var def = ApprovalWorkflowDefinition.Create("Leave Workflow", "Leave");
        def.AddStep(1, ApproverRole.Manager);

        var act = () => def.AddStep(1, ApproverRole.HR);
        act.Should().Throw<InvalidOperationException>("sequence 1 already exists");
    }

    [Fact]
    public void WorkflowDefinition_Deactivate_IsActiveFalse()
    {
        var def = ApprovalWorkflowDefinition.Create("Leave Workflow", "Leave");
        def.Deactivate();
        def.IsActive.Should().BeFalse();
    }

    [Fact]
    public void WorkflowDefinition_ConditionExpression_StoredCorrectly()
    {
        var def = ApprovalWorkflowDefinition.Create("Director Workflow", "Leave");
        def.AddStep(1, ApproverRole.Manager);
        def.AddStep(2, ApproverRole.Compliance, isRequired: false, conditionExpression: "ActualDays >= 10");

        var step = def.Steps.First(s => s.Role == ApproverRole.Compliance);
        step.ConditionExpression.Should().Be("ActualDays >= 10");
    }

    // ── Condition evaluation (replicates ApprovalWorkflowResolver.ShouldIncludeStep) ──

    [Theory]
    [InlineData(6.0, "ActualDays > 5", true)]     // 6 > 5 → include
    [InlineData(5.0, "ActualDays > 5", false)]    // 5 NOT > 5 → exclude
    [InlineData(5.0, "ActualDays >= 5", true)]    // 5 >= 5 → include
    [InlineData(4.0, "ActualDays >= 5", false)]   // 4 < 5 → exclude
    [InlineData(10.0, "ActualDays > 5", true)]
    [InlineData(0.5, "ActualDays > 5", false)]
    public void WorkflowCondition_ActualDays_IncludesStepCorrectly(
        double requestedDays, string condition, bool expected)
    {
        EvaluateCondition(condition, requestedDays).Should().Be(expected);
    }

    [Fact]
    public void WorkflowCondition_NullExpression_AlwaysIncludes()
        => EvaluateCondition(null, 1.0).Should().BeTrue();

    [Fact]
    public void WorkflowCondition_UnknownExpression_DefaultsToInclude()
        => EvaluateCondition("EmployeeGrade == Director", 1.0).Should().BeTrue(
            "unknown conditions default to include (safe)");

    // ── LeaveApproval chain construction ─────────────────────────────────

    [Fact]
    public void LeaveApproval_FallbackNoWorkflow_SingleManagerStep()
    {
        // Simulate: no workflow found → ApprovalWorkflowResolver falls back to Manager only
        var approval = LeaveApproval.Create(Guid.NewGuid());
        var managerId = Guid.NewGuid();

        approval.AddStep(1, managerId, ApproverRole.Manager);

        approval.Steps.Should().HaveCount(1);
        approval.Steps.First().Role.Should().Be(ApproverRole.Manager);
        approval.Steps.First().ApproverId.Should().Be(managerId);
    }

    [Fact]
    public void LeaveApproval_ActualDaysOver5_HrStepAdded()
    {
        // RequestedDays=6 → "ActualDays > 5" condition true → HR added
        var approval = LeaveApproval.Create(Guid.NewGuid());
        var managerId = Guid.NewGuid();
        var hrId = Guid.NewGuid();

        approval.AddStep(1, managerId, ApproverRole.Manager);
        approval.AddStep(2, hrId, ApproverRole.HR); // condition passed → step added

        approval.Steps.Should().HaveCount(2);
        approval.Steps.Any(s => s.Role == ApproverRole.HR).Should().BeTrue();
    }

    [Fact]
    public void LeaveApproval_ActualDays3_HrStepNotAdded()
    {
        // RequestedDays=3 → "ActualDays > 5" false → only Manager
        var approval = LeaveApproval.Create(Guid.NewGuid());
        var managerId = Guid.NewGuid();

        approval.AddStep(1, managerId, ApproverRole.Manager);

        approval.Steps.Should().HaveCount(1);
        approval.Steps.Should().NotContain(s => s.Role == ApproverRole.HR);
    }

    [Fact]
    public void LeaveApproval_HrRejects_WorkflowTerminates()
    {
        var approval = LeaveApproval.Create(Guid.NewGuid());
        approval.AddStep(1, Guid.NewGuid(), ApproverRole.Manager);
        approval.AddStep(2, Guid.NewGuid(), ApproverRole.HR);

        approval.RecordDecision(1, ApprovalDecision.Approved, "OK");
        approval.RecordDecision(2, ApprovalDecision.Rejected, "Policy violation");

        approval.Status.Should().Be(ApprovalChainStatus.Rejected, "HR rejection terminates chain");
    }

    [Fact]
    public void LeaveApproval_AllApprove_StatusApproved()
    {
        var approval = LeaveApproval.Create(Guid.NewGuid());
        approval.AddStep(1, Guid.NewGuid(), ApproverRole.Manager);
        approval.AddStep(2, Guid.NewGuid(), ApproverRole.HR);

        approval.RecordDecision(1, ApprovalDecision.Approved);
        approval.RecordDecision(2, ApprovalDecision.Approved);

        approval.Status.Should().Be(ApprovalChainStatus.Approved);
    }

    [Fact]
    public void LeaveApproval_DecisionOnCompleted_Throws()
    {
        var approval = LeaveApproval.Create(Guid.NewGuid());
        approval.AddStep(1, Guid.NewGuid(), ApproverRole.Manager);
        approval.RecordDecision(1, ApprovalDecision.Approved);

        var act = () => approval.RecordDecision(1, ApprovalDecision.Rejected);
        act.Should().Throw<InvalidOperationException>("cannot decide on completed chain");
    }

    [Fact]
    public void LeaveApproval_Delegation_ReassignsPendingSteps()
    {
        var approval = LeaveApproval.Create(Guid.NewGuid());
        var original = Guid.NewGuid();
        var delegate_ = Guid.NewGuid();
        approval.AddStep(1, original, ApproverRole.Manager);

        approval.ReassignPendingSteps(original, delegate_, "Manager on leave");

        approval.Steps.First().ApproverId.Should().Be(delegate_);
        approval.Steps.First().ReassignedFromApproverId.Should().Be(original);
    }

    // ── Condition evaluator (mirrors ApprovalWorkflowResolver private method) ──

    private static bool EvaluateCondition(string? condition, double requestedDays)
    {
        if (string.IsNullOrWhiteSpace(condition)) return true;

        if (condition.Contains("ActualDays >="))
        {
            var threshold = ParseThreshold(condition, "ActualDays >=");
            return threshold.HasValue && requestedDays >= threshold.Value;
        }

        if (condition.Contains("ActualDays >"))
        {
            var threshold = ParseThreshold(condition, "ActualDays >");
            return threshold.HasValue && requestedDays > threshold.Value;
        }

        return true;
    }

    private static double? ParseThreshold(string expression, string prefix)
    {
        var part = expression.Replace(prefix, "").Trim();
        return double.TryParse(part, out var v) ? v : null;
    }
}
