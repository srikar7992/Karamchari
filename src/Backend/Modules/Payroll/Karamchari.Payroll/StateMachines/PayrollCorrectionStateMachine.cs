// -----------------------------------------------------------------------
// <copyright file="PayrollCorrectionStateMachine.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Payroll.Contracts;
using MassTransit;

namespace Karamchari.Payroll.StateMachines;

/// <summary>
/// Saga for payroll correction lifecycle.
/// Draft â†’ PendingApproval â†’ Approved â†’ Recalculating â†’ Processed
/// Guards against duplicate recalculation via RecalculationTriggered flag.
/// </summary>
public sealed class PayrollCorrectionStateMachine : MassTransitStateMachine<PayrollCorrectionState>
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public State PendingApproval { get; private set; } = null!;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public State Approved { get; private set; } = null!;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public State Recalculating { get; private set; } = null!;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public State Processed { get; private set; } = null!;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public State Rejected { get; private set; } = null!;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Event<CorrectionApprovedIntegrationEvent> CorrectionApproved { get; private set; } = null!;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Event<TriggerCorrectionRecalculationCommand> RecalculationTriggeredEvent { get; private set; } = null!;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Event<CorrectionProcessedIntegrationEvent> CorrectionProcessed { get; private set; } = null!;

    public PayrollCorrectionStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => CorrectionApproved, x => x.CorrelateById(ctx => ctx.Message.CorrectionId));
        Event(() => RecalculationTriggeredEvent, x => x.CorrelateById(ctx => ctx.Message.CorrectionId));
        Event(() => CorrectionProcessed, x => x.CorrelateById(ctx => ctx.Message.CorrectionId));

        Initially(
            When(CorrectionApproved)
                .Then(ctx =>
                {
                    ctx.Saga.TenantId = ctx.Message.TenantId;
                    ctx.Saga.EmployeeId = ctx.Message.EmployeeId;
                    ctx.Saga.CorrectionType = ctx.Message.CorrectionType;
                    ctx.Saga.CorrectionScope = ctx.Message.CorrectionScope;
                    ctx.Saga.AffectedPeriodName = ctx.Message.AffectedPeriodName;
                    ctx.Saga.ApprovedBy = "system";
                })
                .TransitionTo(Approved)
                // Trigger recalculation immediately on approval
                .PublishAsync(ctx => ctx.Init<TriggerCorrectionRecalculationCommand>(new
                {
                    CorrectionId = ctx.Saga.CorrelationId,
                    ctx.Message.TenantId,
                    ctx.Message.EmployeeId,
                    ctx.Message.AffectedPeriodName,
                    ctx.Message.CorrectionScope,
                    ChangeDetailsJson = string.Empty
                }))
        );

        During(Approved,
            When(RecalculationTriggeredEvent)
                // Idempotency: skip if already triggered in this saga
                .If(ctx => !ctx.Saga.RecalculationTriggered, binder => binder
                    .Then(ctx =>
                    {
                        ctx.Saga.RecalculationTriggered = true;
                        ctx.Saga.ChangeDetailsJson = ctx.Message.ChangeDetailsJson;
                    })
                    .TransitionTo(Recalculating))
        );

        During(Recalculating,
            When(CorrectionProcessed)
                .Then(ctx =>
                {
                    ctx.Saga.DifferentialAmount = ctx.Message.DifferentialAmount;
                })
                .TransitionTo(Processed)
                .Finalize()
        );

        SetCompletedWhenFinalized();
    }
}
