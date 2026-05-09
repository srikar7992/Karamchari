using MassTransit;
using Karamchari.Payroll.Contracts;
using Karamchari.Core.Contracts.IntegrationEvents;
using Karamchari.Payroll.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Payroll.StateMachines;

/// <summary>
/// State machine for orchestrating a payroll run saga.
/// </summary>
public class PayrollRunStateMachine : MassTransitStateMachine<PayrollRunState>
{
    // 1. Define States
    /// <summary>
    /// Gets the state representing the calculation phase.
    /// </summary>
    public State Calculating { get; private set; } = null!;
    /// <summary>
    /// Gets the state representing the approval pending phase.
    /// </summary>
    public State PendingReview { get; private set; } = null!;
    /// <summary>
    /// Gets the state representing the finalized phase.
    /// </summary>
    public State Finalized { get; private set; } = null!;

    // 2. Define Events
    /// <summary>
    /// Gets the event for starting a payroll run.
    /// </summary>
    public Event<Karamchari.Core.Contracts.IntegrationEvents.StartPayrollRunCommand> StartCommand { get; private set; } = null!;
    /// <summary>
    /// Gets the event for an individual employee's pay being calculated.
    /// </summary>
    public Event<EmployeePayCalculatedEvent> PayCalculated { get; private set; } = null!;
    /// <summary>
    /// Gets the event for locking a payroll run.
    /// </summary>
    public Event<Karamchari.Core.Contracts.IntegrationEvents.LockPayrollRunCommand> LockCommand { get; private set; } = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="PayrollRunStateMachine"/> class.
    /// </summary>
    public PayrollRunStateMachine()
    {
        // Tell MassTransit which property holds the state string
        InstanceState(x => x.CurrentState);

        // Correlate the events to the Saga instance
        Event(() => StartCommand, x => x.CorrelateById(context => context.Message.RunId));
        Event(() => PayCalculated, x => x.CorrelateById(context => context.Message.RunId));
        Event(() => LockCommand, x => x.CorrelateById(context => context.Message.RunId));

        // Define the behavior
        Initially(
            When(StartCommand)
                .ThenAsync(async context => 
                {
                    context.Saga.TenantId = context.Message.TenantId;
                    context.Saga.PeriodName = context.Message.PeriodName;
                    context.Saga.StartedAt = DateTime.UtcNow;

                    // Resolve DbContext from the container (RLS automatically filters by tenant)
                    var serviceProvider = context.GetPayload<IServiceProvider>();
                    var dbContext = serviceProvider.GetRequiredService<PayrollDbContext>();

                    context.Saga.TotalEmployeesToProcess = await dbContext.PayrollProfiles
                        .CountAsync(p => p.IsActive)
                        .ConfigureAwait(false);
                })
                .TransitionTo(Calculating)
                .PublishAsync(context => context.Init<CalculateAllEmployeePayCommand>(new 
                { 
                    RunId = context.Saga.CorrelationId,
                    PeriodName = context.Saga.PeriodName 
                }))
        );

        During(Calculating,
            When(PayCalculated)
                .Then(context => 
                {
                    context.Saga.ProcessedEmployees++;
                    context.Saga.TotalGross += context.Message.Gross;
                    context.Saga.TotalNet += context.Message.NetPay;
                })
                // If Processed == Total, transition to PendingReview
                .If(context => context.Saga.ProcessedEmployees >= context.Saga.TotalEmployeesToProcess, 
                    binder => binder.TransitionTo(PendingReview))
        );

        During(PendingReview,
            When(LockCommand)
                .Then(context => 
                {
                    context.Saga.LockedBy = context.Message.ApprovedBy;
                    context.Saga.LockedAt = DateTime.UtcNow;
                    context.Saga.CompletedAt = DateTime.UtcNow;
                })
                .TransitionTo(Finalized)
                // Publish an internal integration event that the PublishPayslipsConsumer will handle
                .PublishAsync(context => context.Init<PayrollRunLockedIntegrationEvent>(new
                {
                    RunId = context.Saga.CorrelationId,
                    TenantId = context.Saga.TenantId,
                    PeriodName = context.Saga.PeriodName
                }))
        );
    }
}
