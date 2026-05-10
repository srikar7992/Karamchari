using System.Diagnostics;
using System.Text.Json;
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
    private readonly Func<TenantExecutionContext?> _contextResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantSendFilter"/> class.
    /// </summary>
    /// <param name="logger">The logger for tenant send filter events.</param>
    /// <param name="contextResolver">A function to resolve the current tenant execution context.</param>
    public TenantSendFilter(
        ILogger<TenantSendFilter> logger,
        Func<TenantExecutionContext?> contextResolver)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(contextResolver);

        _logger = logger;
        _contextResolver = contextResolver;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantSendFilter"/> class using static context.
    /// </summary>
    /// <param name="logger">The logger for tenant send filter events.</param>
    public TenantSendFilter(ILogger<TenantSendFilter> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        _contextResolver = () => TenantExecutionContext.Current;
    }

    /// <inheritdoc />
    public Task Send(SendContext context, IPipe<SendContext> next)
    {
        var executionContext = _contextResolver();

        if (executionContext == null)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(
                    "Sending message without tenant context - no active tenant found. Destination: {Destination}",
                    context.DestinationAddress);
            }
        }
        else
        {
            InjectTenantHeaders(context, executionContext);

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "Tenant headers injected for sent message, TenantId {TenantId}, CorrelationId {CorrelationId}, Destination {Destination}",
                    executionContext.TenantId, executionContext.CorrelationId, context.DestinationAddress);
            }
        }

        return next.Send(context);
    }

    /// <inheritdoc />
    public void Probe(ProbeContext context)
    {
        context.CreateScope("tenant-send-filter");
    }

    private static void InjectTenantHeaders(SendContext context, TenantExecutionContext executionContext)
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
