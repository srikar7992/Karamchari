using System.Diagnostics;
using System.Text.Json;
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
    private readonly Func<TenantExecutionContext?> _contextResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantPublishFilter"/> class.
    /// </summary>
    /// <param name="logger">The logger for tenant publish filter events.</param>
    /// <param name="contextResolver">A function to resolve the current tenant execution context.</param>
    public TenantPublishFilter(
        ILogger<TenantPublishFilter> logger,
        Func<TenantExecutionContext?> contextResolver)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(contextResolver);

        _logger = logger;
        _contextResolver = contextResolver;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantPublishFilter"/> class using static context.
    /// </summary>
    /// <param name="logger">The logger for tenant publish filter events.</param>
    public TenantPublishFilter(ILogger<TenantPublishFilter> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        _contextResolver = () => TenantExecutionContext.Current;
    }

    /// <inheritdoc />
    public Task Send(PublishContext context, IPipe<PublishContext> next)
    {
        var executionContext = _contextResolver();

        if (executionContext == null)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(
                    "Publishing message without tenant context - no active tenant found. Destination: {Destination}",
                    context.DestinationAddress);
            }
        }
        else
        {
            InjectTenantHeaders(context, executionContext);

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "Tenant headers injected for message, TenantId {TenantId}, CorrelationId {CorrelationId}, Destination {Destination}",
                    executionContext.TenantId, executionContext.CorrelationId, context.DestinationAddress);
            }
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
        context.Headers.Set(TenantMessageHeaderKeys.TenantId, executionContext.TenantId);
        context.Headers.Set(TenantMessageHeaderKeys.CorrelationId, executionContext.CorrelationId);

        if (!string.IsNullOrWhiteSpace(executionContext.TraceId))
        {
            context.Headers.Set(TenantMessageHeaderKeys.TraceId, executionContext.TraceId);
        }

        if (!string.IsNullOrWhiteSpace(executionContext.SpanId))
        {
            context.Headers.Set(TenantMessageHeaderKeys.SpanId, executionContext.SpanId);
        }

        if (!string.IsNullOrWhiteSpace(executionContext.UserIdentity))
        {
            context.Headers.Set(TenantMessageHeaderKeys.UserIdentity, executionContext.UserIdentity);
        }

        if (!string.IsNullOrWhiteSpace(executionContext.ExecutionSource))
        {
            context.Headers.Set(TenantMessageHeaderKeys.ExecutionSource, executionContext.ExecutionSource);
        }

        if (!string.IsNullOrWhiteSpace(executionContext.OriginalMessageId))
        {
            context.Headers.Set(TenantMessageHeaderKeys.OriginalMessageId, executionContext.OriginalMessageId);
        }

        context.Headers.Set(TenantMessageHeaderKeys.RetryAttempt, executionContext.RetryAttempt);
        context.Headers.Set(TenantMessageHeaderKeys.Timestamp, DateTime.UtcNow);

        context.Headers.Set(
            TenantMessageHeaderKeys.ExecutionEnvelope,
            executionContext.ToEnvelope().ToJson());

        if (context.InitiatorId != null)
        {
            context.Headers.Set(TenantMessageHeaderKeys.RequestId, context.InitiatorId);
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
