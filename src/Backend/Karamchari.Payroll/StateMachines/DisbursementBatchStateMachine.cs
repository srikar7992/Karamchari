using Karamchari.Payroll.Contracts;
using MassTransit;

namespace Karamchari.Payroll.StateMachines;

/// <summary>
/// Saga for bank disbursement batch lifecycle.
/// Pending â†’ FileGenerated â†’ Submitted â†’ (Completed | PartiallyProcessed | Failed)
/// Handles retry up to MaxRetries before marking Failed.
/// </summary>
public sealed class DisbursementBatchStateMachine : MassTransitStateMachine<DisbursementBatchState>
{
    private const int MaxRetries = 3;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public State Generating { get; private set; } = null!;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public State Submitted { get; private set; } = null!;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public State PartiallyProcessed { get; private set; } = null!;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public State Completed { get; private set; } = null!;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public State Failed { get; private set; } = null!;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Event<InitiateDisbursementCommand> Initiate { get; private set; } = null!;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Event<DisbursementBatchSubmittedIntegrationEvent> BatchSubmitted { get; private set; } = null!;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Event<DisbursementBatchCompletedIntegrationEvent> BatchCompleted { get; private set; } = null!;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Event<DisbursementBatchFailedIntegrationEvent> BatchFailed { get; private set; } = null!;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Event<RetryDisbursementCommand> RetryRequested { get; private set; } = null!;

    public DisbursementBatchStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => Initiate, x => x.CorrelateBy<Guid>(s => s.RunId, ctx => ctx.Message.RunId)
            .SelectId(ctx => Guid.NewGuid()));
        Event(() => BatchSubmitted, x => x.CorrelateById(ctx => ctx.Message.BatchId));
        Event(() => BatchCompleted, x => x.CorrelateById(ctx => ctx.Message.BatchId));
        Event(() => BatchFailed, x => x.CorrelateById(ctx => ctx.Message.BatchId));
        Event(() => RetryRequested, x => x.CorrelateById(ctx => ctx.Message.BatchId));

        Initially(
            When(Initiate)
                .Then(ctx =>
                {
                    ctx.Saga.TenantId = ctx.Message.TenantId;
                    ctx.Saga.RunId = ctx.Message.RunId;
                    ctx.Saga.PeriodName = ctx.Message.PeriodName;
                    ctx.Saga.BankProvider = ctx.Message.BankProvider;
                    ctx.Saga.InitiatedBy = ctx.Message.InitiatedBy;
                })
                .TransitionTo(Generating)
        );

        During(Generating,
            When(BatchSubmitted)
                .Then(ctx =>
                {
                    ctx.Saga.TotalAmount = ctx.Message.TotalAmount;
                    ctx.Saga.BankAcknowledgementId = ctx.Message.BatchId.ToString();
                })
                .TransitionTo(Submitted)
        );

        During(Submitted,
            When(BatchCompleted)
                .Then(ctx =>
                {
                    ctx.Saga.SuccessCount = ctx.Message.SuccessCount;
                    ctx.Saga.FailedCount = ctx.Message.FailedCount;
                })
                .IfElse(ctx => ctx.Message.FailedCount == 0,
                    thenBinder => thenBinder.TransitionTo(Completed).Finalize(),
                    elseBinder => elseBinder.TransitionTo(PartiallyProcessed)),
            When(BatchFailed)
                .Then(ctx => ctx.Saga.FailureReason = ctx.Message.Reason)
                .TransitionTo(Failed)
        );

        During(PartiallyProcessed,
            When(RetryRequested)
                .Then(ctx =>
                {
                    ctx.Saga.RetryCount++;
                })
                .If(ctx => ctx.Saga.RetryCount <= MaxRetries,
                    binder => binder.TransitionTo(Generating))
                .If(ctx => ctx.Saga.RetryCount > MaxRetries,
                    binder => binder.TransitionTo(Failed))
        );

        During(Failed,
            When(RetryRequested)
                .Then(ctx => ctx.Saga.RetryCount++)
                .If(ctx => ctx.Saga.RetryCount <= MaxRetries,
                    binder => binder.TransitionTo(Generating))
        );

        SetCompletedWhenFinalized();
    }
}
