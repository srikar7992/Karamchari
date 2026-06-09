using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Threading;

namespace Karamchari.Core.Observability;

/// <summary>
/// Projection pipeline and intelligence prediction metrics for the Karamchari platform.
/// Meter name: <c>Karamchari.Projections</c> — collected automatically by the
/// <c>AddMeter("Karamchari.*")</c> wildcard registered in InfrastructureExtensions.
/// Register as a singleton and dispose with the host.
/// </summary>
public sealed class KaramchariProjectionMetrics : IDisposable
{
    private readonly Meter _meter;

    // Backing state for ObservableGauges — updated by ProjectionLagPollerService
    private readonly ConcurrentDictionary<string, long> _lagEvents = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, double> _lagMs = new(StringComparer.Ordinal);
    private long _checkpointCount;

    // ── Counters ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Incremented once per event successfully processed by a projection handler.
    /// Tags: projection.name, tenant.id
    /// </summary>
    public Counter<long> ProjectionEventsProcessed { get; }

    /// <summary>
    /// Incremented when a projection handler throws an unhandled exception.
    /// Tags: projection.name, tenant.id, error.type
    /// </summary>
    public Counter<long> ProjectionFailures { get; }

    /// <summary>
    /// Incremented when a projection replay operation is triggered.
    /// Tags: projection.name
    /// </summary>
    public Counter<long> ProjectionReplays { get; }

    /// <summary>
    /// Incremented each time a prediction score is recalculated.
    /// Tags: prediction.name (FlightRisk | PromotionReadiness | SuccessorReadiness | InternalMobility), tenant.id
    /// </summary>
    public Counter<long> PredictionRecalculations { get; }

    // ── Histograms ────────────────────────────────────────────────────────────

    /// <summary>
    /// Duration of a full projection replay operation, in milliseconds.
    /// Tags: projection.name
    /// </summary>
    public Histogram<double> ProjectionReplayDurationMs { get; }

    // ── ObservableGauges (updated by ProjectionLagPollerService) ──────────────

    // Registered via CreateObservableGauge callbacks in the constructor.

    /// <summary>
    /// Initializes projection metrics and registers Observable gauges backed by state
    /// updated by <see cref="ProjectionLagPollerService"/>.
    /// </summary>
    public KaramchariProjectionMetrics()
    {
        _meter = new Meter("Karamchari.Projections", "1.0.0");

        ProjectionEventsProcessed = _meter.CreateCounter<long>(
            "karamchari_projection_events_processed_total",
            description: "Total events processed successfully by projection handlers.");

        ProjectionFailures = _meter.CreateCounter<long>(
            "karamchari_projection_failures_total",
            description: "Total projection handler failures.");

        ProjectionReplays = _meter.CreateCounter<long>(
            "karamchari_projection_replays_total",
            description: "Total projection replay operations triggered.");

        PredictionRecalculations = _meter.CreateCounter<long>(
            "karamchari_prediction_recalculations_total",
            description: "Total prediction recalculations per prediction type.");

        ProjectionReplayDurationMs = _meter.CreateHistogram<double>(
            "karamchari_projection_replay_duration_ms",
            unit: "ms",
            description: "Duration of a full projection replay operation.");

        _meter.CreateObservableGauge(
            "karamchari_projection_lag_events",
            observeValues: ObserveLagEvents,
            description: "Event position lag (MaxCheckpointPos - ProjectionCheckpointPos) per projection. Threshold: >10k Warning, >100k Critical.");

        _meter.CreateObservableGauge(
            "karamchari_projection_lag_ms",
            observeValues: ObserveLagMs,
            unit: "ms",
            description: "Time-based lag (UtcNow - LastProcessedUtc) per projection, in milliseconds.");

        _meter.CreateObservableGauge<long>(
            "karamchari_projection_checkpoint_count",
            observeValue: () => Interlocked.Read(ref _checkpointCount),
            description: "Number of registered projection checkpoints. A drop below expected count indicates an unregistered projection.");
    }

    // ── State update methods (called by ProjectionLagPollerService) ───────────

    /// <summary>Updates lag gauges for a single projection.</summary>
    public void UpdateLag(string projectionName, long lagEvents, double lagMs)
    {
        _lagEvents[projectionName] = lagEvents;
        _lagMs[projectionName] = lagMs;
    }

    /// <summary>Updates the total number of registered projection checkpoints.</summary>
    public void UpdateCheckpointCount(long count) =>
        Interlocked.Exchange(ref _checkpointCount, count);

    // ── Recording helpers ─────────────────────────────────────────────────────

    /// <summary>Records a successfully processed projection event.</summary>
    public void RecordEventProcessed(string projectionName, string tenantId)
    {
        ProjectionEventsProcessed.Add(1,
            new KeyValuePair<string, object?>("projection.name", projectionName),
            new KeyValuePair<string, object?>("tenant.id", tenantId));
    }

    /// <summary>Records a projection handler failure.</summary>
    public void RecordFailure(string projectionName, string tenantId, string errorType)
    {
        ProjectionFailures.Add(1,
            new KeyValuePair<string, object?>("projection.name", projectionName),
            new KeyValuePair<string, object?>("tenant.id", tenantId),
            new KeyValuePair<string, object?>("error.type", errorType));
    }

    /// <summary>Records a prediction recalculation.</summary>
    public void RecordPredictionRecalculation(string predictionName, string tenantId)
    {
        PredictionRecalculations.Add(1,
            new KeyValuePair<string, object?>("prediction.name", predictionName),
            new KeyValuePair<string, object?>("tenant.id", tenantId));
    }

    // ── Observable callbacks ──────────────────────────────────────────────────

    private IEnumerable<Measurement<long>> ObserveLagEvents()
    {
        foreach (var (name, value) in _lagEvents)
            yield return new Measurement<long>(value, new KeyValuePair<string, object?>("projection.name", name));
    }

    private IEnumerable<Measurement<double>> ObserveLagMs()
    {
        foreach (var (name, value) in _lagMs)
            yield return new Measurement<double>(value, new KeyValuePair<string, object?>("projection.name", name));
    }

    /// <inheritdoc />
    public void Dispose() => _meter.Dispose();
}
