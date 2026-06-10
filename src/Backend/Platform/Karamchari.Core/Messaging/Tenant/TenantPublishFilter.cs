// -----------------------------------------------------------------------
// <copyright file="TenantPublishFilter.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using Karamchari.Core.Multitenancy.Execution;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Messaging.Tenant;

/// <summary>
/// A MassTransit filter that injects tenant execution context into message headers.
/// Generic version required for Entity Framework Outbox capture in MassTransit 8.x.
/// </summary>
public sealed class TenantPublishFilter<T> : IFilter<PublishContext<T>>
    where T : class
{
    private readonly ILogger<TenantPublishFilter<T>> _logger;
    private readonly ExecutionContextSigner _signer;

    public TenantPublishFilter(ILogger<TenantPublishFilter<T>> logger, ExecutionContextSigner signer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _signer = signer ?? throw new ArgumentNullException(nameof(signer));
    }

    public Task Send(PublishContext<T> context, IPipe<PublishContext<T>> next)
    {
        var executionContext = TenantExecutionContext.Current;

        if (executionContext == null)
        {
            if (context.TryGetPayload<ConsumeContext>(out var consumeContext))
            {
                if (consumeContext.Headers.TryGetHeader(ExecutionContextHeaders.TenantId, out var tenantIdObj) &&
                    tenantIdObj is string tenantId)
                {
                    context.Headers.Set(ExecutionContextHeaders.TenantId, tenantId);

                    if (consumeContext.Headers.TryGetHeader(ExecutionContextHeaders.CorrelationId, out var correlationIdObj))
                    {
                        context.Headers.Set(ExecutionContextHeaders.CorrelationId, correlationIdObj);
                    }

                    if (consumeContext.Headers.TryGetHeader(ExecutionContextHeaders.ExecutionEnvelope, out var envelopeObj))
                    {
                        context.Headers.Set(ExecutionContextHeaders.ExecutionEnvelope, envelopeObj);
                    }

                    // Recover other Tier 2/3 if possible, but the signature from the original message is already lost or invalid for a new Fault
                    // For now, we don't re-sign Faults unless we have a full envelope.
                    // BUT! We should sign the Fault as a NEW infrastructure event.

                    _logger.LogDebug(
                        "Tenant headers recovered from ConsumeContext for message type {MessageType}, TenantId {TenantId}",
                        typeof(T).Name, tenantId);
                }
            }
        }
        else
        {
            TenantPublishFilter.InjectAndSignTenantHeaders(context, executionContext, _signer);

            _logger.LogTrace(
                "Tenant headers signed and injected for message type {MessageType}, TenantId {TenantId}, CorrelationId {CorrelationId}",
                typeof(T).Name, executionContext.TenantId, executionContext.CorrelationId);
        }

        return next.Send(context);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("tenant-publish-filter");
    }
}

/// <summary>
/// A MassTransit filter that injects tenant execution context into message headers.
/// Non-generic version for broader coverage including Faults.
/// </summary>
public sealed class TenantPublishFilter : IFilter<PublishContext>
{
    private readonly ILogger<TenantPublishFilter> _logger;
    private readonly ExecutionContextSigner _signer;

    public TenantPublishFilter(ILogger<TenantPublishFilter> logger, ExecutionContextSigner signer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _signer = signer ?? throw new ArgumentNullException(nameof(signer));
    }

    public Task Send(PublishContext context, IPipe<PublishContext> next)
    {
        var executionContext = TenantExecutionContext.Current;

        if (executionContext == null)
        {
            if (context.TryGetPayload<ConsumeContext>(out var consumeContext))
            {
                if (consumeContext.Headers.TryGetHeader(ExecutionContextHeaders.TenantId, out var tenantIdObj) &&
                    tenantIdObj is string tenantId)
                {
                    context.Headers.Set(ExecutionContextHeaders.TenantId, tenantId);

                    if (consumeContext.Headers.TryGetHeader(ExecutionContextHeaders.CorrelationId, out var correlationIdObj))
                    {
                        context.Headers.Set(ExecutionContextHeaders.CorrelationId, correlationIdObj);
                    }

                    if (consumeContext.Headers.TryGetHeader(ExecutionContextHeaders.ExecutionEnvelope, out var envelopeObj))
                    {
                        context.Headers.Set(ExecutionContextHeaders.ExecutionEnvelope, envelopeObj);
                    }

                    _logger.LogTrace(
                        "Tenant headers recovered from ConsumeContext for TenantId {TenantId}",
                        tenantId);
                }
            }
        }
        else
        {
            InjectAndSignTenantHeaders(context, executionContext, _signer);

            _logger.LogTrace(
                "Tenant headers signed and injected for TenantId {TenantId}, CorrelationId {CorrelationId}",
                executionContext.TenantId, executionContext.CorrelationId);
        }

        return next.Send(context);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("tenant-publish-filter-base");
    }

