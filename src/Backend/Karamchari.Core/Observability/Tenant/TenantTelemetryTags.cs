namespace Karamchari.Core.Observability.Tenant;

/// <summary>
/// Static class containing telemetry tag constants for tenant-aware observability.
/// These tags are used across tracing, metrics, and logging to ensure tenant context
/// is consistently propagated throughout the Karamchari platform.
/// </summary>
public static class TenantTelemetryTags
{
    /// <summary>
    /// The unique identifier for the tenant associated with the operation.
    /// Used as the primary tenant correlation tag in all telemetry data.
    /// </summary>
    public const string TenantId = "tenant.id";

    /// <summary>
    /// A correlation identifier that links related operations across distributed services.
    /// Enables tracing of a single request through multiple service boundaries.
    /// </summary>
    public const string CorrelationId = "tenant.correlation_id";

    /// <summary>
    /// Indicates the origin source of the execution (e.g., HTTP request, message consumer, background job).
    /// Useful for debugging tenant context resolution and understanding request flow.
    /// </summary>
    public const string ExecutionSource = "tenant.execution_source";

    /// <summary>
    /// The current attempt number when an operation is being retried.
    /// Used to identify retry storms and analyze retry patterns per tenant.
    /// </summary>
    public const string RetryAttempt = "tenant.retry_attempt";

    /// <summary>
    /// The number of times an operation has been replayed (e.g., for idempotency handling).
    /// Helps detect and diagnose replay attacks or excessive reprocessing.
    /// </summary>
    public const string ReplayCount = "tenant.replay_count";
}
