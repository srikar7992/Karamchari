// -----------------------------------------------------------------------
// <copyright file="TenantLogEnricher.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Multitenancy.Execution;
using Karamchari.Core.Observability.Tenant;
using Serilog.Core;
using Serilog.Events;

namespace Karamchari.Core.Observability.Tenant;

/// <summary>
/// Serilog log event enricher that automatically pulls tenant and correlation metadata
/// from the active <see cref="TenantExecutionContext"/> and adds them as structured properties
/// to every log event.
/// </summary>
public sealed class TenantLogEnricher : ILogEventEnricher
{
    /// <summary>
    /// Enriches the log event with tenant-specific properties extracted from the current execution context.
    /// </summary>
    /// <param name="logEvent">The log event to enrich.</param>
    /// <param name="propertyFactory">The factory used to create new log properties.</param>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var context = TenantExecutionContext.Current;
        if (context == null)
        {
            return;
        }

        var envelope = context.Envelope;

        AddPropertyIfAbsent(logEvent, propertyFactory, TenantTelemetryTags.TenantId, envelope.TenantId);
        AddPropertyIfAbsent(logEvent, propertyFactory, "TenantId", envelope.TenantId);

        AddPropertyIfAbsent(logEvent, propertyFactory, TenantTelemetryTags.CorrelationId, envelope.CorrelationId);
        AddPropertyIfAbsent(logEvent, propertyFactory, "CorrelationId", envelope.CorrelationId);

        AddPropertyIfAbsent(logEvent, propertyFactory, TenantTelemetryTags.RequestId, envelope.RequestId);
        AddPropertyIfAbsent(logEvent, propertyFactory, TenantTelemetryTags.ExecutionSource, envelope.ExecutionSource.ToString());

        if (!string.IsNullOrEmpty(envelope.UserIdentity))
        {
            AddPropertyIfAbsent(logEvent, propertyFactory, TenantTelemetryTags.UserId, envelope.UserIdentity);
            AddPropertyIfAbsent(logEvent, propertyFactory, "UserId", envelope.UserIdentity);
        }

        if (!string.IsNullOrEmpty(envelope.WorkflowInstanceId))
        {
            AddPropertyIfAbsent(logEvent, propertyFactory, TenantTelemetryTags.WorkflowInstanceId, envelope.WorkflowInstanceId);
        }

        if (!string.IsNullOrEmpty(envelope.WorkflowStepId))
        {
            AddPropertyIfAbsent(logEvent, propertyFactory, TenantTelemetryTags.WorkflowStepId, envelope.WorkflowStepId);
        }
    }

    private static void AddPropertyIfAbsent(
        LogEvent logEvent,
        ILogEventPropertyFactory propertyFactory,
        string name,
        object? value)
    {
        if (value == null) return;

        var property = propertyFactory.CreateProperty(name, value);
        logEvent.AddPropertyIfAbsent(property);
    }
}
