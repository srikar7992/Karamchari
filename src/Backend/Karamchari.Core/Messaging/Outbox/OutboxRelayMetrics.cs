using System.Diagnostics.Metrics;

namespace Karamchari.Core.Messaging.Outbox;

/// <summary>
/// System.Diagnostics.Metrics counters, histograms, and observable gauges
/// for the outbox relay. Register as a singleton and dispose with the host.
/// Export via OpenTelemetry using the meter name <c>Karamchari.OutboxRelay</c>.
/// </summary>
public sealed class OutboxRelayMetrics : IDisposable
{
    private readonly Meter _meter;

    // Observable gauge backing fields â€” updated by the relay on each gauge cycle.
    // Long writes are atomic on 64-bit platforms; no lock needed for single-writer/multi-reader.
    private long _queueDepth;
    private long _oldestPendingAgeSeconds;
    private long _retryBacklog;
    private long _dlqSize;

    // â”€â”€ counters â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>Incremented once per successfully published message.</summary>
    public Counter<long> MessagesProcessed { get; }

    /// <summary>Incremented once per publish attempt that throws a transient exception.</summary>
    public Counter<long> MessagesFailed { get; }

    /// <summary>Incremented when a message crosses MaxRetries and is dead-lettered.</summary>
    public Counter<long> MessagesDeadLettered { get; }

    /// <summary>Incremented when a message is dead-lettered immediately due to a structural (poison) defect.</summary>
    public Counter<long> PoisonMessages { get; }

    /// <summary>Incremented once per relay polling cycle.</summary>
    public Counter<long> BatchCycles { get; }

    /// <summary>Incremented each time stale locks from a crashed relay instance are released.</summary>
    public Counter<long> StaleLockRecoveries { get; }

    // â”€â”€ histograms â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>Wall-clock time per polling cycle in milliseconds.</summary>
    public Histogram<double> BatchDurationMs { get; }

    /// <summary>Round-trip time from publish call to ASB acknowledgment per message, in milliseconds.</summary>
    public Histogram<double> PublishLatencyMs { get; }

    // â”€â”€ observable gauges (polled by OTEL SDK) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>
    /// Initializes a new instance of <see cref="OutboxRelayMetrics"/> and creates all instruments.
    /// Observable gauges are wired to internal backing fields updated by
    /// <see cref="SetQueueDepth"/>, <see cref="SetOldestPendingAgeSeconds"/>,
    /// <see cref="SetRetryBacklog"/>, and <see cref="SetDlqSize"/>.
    /// </summary>
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

        PoisonMessages = _meter.CreateCounter<long>(
            "karamchari_outbox_relay_poison_messages_total",
            description: "Total messages dead-lettered immediately due to structural defects (unresolvable type, missing body, etc.).");

        BatchCycles = _meter.CreateCounter<long>(
            "karamchari_outbox_relay_batch_cycles_total",
            description: "Total polling cycles executed by the relay.");

        StaleLockRecoveries = _meter.CreateCounter<long>(
            "karamchari_outbox_relay_stale_lock_recoveries_total",
            description: "Total OutboxState rows released due to stale lock detection on startup.");

        BatchDurationMs = _meter.CreateHistogram<double>(
            "karamchari_outbox_relay_batch_duration_ms",
            unit: "ms",
            description: "Wall-clock time per relay polling cycle.");

        PublishLatencyMs = _meter.CreateHistogram<double>(
            "karamchari_outbox_relay_publish_latency_ms",
            unit: "ms",
            description: "Round-trip time per message from publish call to ASB acknowledgment.");

        _meter.CreateObservableGauge(
            "karamchari_outbox_queue_depth",
            () => _queueDepth,
            description: "Current count of undelivered OutboxState rows.");

        _meter.CreateObservableGauge(
            "karamchari_outbox_oldest_pending_age_seconds",
            () => _oldestPendingAgeSeconds,
            unit: "s",
            description: "Age of the oldest undelivered OutboxState row, in seconds.");

        _meter.CreateObservableGauge(
            "karamchari_outbox_retry_backlog",
            () => _retryBacklog,
            description: "Count of OutboxProcessingState rows with RetryCount > 0 (messages that have failed at least once).");

        _meter.CreateObservableGauge(
            "karamchari_outbox_dlq_size",
            () => _dlqSize,
            description: "Current row count of dbo.OutboxDeadLetter.");
    }

    /// <summary>Updates the queue depth gauge (undelivered OutboxState rows).</summary>
    public void SetQueueDepth(long value) => Interlocked.Exchange(ref _queueDepth, value);

    /// <summary>Updates the oldest-pending-age gauge (seconds since oldest undelivered OutboxState row was created).</summary>
    public void SetOldestPendingAgeSeconds(long value) => Interlocked.Exchange(ref _oldestPendingAgeSeconds, value);

    /// <summary>Updates the retry backlog gauge (messages with at least one failed attempt).</summary>
    public void SetRetryBacklog(long value) => Interlocked.Exchange(ref _retryBacklog, value);

    /// <summary>Updates the DLQ size gauge.</summary>
    public void SetDlqSize(long value) => Interlocked.Exchange(ref _dlqSize, value);

    /// <inheritdoc />
    public void Dispose() => _meter.Dispose();
}
