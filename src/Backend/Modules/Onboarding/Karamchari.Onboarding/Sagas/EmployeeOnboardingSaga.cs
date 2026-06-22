// -----------------------------------------------------------------------
// <copyright file="EmployeeOnboardingSaga.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Benefits.Contracts.IntegrationEvents;
using Karamchari.Core.Contracts.IntegrationEvents.V1;
using Karamchari.Onboarding.Contracts.IntegrationEvents;
using Karamchari.Payroll.Contracts;
using MassTransit;

namespace Karamchari.Onboarding.Sagas;

/// <summary>
/// EmployeeOnboarding saga (H-2 priority 1). Orchestrates the cross-module fan-out
/// after an employee is onboarded: it waits for BOTH payroll-profile provisioning
/// AND benefits enrollment before publishing a Completed event. An employee is never
/// declared fully onboarded on partial completion.
///
/// Follow-ups (tracked in CERTIFICATION.md, H-2): (1) durable EF saga persistence so
/// in-flight state survives a worker restart; (2) a scheduled provisioning-deadline
/// that publishes <see cref="EmployeeOnboardingIncompleteIntegrationEventV1"/> to
/// escalate a stalled onboarding. Both require host-side scheduler + saga DbContext
/// wiring and are intentionally out of this increment, which establishes and tests
/// the core orchestration invariant.
/// </summary>
public sealed class EmployeeOnboardingSaga : MassTransitStateMachine<EmployeeOnboardingSagaState>
{
    /// <summary>The employee has been onboarded; provisioning is in progress.</summary>
    public State Provisioning { get; private set; } = null!;

    /// <summary>Trigger: HR published EmployeeOnboarded.</summary>
    public Event<EmployeeOnboardedIntegrationEventV1> Onboarded { get; private set; } = null!;

    /// <summary>Step: Payroll provisioned a profile.</summary>
    public Event<PayrollProfileProvisionedIntegrationEventV1> PayrollProvisioned { get; private set; } = null!;

    /// <summary>Step: Benefits enrollment activated.</summary>
    public Event<BenefitEnrollmentActivatedIntegrationEventV1> BenefitsActivated { get; private set; } = null!;

    /// <summary>Initializes the saga state machine.</summary>
    public EmployeeOnboardingSaga()
    {
        InstanceState(x => x.CurrentState);

        Event(() => Onboarded, x => x.CorrelateById(c => c.Message.EmployeeId));
        Event(() => PayrollProvisioned, x => x.CorrelateById(c => c.Message.EmployeeId));
        Event(() => BenefitsActivated, x => x.CorrelateById(c => c.Message.EmployeeId));

        Initially(
            When(Onboarded)
                .Then(c =>
                {
                    c.Saga.CorrelationId = c.Message.EmployeeId;
                    c.Saga.TenantId = c.Message.TenantId;
                    c.Saga.CreatedAtUtc = DateTimeOffset.UtcNow;
                })
                .TransitionTo(Provisioning));

        During(Provisioning,
            When(PayrollProvisioned)
                .Then(c => { c.Saga.PayrollProvisioned = true; c.Saga.UpdatedAtUtc = DateTimeOffset.UtcNow; })
                .IfElse(c => c.Saga.BenefitsEnrolled,
                    bothDone => bothDone.ThenComplete().Finalize(),
                    waiting => waiting),
            When(BenefitsActivated)
                .Then(c => { c.Saga.BenefitsEnrolled = true; c.Saga.UpdatedAtUtc = DateTimeOffset.UtcNow; })
                .IfElse(c => c.Saga.PayrollProvisioned,
                    bothDone => bothDone.ThenComplete().Finalize(),
                    waiting => waiting));

        SetCompletedWhenFinalized();
    }
}

/// <summary>Extension to publish the terminal Completed event in both completion branches.</summary>
internal static class EmployeeOnboardingSagaBehaviorExtensions
{
    public static EventActivityBinder<EmployeeOnboardingSagaState, T> ThenComplete<T>(
        this EventActivityBinder<EmployeeOnboardingSagaState, T> binder)
        where T : class
        => binder.Publish(c => new EmployeeOnboardingCompletedIntegrationEventV1(c.Saga.CorrelationId, c.Saga.TenantId));
}
