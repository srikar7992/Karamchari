// -----------------------------------------------------------------------
// <copyright file="WorkflowConcurrencyTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using FluentAssertions;
using Karamchari.Core.Domain.Workflows;
using Karamchari.Workflow.Domain;
using Xunit;

namespace Karamchari.TenantIsolationCertification.Concurrency;

public sealed class WorkflowConcurrencyTests
{
    [Fact]
    public async Task ConcurrentWorkflowApprovals_MustNotCauseRaceConditions()
    {
        var tenantId = "acme";
        var entityId = Guid.NewGuid();

        // 1. Setup a definition with a parallel step
        var definition = WorkflowDefinition.Create(tenantId, "TestEntity", "Parallel Approval Test");
        definition.AddStep(WorkflowStep.Create(definition.Id, 0, "Parallel Approval", true, QuorumRule.All, 2, ["Manager", "Director"]));

        // 2. Start Instance
        var instance = WorkflowInstance.Start(tenantId, "TestEntity", entityId, definition);
        var stepInstances = instance.StepInstances.ToList();

        // 3. Simultaneously approve from two threads
        var actor1 = Guid.NewGuid();
        var actor2 = Guid.NewGuid();

        var task1 = Task.Run(() => instance.Approve(stepInstances[0].Id, actor1));
        var task2 = Task.Run(() => instance.Approve(stepInstances[1].Id, actor2));

        await Task.WhenAll(task1, task2);

        // 4. Verify Final State
        instance.Status.Should().Be(WorkflowStatus.Approved, "Parallel quorum met simultaneously should result in approval");
        instance.AuditLog.Should().Contain(a => a.Action == "Approved", "Final approval should be audited once");
    }

    [Fact]
    public async Task ConcurrentApprovalAndRejection_MustResultInDeterministicOutcome()
    {
        var tenantId = "acme";
        var entityId = Guid.NewGuid();

        var definition = WorkflowDefinition.Create(tenantId, "TestEntity", "Conflict Test");
        definition.AddStep(WorkflowStep.Create(definition.Id, 0, "Conflict Step", true, QuorumRule.All, 2, ["Manager", "Director"]));

        var instance = WorkflowInstance.Start(tenantId, "TestEntity", entityId, definition);
        var stepInstances = instance.StepInstances.ToList();

        var actor1 = Guid.NewGuid();
        var actor2 = Guid.NewGuid();

        // One thread approves, one thread rejects
        var task1 = Task.Run(() =>
        {
            try { instance.Approve(stepInstances[0].Id, actor1); }
            catch (InvalidOperationException) { /* expected if rejected first */ }
        });

        var task2 = Task.Run(() =>
        {
            try { instance.Reject(stepInstances[1].Id, actor2, "Security risk"); }
            catch (InvalidOperationException) { /* expected if approved first */ }
        });

        await Task.WhenAll(task1, task2);
        // REJECTION WINS: Once any step is rejected, workflow status is Rejected
        instance.Status.Should().Be(WorkflowStatus.Rejected, "Rejection should override concurrent approval in the same step group");
    }
}
