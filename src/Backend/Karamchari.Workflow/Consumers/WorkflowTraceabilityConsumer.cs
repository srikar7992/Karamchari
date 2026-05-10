using Karamchari.Core.Messaging;
using Karamchari.Workflow.Domain.Events;
using Karamchari.Workflow.Services;
using MassTransit;

namespace Karamchari.Workflow.Consumers;

/// <summary>
/// MassTransit consumer that listens to workflow domain events and routes them
/// to the <see cref="WorkflowMetricsHandler"/> for telemetry, logging, and tracing.
/// Enables platform-wide workflow execution visibility.
/// </summary>
public sealed class WorkflowTraceabilityConsumer :
    IConsumer<EnterpriseEventEnvelope<WorkflowInstanceStarted>>,
    IConsumer<EnterpriseEventEnvelope<WorkflowStepAdvanced>>,
    IConsumer<EnterpriseEventEnvelope<WorkflowInstanceCompleted>>
{
    private readonly WorkflowMetricsHandler _metricsHandler;

    public WorkflowTraceabilityConsumer(WorkflowMetricsHandler metricsHandler)
    {
        _metricsHandler = metricsHandler ?? throw new ArgumentNullException(nameof(metricsHandler));
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<EnterpriseEventEnvelope<WorkflowInstanceStarted>> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        await _metricsHandler.HandleAsync(context.Message.Payload, context.CancellationToken);
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<EnterpriseEventEnvelope<WorkflowStepAdvanced>> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        await _metricsHandler.HandleAsync(context.Message.Payload, context.CancellationToken);
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<EnterpriseEventEnvelope<WorkflowInstanceCompleted>> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        await _metricsHandler.HandleAsync(context.Message.Payload, context.CancellationToken);
    }
}
