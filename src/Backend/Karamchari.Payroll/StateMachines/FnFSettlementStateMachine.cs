using MassTransit;
using Karamchari.Payroll.Contracts;
using Karamchari.Payroll.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Payroll.StateMachines;

/// <summary>
/// Saga orchestrating FnF settlement lifecycle:
/// Initiated → Calculating → PendingApproval → Approved → Disbursing → Completed
/// Handles hold, reopen, failed disbursement retry.
/// </summary>
public sealed class FnFSettlementStateMachine : MassTransitStateMachine<FnFSettlementState>
{
    public State Calculating { get; private set; } = null!;
    public State PendingApproval { get; private set; } = null!;
    public State Approved { get; private set; } = null!;
    public State Disbursing { get; private set; } = null!;
    public State Completed { get; private set; } = null!;
    public State OnHold { get; private set; } = null!;
    public State Failed { get; private set; } = null!;

    public Event<FnFSettlementInitiatedIntegrationEvent> Initiated { get; private set; } = null!;
    public Event<InitiateFnFCalculationCommand> CalculationRequested { get; private set; } = null!;
    public Event<FnFSettlementApprovedIntegrationEvent> Approved_Evt { get; private set; } = null!;
    public Event<DisburseFnFCommand> DisburseRequested { get; private set; } = null!;
    public Event<FnFSettlementDisbursedIntegrationEvent> Disbursed { get; private set; } = null!;
    public Event<DisbursementBatchFailedIntegrationEvent> DisbursementFailed { get; private set; } = null!;

    public FnFSettlementStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => Initiated, x => x.CorrelateById(ctx => ctx.Message.SettlementId));
        Event(() => CalculationRequested, x => x.CorrelateById(ctx => ctx.Message.SettlementId));
        Event(() => Approved_Evt, x => x.CorrelateById(ctx => ctx.Message.SettlementId));
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
            When(Approved_Evt)
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
