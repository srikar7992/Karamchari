using System.Diagnostics.Metrics;

namespace Karamchari.Core.Messaging.Outbox;

/// <summary>
/// System.Diagnostics.Metrics counters and histograms for the outbox relay.
/// Register as a singleton and dispose with the host. Export via OTEL.
/// </summary>
public sealed class OutboxRelayMetrics : IDisposable
{
    private readonly Meter _meter;

    /// <summary>Incremented once per successfully published message.</summary>
    public Counter<long> MessagesProcessed { get; }

    /// <summary>Incremented once per publish attempt that throws.</summary>
    public Counter<long> MessagesFailed { get; }

    /// <summary>Incremented when a message crosses MaxRetries and is dead-lettered.</summary>
    public Counter<long> MessagesDeadLettered { get; }

    /// <summary>Incremented once per relay polling cycle.</summary>
    public Counter<long> BatchCycles { get; }

    /// <summary>Wall-clock time per polling cycle in milliseconds.</summary>
    public Histogram<double> BatchDurationMs { get; }

    /// <summary>Initializes a new instance of <see cref="OutboxRelayMetrics"/> and creates all meters.</summary>
    public OutboxRelayMetrics()
    {
        _meter = new Meter("Karamchari.OutboxRelay", "1.0.0");

        MessagesProcessed = _meter.CreateCounter<long>(
            "karamchari_outbox_relay_messages_processed_total",
            description: "Total messages successfully published to the bus.");

        MessagesFailed = _meter.CreateCounter<long>(
            "karamchari_outbox_relay_messages_failed_total",
            description: "Total publish attempts that failed (retryable).");

        MessagesDeadLettered = _meter.CreateCounter<long>(
            "karamchari_outbox_relay_messages_dead_lettered_total",
            description: "Total messages moved to OutboxDeadLetter after exhausting retries.");

        BatchCycles = _meter.CreateCounter<long>(
            "karamchari_outbox_relay_batch_cycles_total",
            description: "Total polling cycles executed by the relay.");

        BatchDurationMs = _meter.CreateHistogram<double>(
            "karamchari_outbox_relay_batch_duration_ms",
            unit: "ms",
            description: "Wall-clock time per relay polling cycle.");
    }

    /// <inheritdoc />
    public void Dispose() => _meter.Dispose();
}
