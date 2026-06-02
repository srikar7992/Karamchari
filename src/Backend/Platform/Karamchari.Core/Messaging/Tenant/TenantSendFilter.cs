using System.Diagnostics;
using Karamchari.Core.Multitenancy.Execution;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Messaging.Tenant;

/// <summary>
/// A MassTransit filter that injects tenant execution context into message headers during Send operations.
/// Generic version required for correct filter application in MassTransit 8.x.
/// </summary>
public sealed class TenantSendFilter<T> : IFilter<SendContext<T>>
    where T : class
{
    private readonly ILogger<TenantSendFilter<T>> _logger;
    private readonly ExecutionContextSigner _signer;

    public TenantSendFilter(ILogger<TenantSendFilter<T>> logger, ExecutionContextSigner signer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _signer = signer ?? throw new ArgumentNullException(nameof(signer));
    }

    public Task Send(SendContext<T> context, IPipe<SendContext<T>> next)
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
                        "Tenant headers recovered from ConsumeContext for message type {MessageType}, TenantId {TenantId}",
                        typeof(T).Name, tenantId);
                }
            }
        }
        else
        {
            TenantSendFilter.InjectAndSignTenantHeaders(context, executionContext, _signer);

            _logger.LogTrace(
                "Tenant headers signed and injected for message type {MessageType}, TenantId {TenantId}, CorrelationId {CorrelationId}",
                typeof(T).Name, executionContext.TenantId, executionContext.CorrelationId);
        }

        return next.Send(context);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("tenant-send-filter");
    }
}

/// <summary>
/// A MassTransit filter that injects tenant execution context into message headers during Send operations.
/// Non-generic version for broader coverage.
/// </summary>
public sealed class TenantSendFilter : IFilter<SendContext>
{
    private readonly ILogger<TenantSendFilter> _logger;
    private readonly ExecutionContextSigner _signer;

    public TenantSendFilter(ILogger<TenantSendFilter> logger, ExecutionContextSigner signer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _signer = signer ?? throw new ArgumentNullException(nameof(signer));
    }

    public Task Send(SendContext context, IPipe<SendContext> next)
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
        context.CreateScope("tenant-send-filter-base");
    }

    internal static void InjectAndSignTenantHeaders(SendContext context, TenantExecutionContext executionContext, ExecutionContextSigner signer)
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
/// Extension methods for registering tenant send filter with MassTransit.
/// </summary>
public static class TenantSendFilterExtensions
{
    /// <summary>
    /// Registers the tenant send filter dependencies with the service collection.
    /// </summary>
    public static IServiceCollection AddTenantSendFilter(this IServiceCollection services)
    {
        services.AddSingleton(typeof(TenantSendFilter<>));
        services.AddSingleton<TenantSendFilter>();
        return services;
    }
}
