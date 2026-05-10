using System.Diagnostics;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Multitenancy.Execution;

namespace Karamchari.Core.Observability.Tenant;

/// <summary>
/// A diagnostic activity listener that automatically enriches all activities
/// with tenant context, ensuring distributed traces carry tenant information
/// without requiring manual instrumentation in every component.
/// </summary>
public sealed class TenantAwareActivityListener : IDisposable
{
    private readonly ITenantProvider _tenantProvider;
    private readonly TenantActivitySource _activitySource;
    private readonly List<ActivityListener> _listeners = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantAwareActivityListener"/> class.
    /// </summary>
    /// <param name="tenantProvider">The tenant provider to resolve active tenant context.</param>
    /// <param name="activitySource">The tenant-aware activity source.</param>
    public TenantAwareActivityListener(
        ITenantProvider tenantProvider,
        TenantActivitySource activitySource)
    {
        _tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
        _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));

        var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            ActivityStarted = EnrichActivityWithTenantContext
        };
        _listeners.Add(listener);
        ActivitySource.AddActivityListener(listener);
    }

    private void EnrichActivityWithTenantContext(Activity activity)
    {
        if (activity == null)
        {
            return;
        }

        if (_tenantProvider.TryGetTenant(out var tenantCtx) && tenantCtx != null)
        {
            activity.SetTag(TenantTelemetryTags.TenantId, tenantCtx.TenantId);

            var executionSource = GetExecutionSourceFromActivity(activity);
            if (!string.IsNullOrEmpty(executionSource))
            {
                activity.SetTag(TenantTelemetryTags.ExecutionSource, executionSource);
            }
        }
        else
        {
            activity.SetTag("tenant.resolution", "failed");
        }
    }

    private static string GetExecutionSourceFromActivity(Activity activity)
    {
        return activity.Kind switch
        {
            ActivityKind.Client => ExecutionSource.HttpRequest.ToString(),
            ActivityKind.Server => ExecutionSource.HttpRequest.ToString(),
            ActivityKind.Producer => ExecutionSource.BackgroundJob.ToString(),
            ActivityKind.Consumer => ExecutionSource.MessageConsumer.ToString(),
            _ => ExecutionSource.Manual.ToString()
        };
    }

    /// <summary>
    /// Manually starts a message-based activity with correlation tracking.
    /// </summary>
    /// <param name="operationName">The operation name.</param>
    /// <param name="messageHeaders">Headers from the incoming message.</param>
    /// <returns>A new activity enriched with tenant context and correlation.</returns>
    public Activity? StartMessageActivity(string operationName, IDictionary<string, object?>? messageHeaders)
    {
        _tenantProvider.TryGetTenant(out var tenantContext);

        var fallbackEnvelope = new TenantExecutionEnvelope("unknown", Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"), ExecutionSource.Manual, TenantSource.JwtClaim);

        var activity = _activitySource.StartActivity(
            operationName,
            tenantContext ?? fallbackEnvelope,
            ExecutionSource.MessageConsumer,
            ExtractCorrelationId(messageHeaders));

        return activity;
    }

    /// <summary>
    /// Manually starts a background job activity.
    /// </summary>
    /// <param name="operationName">The operation name.</param>
    /// <param name="jobId">The unique job identifier.</param>
    /// <returns>A new activity enriched with tenant context.</returns>
    public Activity? StartBackgroundJobActivity(string operationName, string jobId)
    {
        _tenantProvider.TryGetTenant(out var tenantContext);

        var fallbackEnvelope = new TenantExecutionEnvelope("unknown", Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"), ExecutionSource.Manual, TenantSource.JwtClaim);

        var activity = _activitySource.StartActivity(
            operationName,
            tenantContext ?? fallbackEnvelope,
            ExecutionSource.BackgroundJob,
            jobId);

        return activity;
    }

    private static string? ExtractCorrelationId(IDictionary<string, object?>? headers)
    {
        if (headers == null)
        {
            return null;
        }

        if (headers.TryGetValue("correlation-id", out var correlationId) && correlationId is string correlationIdString)
        {
            return correlationIdString;
        }

        if (headers.TryGetValue("x-correlation-id", out var xCorrelationId) && xCorrelationId is string xCorrelationIdString)
        {
            return xCorrelationIdString;
        }

        return null;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        foreach (var listener in _listeners)
        {
            listener.ShouldListenTo = _ => false;
        }
        _listeners.Clear();
    }
}
