using System.Diagnostics;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Multitenancy.Execution;
using Karamchari.Core.Observability.Tenant;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Karamchari.Core.Messaging.Tenant;

/// <summary>
/// MassTransit consume filter that extracts, validates, and establishes the tenant execution context
/// from incoming message headers. Enforces replay protection, stale message detection, and SLA monitoring.
/// </summary>
public sealed class TenantConsumeFilter<T> : IFilter<ConsumeContext<T>>
    where T : class
{
    private readonly ILogger<TenantConsumeFilter<T>> _logger;
    private readonly ReplayProtectionService _replayProtection;
    private readonly TenantMetricsCollector _metrics;
    private readonly ExecutionContextSigner _signer;
    private readonly TimeSpan _staleMessageThreshold;
    private readonly bool _enforceValidation;

    public TenantConsumeFilter(
        ILogger<TenantConsumeFilter<T>> logger,
        ReplayProtectionService replayProtection,
        TenantMetricsCollector metrics,
        ExecutionContextSigner signer,
        IConfiguration? configuration = null,
        TimeSpan? staleMessageThreshold = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _replayProtection = replayProtection ?? throw new ArgumentNullException(nameof(replayProtection));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _signer = signer ?? throw new ArgumentNullException(nameof(signer));
        _staleMessageThreshold = staleMessageThreshold ?? TimeSpan.FromMinutes(5);

        var mode = configuration?["Messaging:ExecutionContextValidationMode"] ?? "Enforce";
        _enforceValidation = !string.Equals(mode, "AuditOnly", StringComparison.OrdinalIgnoreCase);
    }

    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        var startTime = Stopwatch.GetTimestamp();
        _logger.LogTrace(
            "Processing message {MessageId} with ConversationId {ConversationId}. Source: {SourceAddress}",
            context.MessageId, context.ConversationId, context.SourceAddress);

        TenantExecutionContext? executionContext = null;
        string? tenantId = null;
        string? contentHash = null;
        string? originalMessageId = null;

        try
        {
            // 1. Extract tenant identity
            if (context.Headers.TryGetHeader(ExecutionContextHeaders.TenantId, out var tenantIdObj) &&
                tenantIdObj is string tid && !string.IsNullOrWhiteSpace(tid))
            {
                tenantId = tid;
            }
            else
            {
                var headersString = string.Join(", ", context.Headers.GetAll().Select(h => $"{h.Key}={h.Value}"));
                _logger.LogWarning(
                    "Message missing required TenantId. ConversationId: {ConversationId}, Available Headers: {Headers}",
                    context.ConversationId,
                    headersString);

                _metrics.RecordEventRejection(null, typeof(T).Name, "MissingTenantId");

                if (_enforceValidation)
                {
                    throw new MalformedTenantMessageException(
                        "Message is missing required tenant identifier",
                        null, TenantMessageFailureReason.MissingTenantId);
                }
            }

            var safeTenantId = tenantId ?? "NULL";

            // 2. Cryptographic Integrity Check (D1-B)
            string? signature = null;
            if (!context.Headers.TryGetHeader(ExecutionContextHeaders.ExecutionContextSignature, out var signatureObj) ||
                signatureObj is not string sig)
            {
                _logger.LogWarning("Execution context signature missing for Tenant {TenantId}", safeTenantId);
                _metrics.RecordEventRejection(safeTenantId, typeof(T).Name, "MissingSignature");
                
                if (_enforceValidation)
                {
                    throw new MalformedTenantMessageException(
                        "Message is missing required cryptographic signature",
                        safeTenantId, TenantMessageFailureReason.InvalidSignature);
                }
            }
            else
            {
                signature = sig;
            }

            context.Headers.TryGetHeader(ExecutionContextHeaders.CorrelationId, out var correlationIdObj);
            context.Headers.TryGetHeader(ExecutionContextHeaders.ConversationId, out var conversationIdObj);
            context.Headers.TryGetHeader(ExecutionContextHeaders.SourceService, out var sourceServiceObj);
            context.Headers.TryGetHeader(ExecutionContextHeaders.Timestamp, out var timestampObj);

            var correlationId = correlationIdObj?.ToString() ?? string.Empty;
            var conversationId = conversationIdObj?.ToString() ?? string.Empty;
            var sourceService = sourceServiceObj?.ToString() ?? string.Empty;
            var timestampStr = timestampObj switch
            {
                DateTime dt => dt.ToString("O"),
                DateTimeOffset dto => dto.ToString("O"),
                _ => timestampObj?.ToString() ?? string.Empty
            };

            if (signature != null && !_signer.Validate(signature, safeTenantId, correlationId, conversationId, sourceService, timestampStr))
            {
                _logger.LogWarning(
                    "Signature validation failed for Tenant {TenantId}. Possible tampering or manual injection detected.",
                    safeTenantId);
                _metrics.RecordEventRejection(safeTenantId, typeof(T).Name, "InvalidSignature");
                
                if (_enforceValidation)
                {
                    throw new MalformedTenantMessageException(
                        "Message cryptographic signature is invalid",
                        safeTenantId, TenantMessageFailureReason.InvalidSignature);
                }
            }

            _logger.LogTrace(
                "Signature validated or bypassed for Tenant {TenantId}, CorrelationId {CorrelationId}",
                safeTenantId, correlationId);

            // 3. Validate timestamp (stale message detection)
            if (!ValidateTimestamp(timestampObj))
            {
                _logger.LogWarning("Stale message detected for Tenant {TenantId}", safeTenantId);
                _metrics.RecordEventRejection(safeTenantId, typeof(T).Name, "StaleMessage");
                
                if (_enforceValidation)
                {
                    throw new MalformedTenantMessageException(
                        "Message timestamp is stale",
                        safeTenantId, TenantMessageFailureReason.StaleMessage);
                }
            }

            // 4. Replay protection
            context.Headers.TryGetHeader(ExecutionContextHeaders.OriginalMessageId, out var originalMessageIdObj);
            context.Headers.TryGetHeader(ExecutionContextHeaders.ContentHash, out var contentHashObj);
            originalMessageId = originalMessageIdObj as string ?? context.MessageId?.ToString();
            contentHash = contentHashObj as string;

            if (!string.IsNullOrWhiteSpace(originalMessageId) && _replayProtection.IsReplay(originalMessageId, contentHash))
            {
                _logger.LogWarning("Replay attack detected for Tenant {TenantId}, MessageId {MessageId}",
                    safeTenantId, originalMessageId);
                _metrics.RecordReplayDetection(safeTenantId, typeof(T).Name);
                _metrics.RecordEventRejection(safeTenantId, typeof(T).Name, "ReplayAttack");
                
                if (_enforceValidation)
                {
                    throw new MalformedTenantMessageException(
                        "Message appears to be a replay and will be rejected",
                        safeTenantId, TenantMessageFailureReason.ReplayAttack);
                }
            }

            // 5. Rehydrate execution context
            TenantExecutionEnvelope? envelope = null;
            if (context.Headers.TryGetHeader(ExecutionContextHeaders.ExecutionEnvelope, out var envelopeObj) &&
                envelopeObj is string envelopeJson)
            {
                envelope = TenantExecutionEnvelope.FromJson(envelopeJson);
            }

            if (envelope == null)
            {
                // Fallback for messages without full envelopes (though signature check now requires core headers)
                context.Headers.TryGetHeader(ExecutionContextHeaders.TraceId, out var traceIdObj);
                context.Headers.TryGetHeader(ExecutionContextHeaders.SpanId, out var spanIdObj);
                context.Headers.TryGetHeader(ExecutionContextHeaders.UserIdentity, out var userIdentityObj);
                context.Headers.TryGetHeader(ExecutionContextHeaders.RetryAttempt, out var retryAttemptObj);
                context.Headers.TryGetHeader(ExecutionContextHeaders.WorkflowInstanceId, out var workflowInstanceIdObj);
                context.Headers.TryGetHeader(ExecutionContextHeaders.WorkflowStepId, out var workflowStepIdObj);
                context.Headers.TryGetHeader(ExecutionContextHeaders.WorkflowSlaDeadline, out var workflowSlaDeadlineObj);

                var requestId = context.MessageId?.ToString() ?? Guid.NewGuid().ToString();

                DateTime? slaDeadline = null;
                if (workflowSlaDeadlineObj is DateTime dt2) slaDeadline = dt2;
                else if (workflowSlaDeadlineObj is string s && DateTime.TryParse(s, out var p)) slaDeadline = p;

                envelope = new TenantExecutionEnvelope(safeTenantId, correlationId, requestId, ExecutionSource.MessageConsumer, TenantSource.Messaging)
                {
                    TraceId = traceIdObj as string,
                    SpanId = spanIdObj as string,
                    UserIdentity = userIdentityObj as string,
                    RetryAttempt = retryAttemptObj is int ra ? ra : 0,
                    MessageId = originalMessageId,
                    ContentHash = contentHash,
                    WorkflowInstanceId = workflowInstanceIdObj as string,
                    WorkflowStepId = workflowStepIdObj as string,
                    SlaDeadline = slaDeadline
                };
            }

            executionContext = new TenantExecutionContext(envelope!);
        }
        catch (MalformedTenantMessageException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error establishing tenant context for message");
            _metrics.RecordEventRejection(tenantId, typeof(T).Name, "MalformedEnvelope");
            
            if (_enforceValidation)
            {
                throw new MalformedTenantMessageException(
                    "Unexpected error during tenant context validation",
                    tenantId, TenantMessageFailureReason.MalformedEnvelope);
            }
            
            // If AuditOnly, we must try to construct SOME context or skip
            if (executionContext == null && tenantId != null)
            {
                executionContext = new TenantExecutionContext(new TenantExecutionEnvelope(tenantId, Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), ExecutionSource.MessageConsumer, TenantSource.Messaging));
            }
        }

        // Establish context and continue to next pipe (Consumer)
        if (executionContext == null)
        {
            await next.Send(context);
            return;
        }

        using var scope = executionContext.Establish();

        // 6. SLA Monitoring
        if (executionContext.Envelope.SlaDeadline.HasValue && executionContext.Envelope.SlaDeadline.Value < DateTime.UtcNow)
        {
            _metrics.RecordWorkflowSlaBreach(executionContext.TenantId, "Messaging", context.SupportedMessageTypes.FirstOrDefault() ?? "Unknown");
            _logger.LogWarning("SLA Breach detected for Tenant {TenantId}, Message {MessageId}, Deadline {Deadline}",
                executionContext.TenantId, originalMessageId, executionContext.Envelope.SlaDeadline.Value);
        }

        _logger.LogTrace(
            "Tenant context established and signature validated for Tenant {TenantId}, CorrelationId {CorrelationId}",
            executionContext.TenantId, executionContext.CorrelationId);

        try
        {
            await next.Send(context);
            
            // 7. Record successful processing for replay protection
            if (!string.IsNullOrWhiteSpace(originalMessageId))
            {
                _replayProtection.RecordProcessed(originalMessageId, contentHash);
            }

            // 8. Record processing latency
            var elapsedMs = Stopwatch.GetElapsedTime(startTime).TotalMilliseconds;
            _metrics.RecordEventProcessingLatency(executionContext.TenantId, typeof(T).Name, elapsedMs);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex,
                "Consumer execution failed for Tenant {TenantId}, CorrelationId {CorrelationId}, MessageType {MessageType}. This is expected to be handled by MassTransit.",
                executionContext.TenantId, executionContext.CorrelationId, typeof(T).Name);
            throw;
        }
    }

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

public static class TenantConsumeFilterExtensions
{
    public static IServiceCollection AddTenantConsumeFilter(this IServiceCollection services, TimeSpan? staleMessageThreshold = null)
    {
        services.AddSingleton(typeof(TenantConsumeFilter<>));
        return services;
    }
}
