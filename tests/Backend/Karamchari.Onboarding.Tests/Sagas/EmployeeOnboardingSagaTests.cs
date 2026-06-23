// -----------------------------------------------------------------------
// <copyright file="EmployeeOnboardingSagaTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Benefits.Contracts.IntegrationEvents;
using Karamchari.Core.Contracts.IntegrationEvents.V1;
using Karamchari.Onboarding.Contracts.IntegrationEvents;
using Karamchari.Onboarding.Sagas;
using Karamchari.Payroll.Contracts;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Karamchari.Onboarding.Tests.Sagas;

/// <summary>
/// H-2 priority 1: proves the EmployeeOnboarding saga only declares an employee
/// fully onboarded when BOTH payroll provisioning AND benefits enrollment complete,
/// and never on partial completion.
/// </summary>
public sealed class EmployeeOnboardingSagaTests
{
    private static EmployeeOnboardedIntegrationEventV1 Onboarded(Guid id) =>
        new(id, "dev", "E-1", "Bob Builder", "bob@example.com", new DateOnly(2026, 1, 1));

    private static async Task<ITestHarness> StartHarnessAsync()
    {
        var provider = new ServiceCollection()
            .AddMassTransitTestHarness(x =>
            {
                x.AddDelayedMessageScheduler();
                x.AddSagaStateMachine<EmployeeOnboardingSaga, EmployeeOnboardingSagaState>()
                    .InMemoryRepository();
            })
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        return harness;
    }

    [Fact]
    public async Task BothStepsComplete_PublishesOnboardingCompleted()
    {
        var harness = await StartHarnessAsync();
        var employeeId = Guid.NewGuid();

        var sagaHarness = harness.GetSagaStateMachineHarness<EmployeeOnboardingSaga, EmployeeOnboardingSagaState>();

        await harness.Bus.Publish(Onboarded(employeeId));
        // Ensure the saga instance exists before sending step events (otherwise the
        // step events would not correlate to any instance).
        (await sagaHarness.Created.Any(x => x.CorrelationId == employeeId)).Should().BeTrue();

        await harness.Bus.Publish(new PayrollProfileProvisionedIntegrationEventV1(employeeId, "dev"));
        (await harness.Consumed.Any<PayrollProfileProvisionedIntegrationEventV1>()).Should().BeTrue();

        await harness.Bus.Publish(new BenefitEnrollmentActivatedIntegrationEventV1(
            Guid.NewGuid(), employeeId, "dev", new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31),
            new List<ActiveElectionDto>(), DateTimeOffset.UtcNow));
        (await harness.Consumed.Any<BenefitEnrollmentActivatedIntegrationEventV1>()).Should().BeTrue();

        (await harness.Published.Any<EmployeeOnboardingCompletedIntegrationEventV1>(
            x => x.Context.Message.EmployeeId == employeeId)).Should().BeTrue(
            "the saga must declare the employee onboarded once payroll AND benefits both complete");

        (await harness.Published.Any<EmployeeOnboardingIncompleteIntegrationEventV1>()).Should().BeFalse();

        await harness.Stop();
    }

    [Fact]
    public async Task OnlyBenefits_DoesNotPublishCompleted_NoHalfProvisionedEmployee()
    {
        var harness = await StartHarnessAsync();
        var employeeId = Guid.NewGuid();

        var sagaHarness = harness.GetSagaStateMachineHarness<EmployeeOnboardingSaga, EmployeeOnboardingSagaState>();

        await harness.Bus.Publish(Onboarded(employeeId));
        (await harness.Consumed.Any<EmployeeOnboardedIntegrationEventV1>()).Should().BeTrue();
        (await sagaHarness.Created.Any(x => x.CorrelationId == employeeId)).Should().BeTrue();

        await harness.Bus.Publish(new BenefitEnrollmentActivatedIntegrationEventV1(
            Guid.NewGuid(), employeeId, "dev", new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31),
            new List<ActiveElectionDto>(), DateTimeOffset.UtcNow));
        (await harness.Consumed.Any<BenefitEnrollmentActivatedIntegrationEventV1>()).Should().BeTrue();

        // Saga must still be in flight (payroll never arrived) — and must NOT have
        // falsely declared the employee fully onboarded.
        (await harness.Published.Any<EmployeeOnboardingCompletedIntegrationEventV1>(
            x => x.Context.Message.EmployeeId == employeeId)).Should().BeFalse(
            "a half-provisioned employee (benefits only, no payroll) must never be marked onboarded");

        await harness.Stop();
    }
}
