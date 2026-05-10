using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Karamchari.Core.Multitenancy.Execution;

/// <summary>
/// Immutable snapshot containing all execution metadata for a tenant-scoped operation.
/// Acts as the single source of truth for tracing, auditing, and replay detection
/// across HTTP requests, message consumers, background jobs, and sagas.
/// </summary>
public sealed partial class TenantExecutionEnvelope
{
    [GeneratedRegex(TenantConstants.TenantIdPattern, RegexOptions.CultureInvariant | RegexOptions.Singleline)]
    private static partial Regex TenantIdRegex();

    /// <summary>
    /// The validated tenant identifier for this execution (e.g., <c>acme</c>).
    /// </summary>
    public string TenantId { get; init; }

    /// <summary>
    /// A correlation ID that chains related operations across services.
    /// </summary>
    public string CorrelationId { get; init; }

    /// <summary>
    /// A unique identifier for this specific execution attempt.
    /// </summary>
    public string RequestId { get; init; }

    /// <summary>
    /// Indicates where this execution originated (HTTP, message, job, etc.).
    /// </summary>
    public ExecutionSource ExecutionSource { get; init; }

    /// <summary>
    /// The current retry attempt number (0 for first attempt).
    /// </summary>
    public int RetryAttempt { get; init; }

    /// <summary>
    /// The number of times this execution has been replayed.
    /// </summary>
    public int ReplayCount { get; init; }

    /// <summary>
    /// The user identity associated with this execution, if available.
    /// </summary>
    public string? UserIdentity { get; init; }

    /// <summary>
    /// The distributed tracing trace ID, if available.
    /// </summary>
    public string? TraceId { get; init; }

    /// <summary>
    /// The distributed tracing span ID, if available.
    /// </summary>
    public string? SpanId { get; init; }

    /// <summary>
    /// The UTC timestamp when this envelope was created.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Additional metadata for replay detection and diagnostics.
    /// </summary>
    public Dictionary<string, string> ReplayMetadata { get; init; } = new();

    /// <summary>
    /// The message ID, if this execution originated from a message.
    /// </summary>
    public string? MessageId { get; init; }

    /// <summary>
    /// SHA256 hash of the message content for replay detection.
    /// </summary>
    public string? ContentHash { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantExecutionEnvelope"/> class.
    /// </summary>
    /// <param name="tenantId">The validated tenant identifier.</param>
    /// <param name="correlationId">The correlation ID for tracing.</param>
    /// <param name="requestId">The unique request ID for this execution.</param>
    /// <param name="executionSource">The source of this execution.</param>
    /// <exception cref="ArgumentException">Thrown when tenantId is null, whitespace, or invalid.</exception>
    public TenantExecutionEnvelope(
        string tenantId,
        string correlationId,
        string requestId,
        ExecutionSource executionSource)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestId);

        if (!TenantIdRegex().IsMatch(tenantId))
        {
            throw new ArgumentException(
                string.Create(CultureInfo.InvariantCulture,
                    $"Tenant id '{tenantId}' is invalid. Must match {TenantConstants.TenantIdPattern}."),
                nameof(tenantId));
        }

        TenantId = tenantId;
        CorrelationId = correlationId;
        RequestId = requestId;
        ExecutionSource = executionSource;
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a copy of this envelope with updated properties.
    /// </summary>
    public TenantExecutionEnvelope With(
        string? TenantId = null,
        string? CorrelationId = null,
        string? RequestId = null,
        ExecutionSource? ExecutionSource = null,
        int? RetryAttempt = null,
        int? ReplayCount = null,
        string? UserIdentity = null,
        string? TraceId = null,
        string? SpanId = null,
        DateTime? Timestamp = null,
        Dictionary<string, string>? ReplayMetadata = null,
        string? MessageId = null,
        string? ContentHash = null)
    {
        return new TenantExecutionEnvelope(
            TenantId ?? this.TenantId,
            CorrelationId ?? this.CorrelationId,
            RequestId ?? this.RequestId,
            ExecutionSource ?? this.ExecutionSource)
        {
            RetryAttempt = RetryAttempt ?? this.RetryAttempt,
            ReplayCount = ReplayCount ?? this.ReplayCount,
            UserIdentity = UserIdentity ?? this.UserIdentity,
            TraceId = TraceId ?? this.TraceId,
            SpanId = SpanId ?? this.SpanId,
            Timestamp = Timestamp ?? this.Timestamp,
            ReplayMetadata = ReplayMetadata ?? this.ReplayMetadata,
            MessageId = MessageId ?? this.MessageId,
            ContentHash = ContentHash ?? this.ContentHash
        };
    }

    /// <summary>
    /// Serializes this envelope to a JSON string for transport.
    /// </summary>
    /// <returns>A JSON representation of the envelope.</returns>
    public string ToJson() => JsonSerializer.Serialize(this);

    /// <summary>
    /// Deserializes a <see cref="TenantExecutionEnvelope"/> from JSON.
    /// </summary>
    /// <param name="json">The JSON string.</param>
    /// <returns>The deserialized envelope, or null if invalid.</returns>
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

    /// <summary>
    /// Computes the SHA256 hash of the provided content.
    /// </summary>
    /// <param name="content">The content to hash.</param>
    /// <returns>The hex-encoded SHA256 hash.</returns>
    public static string ComputeContentHash(byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);
        var hash = SHA256.HashData(content);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
