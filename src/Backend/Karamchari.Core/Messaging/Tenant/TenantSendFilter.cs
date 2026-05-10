using System.Diagnostics;
using Karamchari.Core.Multitenancy.Execution;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Messaging.Tenant;

/// <summary>
/// A MassTransit filter that injects tenant execution envelope into message headers
/// when sending messages point-to-point. Ensures tenant context is propagated
/// through direct send operations.
/// </summary>
public sealed class TenantSendFilter : IFilter<SendContext>
{
    private readonly ILogger<TenantSendFilter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantSendFilter"/> class.
    /// </summary>
    /// <param name="logger">The logger for tenant send filter events.</param>
    public TenantSendFilter(ILogger<TenantSendFilter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task Send(SendContext context, IPipe<SendContext> next)
    {
        var executionContext = TenantExecutionContext.Current;

        if (executionContext == null)
        {
            _logger.LogWarning(
                "Sending message without tenant context - no active tenant found. Destination: {Destination}",
                context.DestinationAddress);
        }
        else
        {
            InjectTenantHeaders(context, executionContext);

            _logger.LogInformation(
                "Tenant headers injected for sent message, TenantId {TenantId}, CorrelationId {CorrelationId}, Destination {Destination}",
                executionContext.TenantId, executionContext.CorrelationId, context.DestinationAddress);
        }

        return next.Send(context);
    }

    /// <inheritdoc />
    public void Probe(ProbeContext context)
    {
        context.CreateScope("tenant-send-filter");
    }

    /// <summary>
    /// Injects tenant-specific headers into the message send context.
    /// </summary>
    /// <param name="context">The send context where headers will be injected.</param>
    /// <param name="executionContext">The current tenant execution context.</param>
    private static void InjectTenantHeaders(SendContext context, TenantExecutionContext executionContext)
    {
        var envelope = executionContext.Envelope;

        context.Headers.Set(TenantMessageHeaderKeys.TenantId, envelope.TenantId);
        context.Headers.Set(TenantMessageHeaderKeys.CorrelationId, envelope.CorrelationId);
        context.Headers.Set(TenantMessageHeaderKeys.RequestId, envelope.RequestId);
        context.Headers.Set(TenantMessageHeaderKeys.Timestamp, envelope.Timestamp);

        if (!string.IsNullOrWhiteSpace(envelope.TraceId))
        {
            context.Headers.Set(TenantMessageHeaderKeys.TraceId, envelope.TraceId);
        }

        if (!string.IsNullOrWhiteSpace(envelope.SpanId))
        {
            context.Headers.Set(TenantMessageHeaderKeys.SpanId, envelope.SpanId);
        }

        if (!string.IsNullOrWhiteSpace(envelope.UserIdentity))
        {
            context.Headers.Set(TenantMessageHeaderKeys.UserIdentity, envelope.UserIdentity);
        }

        context.Headers.Set(TenantMessageHeaderKeys.ExecutionSource, envelope.ExecutionSource.ToString());
        context.Headers.Set(TenantMessageHeaderKeys.RetryAttempt, envelope.RetryAttempt);
        context.Headers.Set(TenantMessageHeaderKeys.ExecutionEnvelope, envelope.ToJson());

        if (!string.IsNullOrWhiteSpace(envelope.ContentHash))
        {
            context.Headers.Set(TenantMessageHeaderKeys.ContentHash, envelope.ContentHash);
        }

        if (!string.IsNullOrWhiteSpace(envelope.WorkflowInstanceId))
        {
            context.Headers.Set(TenantMessageHeaderKeys.WorkflowInstanceId, envelope.WorkflowInstanceId);
        }

        if (!string.IsNullOrWhiteSpace(envelope.WorkflowStepId))
        {
            context.Headers.Set(TenantMessageHeaderKeys.WorkflowStepId, envelope.WorkflowStepId);
        }

        if (envelope.SlaDeadline.HasValue)
        {
            context.Headers.Set(TenantMessageHeaderKeys.WorkflowSlaDeadline, envelope.SlaDeadline.Value.ToString("O"));
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
        services.AddSingleton(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<TenantSendFilter>>();
            return new TenantSendFilter(logger);
        });

        return services;
    }
}
