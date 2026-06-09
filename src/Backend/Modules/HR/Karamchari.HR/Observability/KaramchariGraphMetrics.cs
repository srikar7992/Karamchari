using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;

namespace Karamchari.HR.Observability;

/// <summary>
/// Workforce graph health and traversal metrics.
/// Meter name: <c>Karamchari.WorkforceGraph</c> — collected automatically by the
/// <c>AddMeter("Karamchari.*")</c> wildcard registered in InfrastructureExtensions.
/// Register as a singleton and dispose with the host.
/// </summary>
public sealed class KaramchariGraphMetrics : IDisposable
{
    private readonly Meter _meter;

    // Backing state for ObservableGauges — updated by GraphStatisticsPollerService
    private readonly ConcurrentDictionary<string, long> _nodeCounts = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, long> _edgeCounts = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, long> _activeEdgeCounts = new(StringComparer.Ordinal);

    // ── Histogram / Counter ───────────────────────────────────────────────────

    /// <summary>
    /// Histogram of graph traversal latency per traversal type and tenant.
    /// Tags: traversal.type (see <c>WorkforceGraphTraversalType</c>), tenant.id
    /// Threshold: >250ms Warning.
    /// </summary>
    public Histogram<double> GraphTraversalLatencyMs { get; }

    /// <summary>
    /// Incremented once per graph traversal operation.
    /// Tags: traversal.type, tenant.id
    /// </summary>
    public Counter<long> GraphTraversalCount { get; }

    // ── ObservableGauges (updated by GraphStatisticsPollerService) ────────────

    // Registered via CreateObservableGauge callbacks in the constructor.

    /// <summary>
    /// Initializes graph metrics and registers Observable gauges backed by state
    /// updated by <see cref="GraphStatisticsPollerService"/>.
    /// </summary>
    public KaramchariGraphMetrics()
    {
        _meter = new Meter("Karamchari.WorkforceGraph", "1.0.0");

        GraphTraversalLatencyMs = _meter.CreateHistogram<double>(
            "karamchari_graph_traversal_latency_ms",
            unit: "ms",
            description: "Workforce graph traversal latency per type. Threshold: >250ms Warning.");

        GraphTraversalCount = _meter.CreateCounter<long>(
            "karamchari_graph_traversal_count_total",
            description: "Total workforce graph traversal operations.");

        _meter.CreateObservableGauge(
            "karamchari_graph_node_count",
            observeValues: ObserveNodeCounts,
            description: "Total workforce graph nodes by node type.");

        _meter.CreateObservableGauge(
            "karamchari_graph_edge_count",
            observeValues: ObserveEdgeCounts,
            description: "Total workforce graph edges by edge type. Threshold: >10M Warning, >25M Critical.");

        _meter.CreateObservableGauge(
            "karamchari_graph_active_edge_count",
            observeValues: ObserveActiveEdgeCounts,
            description: "Active workforce graph edges by edge type.");
    }

    // ── State update methods (called by GraphStatisticsPollerService) ─────────

    /// <summary>Updates node count gauges. Key = node type name, value = count.</summary>
    public void UpdateNodeCounts(IReadOnlyDictionary<string, long> counts)
    {
        foreach (var (k, v) in counts)
            _nodeCounts[k] = v;
    }

    /// <summary>Updates edge count gauges. Keys = edge type name, values = counts.</summary>
    public void UpdateEdgeCounts(
        IReadOnlyDictionary<string, long> totalCounts,
        IReadOnlyDictionary<string, long> activeCounts)
    {
        foreach (var (k, v) in totalCounts)
            _edgeCounts[k] = v;
        foreach (var (k, v) in activeCounts)
            _activeEdgeCounts[k] = v;
    }

    // ── Recording helpers ─────────────────────────────────────────────────────

    /// <summary>Records a graph traversal latency and increments traversal count.</summary>
    public void RecordTraversal(string traversalType, string tenantId, double latencyMs)
    {
        var tags = new[]
        {
            new KeyValuePair<string, object?>("traversal.type", traversalType),
            new KeyValuePair<string, object?>("tenant.id", tenantId),
        };
        GraphTraversalLatencyMs.Record(latencyMs, tags);
        GraphTraversalCount.Add(1, tags);
    }

    // ── Observable callbacks ──────────────────────────────────────────────────

    private IEnumerable<Measurement<long>> ObserveNodeCounts()
    {
        foreach (var (type, count) in _nodeCounts)
            yield return new Measurement<long>(count, new KeyValuePair<string, object?>("node.type", type));
    }

    private IEnumerable<Measurement<long>> ObserveEdgeCounts()
    {
        foreach (var (type, count) in _edgeCounts)
            yield return new Measurement<long>(count, new KeyValuePair<string, object?>("edge.type", type));
    }

    private IEnumerable<Measurement<long>> ObserveActiveEdgeCounts()
    {
        foreach (var (type, count) in _activeEdgeCounts)
            yield return new Measurement<long>(count, new KeyValuePair<string, object?>("edge.type", type));
    }

    /// <inheritdoc />
    public void Dispose() => _meter.Dispose();
}
