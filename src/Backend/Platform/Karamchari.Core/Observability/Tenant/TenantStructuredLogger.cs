using System.Runtime.CompilerServices;
using Karamchari.Core.Multitenancy;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Observability.Tenant;

/// <summary>
/// Structured logger with tenant context for the Karamchari platform.
/// Provides methods for logging tenant-aware operations, errors, and warnings
/// with automatic tenant correlation and structured logging support.
/// </summary>
public static class TenantStructuredLogger
{
    private const string TenantExecutionEnvelopeKey = "TenantExecutionEnvelope";

    /// <summary>
    /// Logs a tenant operation with structured parameters.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="operation">The operation being performed.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="executionSource">The source of execution.</param>
    /// <param name="correlationId">Optional correlation ID for distributed tracing.</param>
    /// <param name="additionalProperties">Additional structured properties.</param>
    public static void LogTenantOperation(
        this ILogger logger,
        string operation,
        string tenantId,
        ExecutionSource executionSource,
        string? correlationId = null,
        [CallerMemberName] string? memberName = null,
        [CallerFilePath] string? filePath = null,
        [CallerLineNumber] int lineNumber = 0,
        params (string Key, object? Value)[] additionalProperties)
    {
        var properties = new List<KeyValuePair<string, object?>>
        {
            new("operation", operation),
            new(TenantTelemetryTags.TenantId, tenantId),
            new(TenantTelemetryTags.ExecutionSource, executionSource.ToString())
        };

        if (!string.IsNullOrEmpty(correlationId))
        {
            properties.Add(new KeyValuePair<string, object?>(TenantTelemetryTags.CorrelationId, correlationId));
        }

        foreach (var (key, value) in additionalProperties)
        {
            properties.Add(new KeyValuePair<string, object?>(key, value));
        }

        logger.LogInformation(
            "[{Operation}] Tenant: {TenantId}, Source: {ExecutionSource}, Correlation: {CorrelationId}, Member: {MemberName}, File: {FilePath}:{LineNumber}",
            operation,
            tenantId,
            executionSource,
            correlationId ?? "none",
            memberName ?? "unknown",
            filePath ?? "unknown",
            lineNumber);
    }

    /// <summary>
    /// Logs a tenant error with full exception details and context.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="ex">The exception that occurred.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="operation">The operation that failed.</param>
    /// <param name="executionSource">The source of execution.</param>
    /// <param name="correlationId">Optional correlation ID.</param>
    /// <param name="additionalProperties">Additional structured properties.</param>
    public static void LogTenantError(
        this ILogger logger,
        Exception ex,
        string tenantId,
        string operation,
        ExecutionSource executionSource,
        string? correlationId = null,
        [CallerMemberName] string? memberName = null,
        [CallerFilePath] string? filePath = null,
        [CallerLineNumber] int lineNumber = 0,
        params (string Key, object? Value)[] additionalProperties)
    {
        logger.LogError(
            ex,
            "[{Operation}] Tenant: {TenantId}, Source: {ExecutionSource}, Correlation: {CorrelationId}, Error: {ErrorType}, Message: {ErrorMessage}, Member: {MemberName}, File: {FilePath}:{LineNumber}",
            operation,
            tenantId,
            executionSource,
            correlationId ?? "none",
            ex.GetType().Name,
            ex.Message,
            memberName ?? "unknown",
            filePath ?? "unknown",
            lineNumber);
    }

    /// <summary>
    /// Logs a tenant warning with context.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="warning">The warning message.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="operation">The operation related to the warning.</param>
    /// <param name="executionSource">The source of execution.</param>
    /// <param name="correlationId">Optional correlation ID.</param>
    /// <param name="additionalProperties">Additional structured properties.</param>
    public static void LogTenantWarning(
        this ILogger logger,
        string warning,
        string tenantId,
        string operation,
        ExecutionSource executionSource,
        string? correlationId = null,
        [CallerMemberName] string? memberName = null,
        [CallerFilePath] string? filePath = null,
        [CallerLineNumber] int lineNumber = 0,
        params (string Key, object? Value)[] additionalProperties)
    {
        logger.LogWarning(
            "[{Operation}] Tenant: {TenantId}, Source: {ExecutionSource}, Correlation: {CorrelationId}, Warning: {Warning}, Member: {MemberName}, File: {FilePath}:{LineNumber}",
            operation,
            tenantId,
            executionSource,
            correlationId ?? "none",
            warning,
            memberName ?? "unknown",
            filePath ?? "unknown",
            lineNumber);
    }