    internal static void InjectAndSignTenantHeaders(PublishContext context, TenantExecutionContext executionContext, ExecutionContextSigner signer)
    {
        var envelope = executionContext.Envelope;
        var conversationId = context.ConversationId?.ToString() ?? Guid.NewGuid().ToString();
        var sourceService = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? "Karamchari.Platform";
        var timestamp = envelope.Timestamp.ToString("O");

        context.Headers.Set(ExecutionContextHeaders.TenantId, envelope.TenantId);
        context.Headers.Set(ExecutionContextHeaders.CorrelationId, envelope.CorrelationId);
        context.Headers.Set(ExecutionContextHeaders.ConversationId, conversationId);
        context.Headers.Set(ExecutionContextHeaders.RequestId, envelope.RequestId);
        context.Headers.Set(ExecutionContextHeaders.Timestamp, timestamp);
        context.Headers.Set(ExecutionContextHeaders.SourceService, sourceService);
        context.Headers.Set(ExecutionContextHeaders.ExecutionContextVersion, "1.0");

        var signature = signer.Sign(envelope.TenantId, envelope.CorrelationId, conversationId, sourceService, timestamp);
        context.Headers.Set(ExecutionContextHeaders.ExecutionContextSignature, signature);

        if (!string.IsNullOrWhiteSpace(envelope.TraceId))
        {
            context.Headers.Set(ExecutionContextHeaders.TraceId, envelope.TraceId);
        }

        if (!string.IsNullOrWhiteSpace(envelope.SpanId))
        {
            context.Headers.Set(ExecutionContextHeaders.SpanId, envelope.SpanId);
        }

        if (!string.IsNullOrWhiteSpace(envelope.UserIdentity))
        {
            context.Headers.Set(ExecutionContextHeaders.UserIdentity, envelope.UserIdentity);
        }

        context.Headers.Set(ExecutionContextHeaders.ExecutionSource, envelope.ExecutionSource.ToString());
        context.Headers.Set(ExecutionContextHeaders.RetryAttempt, envelope.RetryAttempt);
        context.Headers.Set(ExecutionContextHeaders.ExecutionEnvelope, envelope.ToJson());

        if (!string.IsNullOrWhiteSpace(envelope.ContentHash))
        {
            context.Headers.Set(ExecutionContextHeaders.ContentHash, envelope.ContentHash);
        }

        if (!string.IsNullOrWhiteSpace(envelope.WorkflowInstanceId))
        {
            context.Headers.Set(ExecutionContextHeaders.WorkflowInstanceId, envelope.WorkflowInstanceId);
        }

        if (!string.IsNullOrWhiteSpace(envelope.WorkflowStepId))
        {
            context.Headers.Set(ExecutionContextHeaders.WorkflowStepId, envelope.WorkflowStepId);
        }

        if (envelope.SlaDeadline.HasValue)
        {
            context.Headers.Set(ExecutionContextHeaders.WorkflowSlaDeadline, envelope.SlaDeadline.Value.ToString("O"));
        }
    }
}

/// <summary>
/// Extension methods for registering tenant publish filter with MassTransit.
/// </summary>
public static class TenantPublishFilterExtensions
{
    /// <summary>
    /// Registers the tenant publish filter dependencies with the service collection.
    /// </summary>
    public static IServiceCollection AddTenantPublishFilter(this IServiceCollection services)
    {
        services.AddSingleton(typeof(TenantPublishFilter<>));
        services.AddSingleton<TenantPublishFilter>();
        return services;
    }
}
