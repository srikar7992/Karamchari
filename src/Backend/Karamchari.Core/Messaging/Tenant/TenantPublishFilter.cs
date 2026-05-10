using System.Diagnostics;
using Karamchari.Core.Multitenancy.Execution;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Messaging.Tenant;

/// <summary>
/// A MassTransit filter that injects tenant execution envelope into message headers
/// when publishing messages. Ensures tenant context is propagated through the messaging system.
/// </summary>
public sealed class TenantPublishFilter : IFilter<PublishContext>
{
    private readonly ILogger<TenantPublishFilter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantPublishFilter"/> class.
    /// </summary>
    /// <param name="logger">The logger for tenant publish filter events.</param>
    public TenantPublishFilter(ILogger<TenantPublishFilter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task Send(PublishContext context, IPipe<PublishContext> next)
    {
        var executionContext = TenantExecutionContext.Current;

        if (executionContext == null)
        {
            _logger.LogWarning(
                "Publishing message without tenant context - no active tenant found. Destination: {Destination}",
                context.DestinationAddress);
        }
        else
        {
            InjectTenantHeaders(context, executionContext);

            _logger.LogInformation(
                "Tenant headers injected for message, TenantId {TenantId}, CorrelationId {CorrelationId}, Destination {Destination}",
                executionContext.TenantId, executionContext.CorrelationId, context.DestinationAddress);
        }

        return next.Send(context);
    }

    /// <inheritdoc />
    public void Probe(ProbeContext context)
    {
        context.CreateScope("tenant-publish-filter");
    }

    private static void InjectTenantHeaders(PublishContext context, TenantExecutionContext executionContext)
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
        services.AddSingleton(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<TenantPublishFilter>>();
            return new TenantPublishFilter(logger);
        });

        return services;
    }
}
