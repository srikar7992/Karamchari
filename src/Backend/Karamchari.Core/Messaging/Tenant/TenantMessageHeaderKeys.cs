namespace Karamchari.Core.Messaging.Tenant;

/// <summary>
/// Defines header key constants for tenant-aware message headers in MassTransit communications.
/// These headers are used to propagate tenant context across message consumers, publishers, and send operations.
/// </summary>
public static class TenantMessageHeaderKeys
{
    /// <summary>The tenant identifier header key.</summary>
    public const string TenantId = "X-Tenant-Id";

    /// <summary>The correlation identifier header key for request tracking.</summary>
    public const string CorrelationId = "X-Correlation-Id";

    /// <summary>The request identifier header key for unique request identification.</summary>
    public const string RequestId = "X-Request-Id";

    /// <summary>The execution source header key indicating the origin of the message.</summary>
    public const string ExecutionSource = "X-Execution-Source";

    /// <summary>The retry attempt header key indicating the current retry count.</summary>
    public const string RetryAttempt = "X-Retry-Attempt";

    /// <summary>The replay count header key indicating how many times the message has been replayed.</summary>
    public const string ReplayCount = "X-Replay-Count";

    /// <summary>The trace identifier header key for distributed tracing.</summary>
    public const string TraceId = "X-Trace-Id";

    /// <summary>The span identifier header key for distributed tracing spans.</summary>
    public const string SpanId = "X-Span-Id";

    /// <summary>The content hash header key for message integrity verification.</summary>
    public const string ContentHash = "X-Content-Hash";

    /// <summary>The timestamp header key for message creation time.</summary>
    public const string Timestamp = "X-Timestamp";

    /// <summary>The user identity header key for tracking the user who initiated the operation.</summary>
    public const string UserIdentity = "X-User-Identity";

    /// <summary>The original message identifier header key for tracking message origins.</summary>
    public const string OriginalMessageId = "X-Original-Message-Id";

    /// <summary>The execution envelope header key containing serialized tenant execution context.</summary>
    public const string ExecutionEnvelope = "X-Execution-Envelope";

    /// <summary>The workflow instance identifier header key.</summary>
    public const string WorkflowInstanceId = "X-Workflow-Instance-Id";

    /// <summary>The workflow step identifier header key.</summary>
    public const string WorkflowStepId = "X-Workflow-Step-Id";

    /// <summary>The workflow SLA deadline header key.</summary>
    public const string WorkflowSlaDeadline = "X-Workflow-Sla-Deadline";
}
