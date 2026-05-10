using System.Diagnostics;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Multitenancy.Execution;

namespace Karamchari.Core.Observability.Tenant;

/// <summary>
/// Tenant-aware activity source for distributed tracing.
/// Creates activities with tenant context, automatically adds tenant tags,
/// supports W3C TraceContext format, and enables OpenTelemetry integration.
/// </summary>
public sealed class TenantActivitySource
{
    private readonly ActivitySource _activitySource;

    /// <summary>
    /// The name of the activity source.
    /// </summary>
    public const string SourceName = "Karamchari.TenantTracing";

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantActivitySource"/> class.
    /// </summary>
    public TenantActivitySource()
    {
        _activitySource = new ActivitySource(SourceName);
    }

    /// <summary>
    /// Gets the underlying ActivitySource for OpenTelemetry integration.
    /// </summary>
    public ActivitySource Source => _activitySource;

    /// <summary>
    /// Creates an activity with tenant context for the specified operation name.
    /// </summary>
    /// <param name="operationName">The name of the operation being traced.</param>
    /// <param name="tenantContext">The tenant context containing tenant information.</param>
    /// <param name="executionSource">The source of the execution (HTTP, message, background job, etc.).</param>
    /// <param name="correlationId">Optional correlation ID for linking distributed operations.</param>
    /// <returns>A new <see cref="Activity"/> instance with tenant tags, or null if tracing is not active.</returns>
    public Activity? StartActivity(
        string operationName,
        TenantExecutionEnvelope tenantContext,
        ExecutionSource executionSource,
        string? correlationId = null)
    {
        var activity = _activitySource.StartActivity(
            operationName,
            ActivityKind.Internal);

        if (activity == null)
        {
            return null;
        }

        SetTenantTags(activity, tenantContext, executionSource, correlationId);

        return activity;
    }

    /// <summary>
    /// Creates an activity for external (client/server) operations with tenant context.
    /// </summary>
    /// <param name="operationName">The name of the operation.</param>
    /// <param name="tenantContext">The tenant context.</param>
    /// <param name="executionSource">The execution source.</param>
    /// <param name="kind">The activity kind (Client or Server).</param>
    /// <param name="correlationId">Optional correlation ID.</param>
    /// <returns>A new <see cref="Activity"/> instance with tenant tags.</returns>
    public Activity? StartExternalActivity(
        string operationName,
        TenantExecutionEnvelope tenantContext,
        ExecutionSource executionSource,
        ActivityKind kind,
        string? correlationId = null)
    {
        var activity = _activitySource.StartActivity(
            operationName,
            kind);

        if (activity == null)
        {
            return null;
        }

        SetTenantTags(activity, tenantContext, executionSource, correlationId);

        return activity;
    }

    /// <summary>
    /// Creates an activity with retry context for tracking retry attempts.
    /// </summary>
    /// <param name="operationName">The operation name.</param>
    /// <param name="tenantContext">The tenant context.</param>
    /// <param name="executionSource">The execution source.</param>
    /// <param name="retryAttempt">The current retry attempt number.</param>
    /// <param name="correlationId">Optional correlation ID.</param>
    /// <returns>A new activity with retry tracking tags.</returns>
    public Activity? StartRetryActivity(
        string operationName,
        TenantExecutionEnvelope tenantContext,
        ExecutionSource executionSource,
        int retryAttempt,
        string? correlationId = null)
    {
        var activity = _activitySource.StartActivity(
            operationName,
            ActivityKind.Internal);

        if (activity == null)
        {
            return null;
        }

        SetTenantTags(activity, tenantContext, executionSource, correlationId);
        activity.SetTag(TenantTelemetryTags.RetryAttempt, retryAttempt.ToString());

        return activity;
    }

    /// <summary>
    /// Creates an activity with replay context for idempotent operations.
    /// </summary>
    /// <param name="operationName">The operation name.</param>
    /// <param name="tenantContext">The tenant context.</param>
    /// <param name="executionSource">The execution source.</param>
    /// <param name="replayCount">The number of times the operation has been replayed.</param>
    /// <param name="correlationId">Optional correlation ID.</param>
    /// <returns>A new activity with replay tracking tags.</returns>
    public Activity? StartReplayActivity(
        string operationName,
        TenantExecutionEnvelope tenantContext,
        ExecutionSource executionSource,
        int replayCount,
        string? correlationId = null)
    {
        var activity = _activitySource.StartActivity(
            operationName,
            ActivityKind.Internal);

        if (activity == null)
        {
            return null;
        }

        SetTenantTags(activity, tenantContext, executionSource, correlationId);
        activity.SetTag(TenantTelemetryTags.ReplayCount, replayCount.ToString());

        return activity;
    }

    private static void SetTenantTags(
        Activity activity,
        TenantExecutionEnvelope tenantContext,
        ExecutionSource executionSource,
        string? correlationId)
    {
        activity.SetTag(TenantTelemetryTags.TenantId, tenantContext.TenantId);
        activity.SetTag(TenantTelemetryTags.ExecutionSource, executionSource.ToString());

        if (!string.IsNullOrEmpty(correlationId))
        {
            activity.SetTag(TenantTelemetryTags.CorrelationId, correlationId);
        }
    }
}