    /// <summary>
    /// Logs a tenant isolation violation (security event).
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="violationType">Type of isolation violation.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="details">Additional violation details.</param>
    public static void LogTenantIsolationViolation(
        this ILogger logger,
        string violationType,
        string tenantId,
        string details)
    {
        logger.LogCritical(
            "TENANT ISOLATION VIOLATION: Type={ViolationType}, Tenant={TenantId}, Details={Details}",
            violationType,
            tenantId,
            details);
    }

    /// <summary>
    /// Logs retry attempt for tenant operations.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="operation">The operation being retried.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="attemptNumber">The current attempt number.</param>
    /// <param name="maxAttempts">Maximum retry attempts.</param>
    /// <param name="correlationId">Optional correlation ID.</param>
    public static void LogTenantRetry(
        this ILogger logger,
        string operation,
        string tenantId,
        int attemptNumber,
        int maxAttempts,
        string? correlationId = null)
    {
        logger.LogWarning(
            "Tenant retry: Tenant={TenantId}, Operation={Operation}, Attempt={Attempt}/{MaxAttempts}, Correlation={CorrelationId}",
            tenantId,
            operation,
            attemptNumber,
            maxAttempts,
            correlationId ?? "none");
    }

    /// <summary>
    /// Logs replay detection for idempotent operations.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="operation">The operation being replayed.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="replayCount">Number of times the operation has been replayed.</param>
    /// <param name="correlationId">Optional correlation ID.</param>
    public static void LogTenantReplay(
        this ILogger logger,
        string operation,
        string tenantId,
        int replayCount,
        string? correlationId = null)
    {
        logger.LogWarning(
            "Tenant replay detected: Tenant={TenantId}, Operation={Operation}, ReplayCount={ReplayCount}, Correlation={CorrelationId}",
            tenantId,
            operation,
            replayCount,
            correlationId ?? "none");
    }

    /// <summary>
    /// Logs tenant context switching (for debugging purposes).
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="fromTenantId">Previous tenant ID.</param>
    /// <param name="toTenantId">New tenant ID.</param>
    /// <param name="operation">The operation causing the switch.</param>
    public static void LogTenantExecutionEnvelopeSwitch(
        this ILogger logger,
        string fromTenantId,
        string toTenantId,
        string operation)
    {
        logger.LogWarning(
            "Tenant context switch: From={FromTenant}, To={ToTenant}, Operation={Operation}",
            fromTenantId,
            toTenantId,
            operation);
    }

    /// <summary>
    /// Logs a business workflow transition with full state and SLA context.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="workflowInstanceId">The unique workflow identifier.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="sourceState">The current state before transition.</param>
    /// <param name="targetState">The target state after transition.</param>
    /// <param name="actor">The entity or user that triggered the transition.</param>
    /// <param name="slaDeadline">Optional SLA deadline for the new state.</param>
    public static void LogWorkflowTransition(
        this ILogger logger,
        string workflowInstanceId,
        string tenantId,
        string sourceState,
        string targetState,
        string actor,
        DateTime? slaDeadline = null)
    {
        var properties = new List<KeyValuePair<string, object?>>
        {
            new(TenantTelemetryTags.WorkflowInstanceId, workflowInstanceId),
            new(TenantTelemetryTags.TenantId, tenantId),
            new("workflow.source_state", sourceState),
            new("workflow.target_state", targetState),
            new("workflow.actor", actor)
        };

        if (slaDeadline.HasValue)
        {
            properties.Add(new KeyValuePair<string, object?>(TenantTelemetryTags.SlaDeadline, slaDeadline.Value.ToString("O")));
        }

        logger.LogInformation(
            "Workflow Transition: [{WorkflowInstanceId}] {SourceState} -> {TargetState} by {Actor}. Tenant: {TenantId}",
            workflowInstanceId,
            sourceState,
            targetState,
            actor,
            tenantId);
    }
}
