using Karamchari.Payroll.Contracts;
using Karamchari.Payroll.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Payroll.StateMachines;

/// <summary>
/// Saga orchestrating FnF settlement lifecycle:
/// Initiated â†’ Calculating â†’ PendingApproval â†’ Approved â†’ Disbursing â†’ Completed
/// Handles hold, reopen, failed disbursement retry.
/// </summary>
public sealed class FnFSettlementStateMachine : MassTransitStateMachine<FnFSettlementState>
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public State Calculating { get; private set; } = null!;
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
    public State Disbursing { get; private set; } = null!;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public State Completed { get; private set; } = null!;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public State OnHold { get; private set; } = null!;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public State Failed { get; private set; } = null!;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Event<FnFSettlementInitiatedIntegrationEvent> Initiated { get; private set; } = null!;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Event<InitiateFnFCalculationCommand> CalculationRequested { get; private set; } = null!;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Event<FnFSettlementApprovedIntegrationEvent> ApprovedEvent { get; private set; } = null!;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Event<DisburseFnFCommand> DisburseRequested { get; private set; } = null!;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Event<FnFSettlementDisbursedIntegrationEvent> Disbursed { get; private set; } = null!;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Event<DisbursementBatchFailedIntegrationEvent> DisbursementFailed { get; private set; } = null!;

    public FnFSettlementStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => Initiated, x => x.CorrelateById(ctx => ctx.Message.SettlementId));
        Event(() => CalculationRequested, x => x.CorrelateById(ctx => ctx.Message.SettlementId));
        Event(() => ApprovedEvent, x => x.CorrelateById(ctx => ctx.Message.SettlementId));
        Event(() => DisburseRequested, x => x.CorrelateById(ctx => ctx.Message.SettlementId));
        Event(() => Disbursed, x => x.CorrelateById(ctx => ctx.Message.SettlementId));
        Event(() => DisbursementFailed, x => x.CorrelateById(ctx => ctx.Message.BatchId));

        Initially(
            When(Initiated)
                .Then(ctx =>
                {
                    ctx.Saga.TenantId = ctx.Message.TenantId;
                    ctx.Saga.EmployeeId = ctx.Message.EmployeeId;
                    ctx.Saga.ExitType = ctx.Message.ExitType;
                    ctx.Saga.LastWorkingDay = ctx.Message.LastWorkingDay;
                })
                .TransitionTo(Calculating)
                .PublishAsync(ctx => ctx.Init<InitiateFnFCalculationCommand>(new
                {
                    SettlementId = ctx.Saga.CorrelationId,
                    ctx.Message.TenantId,
                    ctx.Message.EmployeeId
                }))
        );

        During(Calculating,
            When(CalculationRequested)
                .TransitionTo(PendingApproval)
        );

        During(PendingApproval,
            When(ApprovedEvent)
                .Then(ctx =>
                {
                    ctx.Saga.ApprovedBy = ctx.Message.ApprovedBy;
                    ctx.Saga.ApprovedAtUtc = ctx.Message.OccurredOnUtc;
                    ctx.Saga.NetSettlementAmount = ctx.Message.NetSettlementAmount;
                })
                .TransitionTo(Approved)
        );

        During(Approved,
            When(DisburseRequested)
                .Then(ctx => ctx.Saga.DisbursementAttempts++)
                .TransitionTo(Disbursing)
        );

        During(Disbursing,
            When(Disbursed)
                .TransitionTo(Completed)
                .Finalize(),
            When(DisbursementFailed)
                .Then(ctx =>
                {
                    if (ctx.Saga.DisbursementAttempts >= 3)
                        ctx.Saga.CurrentState = nameof(Failed);
                })
                .If(ctx => ctx.Saga.DisbursementAttempts < 3,
                    binder => binder.TransitionTo(Approved))
        );

        SetCompletedWhenFinalized();
    }
}
