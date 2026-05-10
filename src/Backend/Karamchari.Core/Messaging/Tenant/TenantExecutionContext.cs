using System.Text.Json;
using Karamchari.Core.Multitenancy;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Messaging.Tenant;

/// <summary>
/// Represents the tenant execution envelope that is serialized and included in message headers
/// to propagate tenant context across message processing pipeline.
/// </summary>
/// <param name="TenantId">The validated tenant identifier.</param>
/// <param name="CorrelationId">The correlation identifier for request tracking.</param>
/// <param name="TraceId">The trace identifier for distributed tracing.</param>
/// <param name="SpanId">The span identifier for distributed tracing.</param>
/// <param name="UserIdentity">The user identity who initiated the operation.</param>
/// <param name="ExecutionSource">The source of the execution (e.g., API, BackgroundJob, etc.).</param>
/// <param name="OriginalMessageId">The original message identifier if this is a retry or replay.</param>
/// <param name="RetryAttempt">The current retry attempt count.</param>
/// <param name="Timestamp">When this envelope was created.</param>
public sealed record TenantExecutionEnvelope(
    string TenantId,
    string CorrelationId,
    string? TraceId,
    string? SpanId,
    string? UserIdentity,
    string? ExecutionSource,
    string? OriginalMessageId,
    int RetryAttempt,
    DateTime Timestamp)
{
    /// <summary>
    /// Serializes this envelope to a JSON string for header transport.
    /// </summary>
    public string ToJson() => JsonSerializer.Serialize(this);

    /// <summary>
    /// Deserializes an envelope from a JSON string.
    /// </summary>
    public static TenantExecutionEnvelope? FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<TenantExecutionEnvelope>(json);
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Represents the runtime tenant execution context that is recreated for each message consumer scope.
/// This differs from TenantContext in that it is reconstructed from message headers rather than HTTP context.
/// </summary>
public sealed class TenantExecutionContext
{
    private static readonly object _lock = new();
    private static string? _currentTenantId;
    private static string? _correlationId;
    private static string? _traceId;
    private static string? _spanId;
    private static string? _userIdentity;
    private static string? _executionSource;
    private static string? _originalMessageId;
    private static int _retryAttempt;
    private static DateTime _timestamp;
    private static readonly AsyncLocal<TenantExecutionContext?> _asyncLocalContext = new();

    /// <summary>
    /// Gets the current tenant execution context from the async local storage.
    /// </summary>
    public static TenantExecutionContext? Current => _asyncLocalContext.Value;

    /// <summary>
    /// Gets the current tenant identifier from the async local storage.
    /// </summary>
    public static string? CurrentTenantId => _asyncLocalContext.Value?.TenantId;

    /// <summary>
    /// Initializes a new tenant execution context from a tenant execution envelope.
    /// </summary>
    public TenantExecutionContext(TenantExecutionEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        TenantId = envelope.TenantId;
        CorrelationId = envelope.CorrelationId;
        TraceId = envelope.TraceId;
        SpanId = envelope.SpanId;
        UserIdentity = envelope.UserIdentity;
        ExecutionSource = envelope.ExecutionSource;
        OriginalMessageId = envelope.OriginalMessageId;
        RetryAttempt = envelope.RetryAttempt;
        Timestamp = envelope.Timestamp;
    }

    /// <summary>
    /// Creates a TenantExecutionContext from headers or throws if tenant context is missing.
    /// </summary>
    public static TenantExecutionContext? CreateFromHeaders(IPublishEndpoint? endpoint, ConsumeContext? consumeContext)
    {
        if (consumeContext == null) return null;

        consumeContext.Headers.TryGetHeader(TenantMessageHeaderKeys.TenantId, out var tenantIdObj);
        consumeContext.Headers.TryGetHeader(TenantMessageHeaderKeys.CorrelationId, out var correlationIdObj);
        consumeContext.Headers.TryGetHeader(TenantMessageHeaderKeys.TraceId, out var traceIdObj);
        consumeContext.Headers.TryGetHeader(TenantMessageHeaderKeys.SpanId, out var spanIdObj);
        consumeContext.Headers.TryGetHeader(TenantMessageHeaderKeys.UserIdentity, out var userIdentityObj);
        consumeContext.Headers.TryGetHeader(TenantMessageHeaderKeys.ExecutionSource, out var executionSourceObj);
        consumeContext.Headers.TryGetHeader(TenantMessageHeaderKeys.OriginalMessageId, out var originalMessageIdObj);
        consumeContext.Headers.TryGetHeader(TenantMessageHeaderKeys.RetryAttempt, out var retryAttemptObj);
        consumeContext.Headers.TryGetHeader(TenantMessageHeaderKeys.Timestamp, out var timestampObj);
        consumeContext.Headers.TryGetHeader(TenantMessageHeaderKeys.ExecutionEnvelope, out var envelopeObj);

        string tenantId = tenantIdObj as string ?? string.Empty;
        if (string.IsNullOrWhiteSpace(tenantId)) return null;

        string correlationId = correlationIdObj as string ?? consumeContext.ConversationId?.ToString() ?? Guid.NewGuid().ToString();
        string? traceId = traceIdObj as string;
        string? spanId = spanIdObj as string;
        string? userIdentity = userIdentityObj as string;
        string? executionSource = executionSourceObj as string;
        string? originalMessageId = originalMessageIdObj as string;
        int retryAttempt = 0;

        if (retryAttemptObj is int ra) retryAttempt = ra;
        else if (retryAttemptObj is string ras && int.TryParse(ras, out var parsed)) retryAttempt = parsed;

        DateTime timestamp = DateTime.UtcNow;
        if (timestampObj is DateTime ts) timestamp = ts;
        else if (timestampObj is string tss && DateTime.TryParse(tss, out var parsedTs)) timestamp = parsedTs;

        if (envelopeObj is string envelopeJson)
        {
            var envelope = TenantExecutionEnvelope.FromJson(envelopeJson);
            if (envelope != null)
            {
                return new TenantExecutionContext(envelope);
            }
        }

        return new TenantExecutionContext(new TenantExecutionEnvelope(
            tenantId, correlationId, traceId, spanId, userIdentity,
            executionSource, originalMessageId, retryAttempt, timestamp));
    }

    /// <summary>
    /// Creates a TenantExecutionContext from only a tenant ID (for simpler scenarios).
    /// </summary>
    public static TenantExecutionContext? CreateFromTenantId(string tenantId, ConsumeContext? consumeContext = null)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) return null;

        var envelope = new TenantExecutionEnvelope(
            tenantId,
            consumeContext?.ConversationId?.ToString() ?? Guid.NewGuid().ToString(),
            null, null, null, null, null, 0, DateTime.UtcNow);

        return new TenantExecutionContext(envelope);
    }

    /// <summary>
    /// Sets this context as the current execution context for the async local storage.
    /// </summary>
    public void SetAsCurrent()
    {
        lock (_lock)
        {
            _currentTenantId = TenantId;
            _correlationId = CorrelationId;
            _traceId = TraceId;
            _spanId = SpanId;
            _userIdentity = UserIdentity;
            _executionSource = ExecutionSource;
            _originalMessageId = OriginalMessageId;
            _retryAttempt = RetryAttempt;
            _timestamp = Timestamp;
            _asyncLocalContext.Value = this;
        }
    }

    /// <summary>
    /// Clears the current execution context.
    /// </summary>
    public static void ClearCurrent()
    {
        lock (_lock)
        {
            _asyncLocalContext.Value = null;
        }
    }

    /// <summary>
    /// Converts this context back to an envelope for serialization.
    /// </summary>
    public TenantExecutionEnvelope ToEnvelope() => new(
        TenantId, CorrelationId, TraceId, SpanId, UserIdentity,
        ExecutionSource, OriginalMessageId, RetryAttempt, Timestamp);

    /// <summary>The validated tenant identifier.</summary>
    public string TenantId { get; }

    /// <summary>The correlation identifier for request tracking.</summary>
    public string CorrelationId { get; }

    /// <summary>The trace identifier for distributed tracing.</summary>
    public string? TraceId { get; }

    /// <summary>The span identifier for distributed tracing spans.</summary>
    public string? SpanId { get; }

    /// <summary>The user identity who initiated the operation.</summary>
    public string? UserIdentity { get; }

    /// <summary>The source of the execution (e.g., API, BackgroundJob, etc.).</summary>
    public string? ExecutionSource { get; }

    /// <summary>The original message identifier if this is a retry or replay.</summary>
    public string? OriginalMessageId { get; }

    /// <summary>The current retry attempt count.</summary>
    public int RetryAttempt { get; }

    /// <summary>When this envelope was created.</summary>
    public DateTime Timestamp { get; }
}
