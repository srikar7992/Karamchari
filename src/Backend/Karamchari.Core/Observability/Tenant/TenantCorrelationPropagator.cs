using System.Collections.Generic;
using System.Diagnostics;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Core.Observability.Tenant;

/// <summary>
/// Correlation propagator for distributed tenant-aware tracing.
/// Propagates tenant context through message headers, HTTP headers,
/// and service bus messages. Supports W3C traceparent header format.
/// </summary>
public sealed class TenantCorrelationPropagator
{
    public const string TraceParentHeader = "traceparent";
    public const string TraceStateHeader = "tracestate";
    public const string CorrelationIdHeader = "x-correlation-id";
    public const string TenantIdHeader = "x-tenant-id";

    private readonly TenantActivitySource _activitySource;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantCorrelationPropagator"/> class.
    /// </summary>
    /// <param name="activitySource">The tenant activity source.</param>
    public TenantCorrelationPropagator(TenantActivitySource activitySource)
    {
        _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));
    }

    /// <summary>
    /// Extracts tenant context from HTTP headers.
    /// </summary>
    /// <param name="headers">HTTP headers dictionary.</param>
    /// <returns>A tuple containing tenant ID, correlation ID, and trace context.</returns>
    public (string? TenantId, string? CorrelationId, ActivityContext? TraceContext) ExtractFromHttpHeaders(
        IDictionary<string, string?> headers)
    {
        if (headers == null || headers.Count == 0)
        {
            return (null, null, null);
        }

        string? tenantId = null;
        string? correlationId = null;
        ActivityContext? traceContext = null;

        if (headers.TryGetValue(TenantIdHeader, out var tenantHeader) && !string.IsNullOrEmpty(tenantHeader))
        {
            tenantId = tenantHeader;
        }

        if (headers.TryGetValue(CorrelationIdHeader, out var correlationHeader) && !string.IsNullOrEmpty(correlationHeader))
        {
            correlationId = correlationHeader;
        }

        if (headers.TryGetValue(TraceParentHeader, out var traceparent) && !string.IsNullOrEmpty(traceparent))
        {
            traceContext = ExtractTraceContext(traceparent);
        }

        return (tenantId, correlationId, traceContext);
    }

    /// <summary>
    /// Injects tenant context into HTTP headers for outgoing requests.
    /// </summary>
    /// <param name="headers">HTTP headers dictionary to modify.</param>
    /// <param name="tenantContext">The tenant context.</param>
    /// <param name="correlationId">Optional correlation ID.</param>
    /// <param name="activity">Optional current activity for trace propagation.</param>
    public void InjectIntoHttpHeaders(
        IDictionary<string, string?> headers,
        TenantExecutionEnvelope tenantContext,
        string? correlationId = null,
        Activity? activity = null)
    {
        if (headers == null)
        {
            return;
        }

        headers[TenantIdHeader] = tenantContext.TenantId;

        if (!string.IsNullOrEmpty(correlationId))
        {
            headers[CorrelationIdHeader] = correlationId;
        }

        if (activity != null)
        {
            var traceparent = SerializeTraceparent(activity);
            if (!string.IsNullOrEmpty(traceparent))
            {
                headers[TraceParentHeader] = traceparent;
            }

            var traceState = activity.TraceStateString;
            if (!string.IsNullOrEmpty(traceState))
            {
                headers[TraceStateHeader] = traceState;
            }
        }
    }

    /// <summary>
    /// Extracts tenant context from message headers (MassTransit or similar).
    /// </summary>
    /// <param name="headers">Message headers dictionary.</param>
    /// <returns>A tuple containing tenant ID, correlation ID, and trace context.</returns>
    public (string? TenantId, string? CorrelationId, ActivityContext? TraceContext) ExtractFromMessageHeaders(
        IDictionary<string, object?> headers)
    {
        if (headers == null || headers.Count == 0)
        {
            return (null, null, null);
        }

        string? tenantId = null;
        string? correlationId = null;
        ActivityContext? traceContext = null;

        if (headers.TryGetValue("tenant-id", out var tenantValue) && tenantValue is string tenantString)
        {
            tenantId = tenantString;
        }

        if (headers.TryGetValue("correlation-id", out var correlationValue) && correlationValue is string correlationString)
        {
            correlationId = correlationString;
        }

        if (headers.TryGetValue("traceparent", out var traceparentValue) && traceparentValue is string traceparentString)
        {
            traceContext = ExtractTraceContext(traceparentString);
        }

        return (tenantId, correlationId, traceContext);
    }

    /// <summary>
    /// Injects tenant context into message headers for outgoing messages.
    /// </summary>
    /// <param name="headers">Message headers dictionary to modify.</param>
    /// <param name="tenantContext">The tenant context.</param>
    /// <param name="correlationId">Optional correlation ID.</param>
    /// <param name="activity">Optional current activity.</param>
    public void InjectIntoMessageHeaders(
        IDictionary<string, object?> headers,
        TenantExecutionEnvelope tenantContext,
        string? correlationId = null,
        Activity? activity = null)
    {
        if (headers == null)
        {
            return;
        }

        headers["tenant-id"] = tenantContext.TenantId;
        headers["tenant-schema"] = tenantContext.SchemaName;

        if (!string.IsNullOrEmpty(correlationId))
        {
            headers["correlation-id"] = correlationId;
        }

        if (activity != null)
        {
            var traceparent = SerializeTraceparent(activity);
            if (!string.IsNullOrEmpty(traceparent))
            {
                headers["traceparent"] = traceparent;
            }

            var traceState = activity.TraceStateString;
            if (!string.IsNullOrEmpty(traceState))
            {
                headers["tracestate"] = traceState;
            }
        }
    }

    /// <summary>
    /// Propagates trace context for background job execution.
    /// </summary>
    /// <param name="headers">Job headers dictionary.</param>
    /// <param name="tenantContext">The tenant context.</param>
    /// <param name="jobId">The job identifier.</param>
    public void PropagateForBackgroundJob(
        IDictionary<string, object?> headers,
        TenantExecutionEnvelope tenantContext,
        string jobId)
    {
        if (headers == null)
        {
            return;
        }

        headers["tenant-id"] = tenantContext.TenantId;
        headers["tenant-schema"] = tenantContext.SchemaName;
        headers["job-id"] = jobId;
        headers["execution-source"] = "BackgroundJob";
    }

    /// <summary>
    /// Creates a correlation context for a new distributed operation.
    /// </summary>
    /// <param name="tenantContext">The tenant context.</param>
    /// <param name="operationName">The operation name.</param>
    /// <returns>A correlation context with trace and tenant information.</returns>
    public TenantCorrelationContext CreateCorrelationContext(
        TenantExecutionEnvelope tenantContext,
        string operationName)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        var activity = _activitySource.StartActivity(
            operationName,
            tenantContext,
            ExecutionSource.Manual,
            correlationId);

        return new TenantCorrelationContext(
            tenantContext.TenantId,
            tenantContext.SchemaName,
            correlationId,
            activity?.Id,
            activity?.TraceId.ToString());
    }

    private static ActivityContext? ExtractTraceContext(string traceparent)
    {
        try
        {
            var parts = traceparent.Split('-');
            if (parts.Length >= 3)
            {
                var traceId = parts[1];
                var spanId = parts[2];
                var traceFlags = parts.Length > 3 ? parts[3] : "01";

                return new ActivityContext(
                    ActivityTraceId.CreateFromString(traceId),
                    ActivitySpanId.CreateFromString(spanId),
                    ActivityTraceFlags.Recorded);
            }
        }
        catch
        {
        }

        return null;
    }

    private static string? SerializeTraceparent(Activity activity)
    {
        return $"00-{activity.TraceId:X}-{activity.SpanId:X}-01";
    }
}

/// <summary>
/// Represents a tenant correlation context for distributed tracing.
/// </summary>
public readonly struct TenantCorrelationContext
{
    public string TenantId { get; }
    public string SchemaName { get; }
    public string CorrelationId { get; }
    public string? ActivityId { get; }
    public string? TraceId { get; }

    public TenantCorrelationContext(
        string tenantId,
        string schemaName,
        string correlationId,
        string? activityId,
        string? traceId)
    {
        TenantId = tenantId;
        SchemaName = schemaName;
        CorrelationId = correlationId;
        ActivityId = activityId;
        TraceId = traceId;
    }
}
