using System.Diagnostics;
using Karamchari.Core.Multitenancy.Execution;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Messaging.Tenant;

/// <summary>
/// MassTransit consume filter that extracts, validates, and establishes the tenant execution context
/// from incoming message headers. Enforces replay protection and stale message detection.
/// </summary>
public sealed class TenantConsumeFilter : IFilter<ConsumeContext>
{
    private readonly ILogger<TenantConsumeFilter> _logger;
    private readonly ReplayProtectionService _replayProtection;
    private readonly TimeSpan _staleMessageThreshold;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantConsumeFilter"/> class.
    /// </summary>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <param name="replayProtection">The service used to detect message replays.</param>
    /// <param name="staleMessageThreshold">The maximum age allowed for a message before it's rejected.</param>
    public TenantConsumeFilter(
        ILogger<TenantConsumeFilter> logger,
        ReplayProtectionService replayProtection,
        TimeSpan? staleMessageThreshold = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _replayProtection = replayProtection ?? throw new ArgumentNullException(nameof(replayProtection));
        _staleMessageThreshold = staleMessageThreshold ?? TimeSpan.FromMinutes(5);
    }

    /// <inheritdoc/>
    public async Task Send(ConsumeContext context, IPipe<ConsumeContext> next)
    {
        _logger.LogInformation(
            "Processing message {MessageId} with ConversationId {ConversationId}",
            context.MessageId, context.ConversationId);

        try
        {
            // 1. Extract tenant identity
            if (!context.Headers.TryGetHeader(TenantMessageHeaderKeys.TenantId, out var tenantIdObj) ||
                tenantIdObj is not string tenantId || string.IsNullOrWhiteSpace(tenantId))
            {
                _logger.LogWarning("Message rejected: Missing TenantId. ConversationId: {ConversationId}", context.ConversationId);
                throw new MalformedTenantMessageException(
                    "Message is missing required tenant identifier",
                    null, TenantMessageFailureReason.MissingTenantId);
            }

            // 2. Validate timestamp (stale message detection)
            context.Headers.TryGetHeader(TenantMessageHeaderKeys.Timestamp, out var timestampObj);
            if (!ValidateTimestamp(timestampObj))
            {
                _logger.LogWarning("Message rejected: Stale message for Tenant {TenantId}", tenantId);
                throw new MalformedTenantMessageException(
                    "Message timestamp is stale",
                    tenantId, TenantMessageFailureReason.StaleMessage);
            }

            // 3. Replay protection
            context.Headers.TryGetHeader(TenantMessageHeaderKeys.OriginalMessageId, out var originalMessageIdObj);
            context.Headers.TryGetHeader(TenantMessageHeaderKeys.ContentHash, out var contentHashObj);
            var originalMessageId = originalMessageIdObj as string ?? context.MessageId?.ToString();
            var contentHash = contentHashObj as string;

            if (!string.IsNullOrWhiteSpace(originalMessageId) && _replayProtection.IsReplay(originalMessageId, contentHash))
            {
                _logger.LogWarning("Message rejected: Replay attack detected for Tenant {TenantId}, MessageId {MessageId}",
                    tenantId, originalMessageId);
                throw new MalformedTenantMessageException(
                    "Message appears to be a replay and will be rejected",
                    tenantId, TenantMessageFailureReason.ReplayAttack);
            }

            // 4. Rehydrate execution context
            TenantExecutionEnvelope? envelope = null;
            if (context.Headers.TryGetHeader(TenantMessageHeaderKeys.ExecutionEnvelope, out var envelopeObj) &&
                envelopeObj is string envelopeJson)
            {
                envelope = TenantExecutionEnvelope.FromJson(envelopeJson);
            }

            if (envelope == null)
            {
                // Fallback for messages without full envelopes
                context.Headers.TryGetHeader(TenantMessageHeaderKeys.CorrelationId, out var correlationIdObj);
                context.Headers.TryGetHeader(TenantMessageHeaderKeys.TraceId, out var traceIdObj);
                context.Headers.TryGetHeader(TenantMessageHeaderKeys.SpanId, out var spanIdObj);
                context.Headers.TryGetHeader(TenantMessageHeaderKeys.UserIdentity, out var userIdentityObj);
                context.Headers.TryGetHeader(TenantMessageHeaderKeys.RetryAttempt, out var retryAttemptObj);

                var correlationId = correlationIdObj as string ?? context.ConversationId?.ToString() ?? Guid.NewGuid().ToString();
                var requestId = context.MessageId?.ToString() ?? Guid.NewGuid().ToString();

                envelope = new TenantExecutionEnvelope(tenantId, correlationId, requestId, ExecutionSource.MessageConsumer)
                {
                    TraceId = traceIdObj as string,
                    SpanId = spanIdObj as string,
                    UserIdentity = userIdentityObj as string,
                    RetryAttempt = retryAttemptObj is int ra ? ra : 0,
                    MessageId = originalMessageId,
                    ContentHash = contentHash
                };
            }

            var executionContext = new TenantExecutionContext(envelope);
            using var scope = executionContext.Establish();

            _logger.LogInformation(
                "Tenant context established for Tenant {TenantId}, CorrelationId {CorrelationId}",
                tenantId, executionContext.CorrelationId);

            await next.Send(context);

            // 5. Record successful processing for replay prevention
            if (!string.IsNullOrWhiteSpace(originalMessageId))
            {
                _replayProtection.RecordProcessed(originalMessageId, contentHash);
            }
        }
        catch (MalformedTenantMessageException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error establishing tenant context for message");
            throw new MalformedTenantMessageException(
                "Unexpected error during tenant context validation",
                null, TenantMessageFailureReason.MalformedEnvelope);
        }
    }

    /// <inheritdoc/>
    public void Probe(ProbeContext context)
    {
        context.CreateScope("tenant-consume-filter");
    }

    private bool ValidateTimestamp(object? timestampObj)
    {
        DateTime? timestamp = null;

        if (timestampObj is DateTime dt)
        {
            timestamp = dt;
        }
        else if (timestampObj is string str && DateTime.TryParse(str, out var parsed))
        {
            timestamp = parsed;
        }

        if (!timestamp.HasValue)
        {
            return true;
        }

        var age = DateTime.UtcNow - timestamp.Value;
        return age <= _staleMessageThreshold;
    }
}

/// <summary>
/// Extension methods for registering tenant consume filters.
/// </summary>
public static class TenantConsumeFilterExtensions
{
    /// <summary>
    /// Adds the tenant consume filter to the service collection.
    /// </summary>
    public static IServiceCollection AddTenantConsumeFilter(this IServiceCollection services, TimeSpan? staleMessageThreshold = null)
    {
        services.AddSingleton(sp =>
            new TenantConsumeFilter(
                sp.GetRequiredService<ILogger<TenantConsumeFilter>>(),
                sp.GetRequiredService<ReplayProtectionService>(),
                staleMessageThreshold));

        return services;
    }
}
