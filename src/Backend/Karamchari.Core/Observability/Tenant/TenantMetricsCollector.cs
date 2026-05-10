using System.Diagnostics.Metrics;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Core.Observability.Tenant;

/// <summary>
/// Metrics collector for tenant isolation observability.
/// Tracks tenant isolation violations, RLS query execution time, connection pool
/// contamination events, replay detection, and retry storm events.
/// </summary>
public sealed class TenantMetricsCollector : IDisposable
{
    private const string MeterName = "Karamchari.TenantIsolation";

    private readonly Meter _meter;
    private readonly Counter<long> _isolationViolationCounter;
    private readonly Histogram<double> _rlsQueryDurationHistogram;
    private readonly Counter<long> _poolContaminationCounter;
    private readonly Counter<long> _replayDetectionCounter;
    private readonly Counter<long> _retryStormCounter;
    private readonly Counter<long> _tenantContextResolutionCounter;
    private readonly Histogram<double> _tenantOperationDurationHistogram;
    private readonly Histogram<double> _workflowDurationHistogram;
    private readonly Counter<long> _workflowSlaBreachCounter;
    private readonly Counter<long> _workflowTransitionFailureCounter;

    public TenantMetricsCollector()
    {
        _meter = new Meter(MeterName);

        _isolationViolationCounter = _meter.CreateCounter<long>(
            "tenant.isolation.violation.count",
            description: "Number of tenant isolation violations detected");

        _rlsQueryDurationHistogram = _meter.CreateHistogram<double>(
            "tenant.rls.query.duration.ms",
            unit: "ms",
            description: "RLS query execution time in milliseconds");

        _poolContaminationCounter = _meter.CreateCounter<long>(
            "tenant.connection.pool.contamination.count",
            description: "Number of connection pool contamination events");

        _replayDetectionCounter = _meter.CreateCounter<long>(
            "tenant.replay.detection.count",
            description: "Number of replay detection events");

        _retryStormCounter = _meter.CreateCounter<long>(
            "tenant.retry.storm.count",
            description: "Number of retry storm events detected");

        _tenantContextResolutionCounter = _meter.CreateCounter<long>(
            "tenant.context.resolution.count",
            description: "Tenant context resolution attempts");

        _tenantOperationDurationHistogram = _meter.CreateHistogram<double>(
            "tenant.operation.duration.ms",
            unit: "ms",
            description: "Tenant-aware operation duration in milliseconds");

        _workflowDurationHistogram = _meter.CreateHistogram<double>(
            "workflow.duration.sec",
            unit: "s",
            description: "Total duration of a business workflow instance");

        _workflowSlaBreachCounter = _meter.CreateCounter<long>(
            "workflow.sla.breach.count",
            description: "Number of workflow SLA breaches detected");

        _workflowTransitionFailureCounter = _meter.CreateCounter<long>(
            "workflow.transition.failure.count",
            description: "Number of failed workflow state transitions");
    }

    /// <summary>
    /// Records total workflow duration.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="workflowType">The type of workflow.</param>
    /// <param name="durationSec">Duration in seconds.</param>
    public void RecordWorkflowDuration(string tenantId, string workflowType, double durationSec)
    {
        _workflowDurationHistogram.Record(
            durationSec,
            new KeyValuePair<string, object?>("tenant.id", tenantId),
            new KeyValuePair<string, object?>("workflow.type", workflowType));
    }

    /// <summary>
    /// Records a workflow SLA breach.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="workflowType">The type of workflow.</param>
    /// <param name="stepId">The step where the breach occurred.</param>
    public void RecordWorkflowSlaBreach(string tenantId, string workflowType, string stepId)
    {
        _workflowSlaBreachCounter.Add(
            1,
            new KeyValuePair<string, object?>("tenant.id", tenantId),
            new KeyValuePair<string, object?>("workflow.type", workflowType),
            new KeyValuePair<string, object?>("workflow.step", stepId));
    }

    /// <summary>
    /// Records a failed workflow state transition.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="workflowType">The type of workflow.</param>
    /// <param name="errorType">The type of error causing the failure.</param>
    public void RecordWorkflowTransitionFailure(string tenantId, string workflowType, string errorType)
    {
        _workflowTransitionFailureCounter.Add(
            1,
            new KeyValuePair<string, object?>("tenant.id", tenantId),
            new KeyValuePair<string, object?>("workflow.type", workflowType),
            new KeyValuePair<string, object?>("error.type", errorType));
    }

    /// <summary>
    /// Records a tenant isolation violation.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="violationType">Type of isolation violation.</param>
    /// <param name="source">Component where violation occurred.</param>
    public void RecordIsolationViolation(string tenantId, string violationType, string source)
    {
        _isolationViolationCounter.Add(
            1,
            new KeyValuePair<string, object?>("tenant.id", tenantId),
            new KeyValuePair<string, object?>("violation.type", violationType),
            new KeyValuePair<string, object?>("source", source));
    }

