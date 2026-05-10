using System.Diagnostics;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Multitenancy.Execution;
using Karamchari.Core.Observability.Tenant;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Karamchari.Core.Middleware;

/// <summary>
/// Middleware that establishes the <see cref="TenantExecutionContext"/> for the current request
/// and enriches both distributed tracing (OpenTelemetry) and structured logging (Serilog)
/// with tenant and correlation metadata.
/// </summary>
public sealed class TenantObservabilityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantObservabilityMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantObservabilityMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the request pipeline.</param>
    /// <param name="logger">The logger for observability events.</param>
    public TenantObservabilityMiddleware(
        RequestDelegate next,
        ILogger<TenantObservabilityMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware to establish tenant context and enrich observability data.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="tenantProvider">The provider used to resolve the tenant for the request.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context, ITenantProvider tenantProvider)
    {
        // 1. Resolve Tenant Context
        // This will throw TenantResolutionException if resolution fails, which is handled
        // by the GlobalExceptionHandler later in the pipeline.
        TenantExecutionEnvelope envelope;
        try
        {
            envelope = tenantProvider.GetTenant();
        }
        catch (TenantResolutionException)
        {
            // For health checks or anonymous endpoints, we allow execution without a tenant.
            await _next(context);
            return;
        }

        // 2. Establish Execution Context (AsyncLocal)
        var executionContext = new TenantExecutionContext(envelope);
        using var scope = executionContext.Establish();

        // 3. Enrich Logging Context (Serilog)
        using (LogContext.PushProperty(TenantTelemetryTags.TenantId, envelope.TenantId))
        using (LogContext.PushProperty(TenantTelemetryTags.CorrelationId, envelope.CorrelationId))
        using (LogContext.PushProperty(TenantTelemetryTags.RequestId, envelope.RequestId))
        using (LogContext.PushProperty(TenantTelemetryTags.ExecutionSource, envelope.ExecutionSource.ToString()))
        {
            if (!string.IsNullOrEmpty(envelope.UserIdentity))
            {
                using var userScope = LogContext.PushProperty(TenantTelemetryTags.UserId, envelope.UserIdentity);
            }

            // 4. Enrich Distributed Trace (OpenTelemetry Activity)
            var activity = Activity.Current;
            if (activity != null)
            {
                activity.SetTag(TenantTelemetryTags.TenantId, envelope.TenantId);
                activity.SetTag(TenantTelemetryTags.CorrelationId, envelope.CorrelationId);
                activity.SetTag(TenantTelemetryTags.RequestId, envelope.RequestId);
                activity.SetTag(TenantTelemetryTags.ExecutionSource, envelope.ExecutionSource.ToString());

                if (!string.IsNullOrEmpty(envelope.UserIdentity))
                {
                    activity.SetTag(TenantTelemetryTags.UserId, envelope.UserIdentity);
                }

                if (!string.IsNullOrEmpty(envelope.WorkflowInstanceId))
                {
                    activity.SetTag(TenantTelemetryTags.WorkflowInstanceId, envelope.WorkflowInstanceId);
                }

                if (!string.IsNullOrEmpty(envelope.WorkflowStepId))
                {
                    activity.SetTag(TenantTelemetryTags.WorkflowStepId, envelope.WorkflowStepId);
                }

                if (envelope.SlaDeadline.HasValue)
                {
                    activity.SetTag(TenantTelemetryTags.SlaDeadline, envelope.SlaDeadline.Value.ToString("O"));
                }
            }

            _logger.LogDebug(
                "Request execution context established. Tenant: {TenantId}, Correlation: {CorrelationId}",
                envelope.TenantId, envelope.CorrelationId);

            await _next(context);
        }
    }
}
