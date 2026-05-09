using System.Text.Json.Serialization;

namespace Karamchari.Core.Messaging;

/// <summary>
/// Mandatory enterprise envelope for all domain and integration events.
/// Enforces distributed system observability, tenant isolation, and contract versioning.
/// </summary>
public sealed class EnterpriseEventEnvelope<TPayload> where TPayload : class
{
    /// <summary>Unique identifier for the event.</summary>
    [JsonPropertyName("eventId")]
    public string EventId { get; init; } = string.Empty;

    /// <summary>Type of the event.</summary>
    [JsonPropertyName("eventType")]
    public string EventType { get; init; } = string.Empty;

    /// <summary>Version of the event schema.</summary>
    [JsonPropertyName("eventVersion")]
    public string EventVersion { get; init; } = "1.0";

    /// <summary>UTC timestamp when the event occurred.</summary>
    [JsonPropertyName("occurredAtUtc")]
    public DateTimeOffset OccurredAtUtc { get; init; }

    /// <summary>Tenant identifier.</summary>
    [JsonPropertyName("tenantId")]
    public string TenantId { get; init; } = string.Empty;

    /// <summary>Correlation identifier for distributed tracing.</summary>
    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; init; }

    /// <summary>Causation identifier tracking what triggered this event.</summary>
    [JsonPropertyName("causationId")]
    public string? CausationId { get; init; }

    /// <summary>Module or service that produced the event.</summary>
    [JsonPropertyName("producer")]
    public string Producer { get; init; } = string.Empty;

    /// <summary>The actual event payload.</summary>
    [JsonPropertyName("payload")]
    public TPayload Payload { get; init; } = default!;

    /// <summary>Wraps a payload in the enterprise event envelope.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "Factory method is appropriate here.")]
    public static EnterpriseEventEnvelope<T> Wrap<T>(
        T payload,
        string tenantId,
        string eventType,
        string version = "1.0",
        string? correlationId = null,
        string? causationId = null,
        string producer = "karamchari.core") where T : class
    {
        return new EnterpriseEventEnvelope<T>
        {
            EventId = Guid.NewGuid().ToString("N"),
            EventType = eventType,
            EventVersion = version,
            OccurredAtUtc = DateTimeOffset.UtcNow,
            TenantId = tenantId,
            CorrelationId = correlationId,
            CausationId = causationId,
            Producer = producer,
            Payload = payload
        };
    }
}