    /// <summary>
    /// Records RLS query execution duration.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="durationMs">Query execution time in milliseconds.</param>
    /// <param name="queryType">Type of query (SELECT, INSERT, UPDATE, DELETE).</param>
    public void RecordRlsQueryDuration(string tenantId, double durationMs, string queryType = "SELECT")
    {
        _rlsQueryDurationHistogram.Record(
            durationMs,
            new KeyValuePair<string, object?>("tenant.id", tenantId),
            new KeyValuePair<string, object?>("query.type", queryType));
    }

    /// <summary>
    /// Records a connection pool contamination event.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="contaminatedBy">Tenant that contaminated the pool.</param>
    /// <param name="severity">Severity level of contamination.</param>
    public void RecordPoolContamination(string tenantId, string contaminatedBy, string severity = "Warning")
    {
        _poolContaminationCounter.Add(
            1,
            new KeyValuePair<string, object?>("tenant.id", tenantId),
            new KeyValuePair<string, object?>("contaminated.by", contaminatedBy),
            new KeyValuePair<string, object?>("severity", severity));
    }

    /// <summary>
    /// Records a replay detection event.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="operationType">Type of operation being replayed.</param>
    /// <param name="replayCount">Number of times detected.</param>
    public void RecordReplayDetection(string tenantId, string operationType, int replayCount = 1)
    {
        _replayDetectionCounter.Add(
            replayCount,
            new KeyValuePair<string, object?>("tenant.id", tenantId),
            new KeyValuePair<string, object?>("operation.type", operationType));
    }

    /// <summary>
    /// Records a retry storm event.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="operationType">Type of operation being retried.</param>
    /// <param name="attemptCount">Number of retry attempts.</param>
    public void RecordRetryStorm(string tenantId, string operationType, int attemptCount)
    {
        _retryStormCounter.Add(
            1,
            new KeyValuePair<string, object?>("tenant.id", tenantId),
            new KeyValuePair<string, object?>("operation.type", operationType),
            new KeyValuePair<string, object?>("attempt.count", attemptCount));
    }

    /// <summary>
    /// Records tenant context resolution (success or failure).
    /// </summary>
    /// <param name="tenantId">The tenant identifier (may be null for failures).</param>
    /// <param name="success">Whether resolution succeeded.</param>
    /// <param name="source">Source of resolution (HTTP, Message, etc.).</param>
    public void RecordContextResolution(string? tenantId, bool success, string source)
    {
        _tenantContextResolutionCounter.Add(
            1,
            new KeyValuePair<string, object?>("tenant.id", tenantId ?? "unknown"),
            new KeyValuePair<string, object?>("success", success),
            new KeyValuePair<string, object?>("source", source));
    }

    /// <summary>
    /// Records tenant-aware operation duration.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="operationType">Type of operation.</param>
    /// <param name="executionSource">Source of execution.</param>
    /// <param name="durationMs">Duration in milliseconds.</param>
    public void RecordOperationDuration(
        string tenantId,
        string operationType,
        string executionSource,
        double durationMs)
    {
        _tenantOperationDurationHistogram.Record(
            durationMs,
            new KeyValuePair<string, object?>("tenant.id", tenantId),
            new KeyValuePair<string, object?>("operation.type", operationType),
            new KeyValuePair<string, object?>("execution.source", executionSource));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _meter.Dispose();
    }
}

/// <summary>
/// Builder for tenant metrics with common tag combinations.
/// </summary>
public readonly struct TenantMetricTags
{
    public string TenantId { get; }
    public string OperationType { get; }
    public string ExecutionSource { get; }

    public TenantMetricTags(string tenantId, string operationType, string executionSource)
    {
        TenantId = tenantId;
        OperationType = operationType;
        ExecutionSource = executionSource;
    }

    public IEnumerable<KeyValuePair<string, object?>> ToKeyValuePairs()
    {
        yield return new KeyValuePair<string, object?>("tenant.id", TenantId);
        yield return new KeyValuePair<string, object?>("operation.type", OperationType);
        yield return new KeyValuePair<string, object?>("execution.source", ExecutionSource);
    }
}

/// <summary>
/// Extension methods for tenant metrics.
/// </summary>
public static class TenantMetricsExtensions
{
    /// <summary>
    /// Creates a metric tags struct from a tenant context.
    /// </summary>
    public static TenantMetricTags CreateMetricTags(
        this TenantExecutionEnvelope context,
        string operationType,
        string executionSource)
    {
        return new TenantMetricTags(context.TenantId, operationType, executionSource);
    }
}
