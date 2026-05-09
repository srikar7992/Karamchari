using System.Text.Json.Serialization;

namespace Karamchari.Core.Messaging;

/// <summary>
/// Mandatory enterprise envelope for all domain and integration events.
/// Enforces distributed system observability, tenant isolation, and contract versioning.
/// </summary>
public sealed class EnterpriseEventEnvelope<TPayload> where TPayload : class
{
    [JsonPropertyName("eventId")]
    public string EventId { get; init; } = string.Empty;

    [JsonPropertyName("eventType")]
    public string EventType { get; init; } = string.Empty;

    [JsonPropertyName("eventVersion")]
    public string EventVersion { get; init; } = "1.0";

    [JsonPropertyName("occurredAtUtc")]
    public DateTimeOffset OccurredAtUtc { get; init; }

    [JsonPropertyName("tenantId")]
    public string TenantId { get; init; } = string.Empty;

    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; init; }

    [JsonPropertyName("causationId")]
    public string? CausationId { get; init; }

    [JsonPropertyName("producer")]
    public string Producer { get; init; } = string.Empty;

    [JsonPropertyName("payload")]
    public TPayload Payload { get; init; } = default!;

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
