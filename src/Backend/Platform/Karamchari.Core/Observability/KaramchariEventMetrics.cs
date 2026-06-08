using System.Diagnostics.Metrics;

namespace Karamchari.Core.Observability;

/// <summary>
/// Per-event operational metrics for the Karamchari integration event bus.
/// Answers the telemetry questions the static event catalog cannot:
///   - How many EmployeeTerminated events fired today?
///   - Average latency per consumer?
///   - Dead-letter rate per event type?
///   - Webhook delivery success rate?
///
/// Meter name: <c>Karamchari.Events</c> — automatically collected by the
/// <c>AddMeter("Karamchari.*")</c> registration in InfrastructureExtensions.
/// Register as a singleton and dispose with the host.
/// </summary>
public sealed class KaramchariEventMetrics : IDisposable
{
    private readonly Meter _meter;

    // ── Publish ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Incremented once per successfully published integration event.
    /// Tags: event.name, event.publisher, tenant.id
    /// </summary>
    public Counter<long> EventsPublished { get; }

    /// <summary>
    /// Incremented when a publish fault occurs before the message leaves the bus.
    /// Tags: event.name, tenant.id, error.type
    /// </summary>
    public Counter<long> PublishFaults { get; }

    // ── Consume ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Histogram of consumer handler duration per event type.
    /// Tags: event.name, consumer.name, tenant.id
    /// </summary>
    public Histogram<double> ConsumerLatencyMs { get; }

    /// <summary>
    /// Incremented when a consumer handler faults (exception thrown).
    /// Tags: event.name, consumer.name, tenant.id, error.type
    /// </summary>
    public Counter<long> ConsumeFaults { get; }

    // ── Dead-letter ───────────────────────────────────────────────────────────

    /// <summary>
    /// Incremented when a message is dead-lettered after exhausting retries.
    /// Tags: event.name, tenant.id
    /// </summary>
    public Counter<long> DeadLetterCount { get; }

    // ── Webhook delivery ──────────────────────────────────────────────────────

    /// <summary>
    /// Incremented once per outbound webhook delivery attempt.
    /// Tags: event.type, tenant.id, status (success|fault)
    /// </summary>
    public Counter<long> WebhookDeliveries { get; }

    /// <summary>
    /// Histogram of outbound webhook HTTP round-trip time.
    /// Tags: event.type, tenant.id
    /// </summary>
    public Histogram<double> WebhookDeliveryLatencyMs { get; }

    public KaramchariEventMetrics()
    {
        _meter = new Meter("Karamchari.Events", "1.0.0");

        EventsPublished = _meter.CreateCounter<long>(
            "karamchari_events_published_total",
            description: "Total integration events successfully published to the bus.");

        PublishFaults = _meter.CreateCounter<long>(
            "karamchari_events_publish_faults_total",
            description: "Total publish faults before the message left the bus.");

        ConsumerLatencyMs = _meter.CreateHistogram<double>(
            "karamchari_events_consumer_latency_ms",
            unit: "ms",
            description: "Consumer handler duration per event type and consumer name.");

        ConsumeFaults = _meter.CreateCounter<long>(
            "karamchari_events_consume_faults_total",
            description: "Total consumer handler faults.");

        DeadLetterCount = _meter.CreateCounter<long>(
            "karamchari_events_dead_letter_total",
            description: "Total messages dead-lettered after exhausting retries.");

        WebhookDeliveries = _meter.CreateCounter<long>(
            "karamchari_webhook_deliveries_total",
            description: "Total outbound webhook delivery attempts.");

        WebhookDeliveryLatencyMs = _meter.CreateHistogram<double>(
            "karamchari_webhook_delivery_latency_ms",
            unit: "ms",
            description: "Outbound webhook HTTP round-trip time.");
    }

    // ── Recording helpers ─────────────────────────────────────────────────────

    /// <summary>Records a successful event publish.</summary>
    public void RecordPublish(string eventName, string publisher, string tenantId)
    {
        EventsPublished.Add(1,
            new KeyValuePair<string, object?>("event.name", eventName),
            new KeyValuePair<string, object?>("event.publisher", publisher),
            new KeyValuePair<string, object?>("tenant.id", tenantId));
    }

    /// <summary>Records a publish fault.</summary>
    public void RecordPublishFault(string eventName, string tenantId, string errorType)
    {
        PublishFaults.Add(1,
            new KeyValuePair<string, object?>("event.name", eventName),
            new KeyValuePair<string, object?>("tenant.id", tenantId),
            new KeyValuePair<string, object?>("error.type", errorType));
    }

    /// <summary>Records consumer latency after a successful consume.</summary>
    public void RecordConsumerLatency(string eventName, string consumerName, string tenantId, double latencyMs)
    {
        ConsumerLatencyMs.Record(latencyMs,
            new KeyValuePair<string, object?>("event.name", eventName),
            new KeyValuePair<string, object?>("consumer.name", consumerName),
            new KeyValuePair<string, object?>("tenant.id", tenantId));
    }

    /// <summary>Records a consumer fault.</summary>
    public void RecordConsumeFault(string eventName, string consumerName, string tenantId, string errorType)
    {
        ConsumeFaults.Add(1,
            new KeyValuePair<string, object?>("event.name", eventName),
            new KeyValuePair<string, object?>("consumer.name", consumerName),
            new KeyValuePair<string, object?>("tenant.id", tenantId),
            new KeyValuePair<string, object?>("error.type", errorType));
    }

    /// <summary>Records a dead-letter event.</summary>
    public void RecordDeadLetter(string eventName, string tenantId)
    {
        DeadLetterCount.Add(1,
            new KeyValuePair<string, object?>("event.name", eventName),
            new KeyValuePair<string, object?>("tenant.id", tenantId));
    }

    /// <summary>Records a webhook delivery attempt.</summary>
    public void RecordWebhookDelivery(string eventType, string tenantId, bool success, double latencyMs)
    {
        var status = success ? "success" : "fault";

        WebhookDeliveries.Add(1,
            new KeyValuePair<string, object?>("event.type", eventType),
            new KeyValuePair<string, object?>("tenant.id", tenantId),
            new KeyValuePair<string, object?>("status", status));

        WebhookDeliveryLatencyMs.Record(latencyMs,
            new KeyValuePair<string, object?>("event.type", eventType),
            new KeyValuePair<string, object?>("tenant.id", tenantId));
    }

    /// <inheritdoc />
    public void Dispose() => _meter.Dispose();
}
