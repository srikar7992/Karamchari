// -----------------------------------------------------------------------
// <copyright file="WorkflowMetricsHandler.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Multitenancy.Execution;
using Karamchari.Core.Observability.Tenant;
using Karamchari.Workflow.Domain;
using Karamchari.Workflow.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Karamchari.Workflow.Services;

/// <summary>
/// Domain event handler that records operational metrics and establishes distributed
/// tracing spans for workflow execution transitions.
/// </summary>
public sealed class WorkflowMetricsHandler
{
    private readonly TenantMetricsCollector _metrics;
    private readonly TenantActivitySource _activitySource;
    private readonly ILogger<WorkflowMetricsHandler> _logger;

    public WorkflowMetricsHandler(
        TenantMetricsCollector metrics,
        TenantActivitySource activitySource,
        ILogger<WorkflowMetricsHandler> logger)
    {
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the workflow instance started event.
    /// </summary>
    public Task HandleAsync(WorkflowInstanceStarted @event, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(@event);

        using var activity = _activitySource.StartWorkflowActivity(
            "Workflow.Started",
            new TenantExecutionEnvelope(@event.TenantId, Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"), ExecutionSource.SagaOrchestration, TenantSource.Messaging),
            @event.InstanceId.ToString());

        _logger.LogWorkflowTransition(
            @event.InstanceId.ToString(),
            @event.TenantId,
            "None",
            "Started",
            "System");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the workflow step advanced event.
    /// </summary>
    public Task HandleAsync(WorkflowStepAdvanced @event, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(@event);

        using var activity = _activitySource.StartWorkflowActivity(
            "Workflow.StepAdvanced",
            new TenantExecutionEnvelope(@event.TenantId, Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"), ExecutionSource.SagaOrchestration, TenantSource.Messaging),
            @event.InstanceId.ToString(),
            @event.ToStep.ToString(CultureInfo.InvariantCulture));

        _logger.LogWorkflowTransition(
            @event.InstanceId.ToString(),
            @event.TenantId,
            @event.FromStep.ToString(CultureInfo.InvariantCulture),
            @event.ToStep.ToString(CultureInfo.InvariantCulture),
            "System");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the workflow instance completed event and records duration metrics.
    /// </summary>
    public Task HandleAsync(WorkflowInstanceCompleted @event, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(@event);

        using var activity = _activitySource.StartWorkflowActivity(
            "Workflow.Completed",
            new TenantExecutionEnvelope(@event.TenantId, Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"), ExecutionSource.SagaOrchestration, TenantSource.Messaging),
            @event.InstanceId.ToString());

        var duration = DateTimeOffset.UtcNow - @event.OccurredOnUtc;

        _metrics.RecordWorkflowDuration(
            @event.TenantId,
            @event.EntityType,
            duration.TotalSeconds);

        _logger.LogWorkflowTransition(
            @event.InstanceId.ToString(),
            @event.TenantId,
            "Active",
            @event.Status.ToString(),
            "System");

        return Task.CompletedTask;
    }
}
